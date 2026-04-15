import base64
import boto3
import hmac
import hashlib
import json
import time
from urllib.parse import unquote

# -------- Config --------
SECRET_ID = "pdm-poc-payer-migration"   # or full ARN
SECRET_KEY_NAME = "tenantctx_hmac_key"
SECRETS_REGION = "us-east-2"

EXCLUDE_PREFIXES = [
    "/api/employee/health",
    "/api/employee/swagger",
    "/api/employee/metrics",
]

COOKIE_NAME = "TenantCtx"
TENANT_HEADER_KEY = "X-Tenant-Id"
ENTITY_HEADER_KEY = "X-Entity"

# -------- Secret cache (global, reused across invocations) --------
_HMAC_BYTES = None
_HMAC_LOADED_AT = 0
_HMAC_CACHE_TTL_SECONDS = 300


def handler(event, context):
    request = (
        event.get("Records", [{}])[0]
        .get("cf", {})
        .get("request")
    )
    if not request:
        return {"status": "400", "statusDescription": "Not a CloudFront (Lambda@Edge) event"}

    uri = request.get("uri", "") or ""
    headers = request.get("headers", {}) or {}

    if any(uri == p or uri.startswith(p + "/") for p in EXCLUDE_PREFIXES):
        return request

    cookie_headers = headers.get("cookie")
    if not cookie_headers:
        return request

    cookie_header = "; ".join([h.get("value", "") for h in cookie_headers if h.get("value")])
    if not cookie_header:
        return request

    token = parse_cookie(cookie_header, COOKIE_NAME)
    if not token:
        return request

    try:
        secret_bytes = get_hmac_key_bytes()
    except Exception:
        return deny(500, "Unable to load TenantCtx secret")

    payload = verify_tenant_ctx(token, secret_bytes)
    if not payload or not payload.get("tid"):
        return deny(401, "Invalid TenantCtx")

    # Validate encrypted IP against client's X-Forwarded-For
    encrypted_ip = payload.get("ip", "")
    if encrypted_ip:
        client_ip = get_client_ip(headers)
        decrypted_ip = decrypt_ip(encrypted_ip, secret_bytes)
        if decrypted_ip and client_ip and decrypted_ip != client_ip:
            return deny(401, "IP Mismatch")

    tenant_id = str(payload["tid"])
    entity = str(payload.get("entity", ""))

    headers["x-tenant-id"] = [{"key": TENANT_HEADER_KEY, "value": tenant_id}]
    if entity:
        headers["x-entity"] = [{"key": ENTITY_HEADER_KEY, "value": entity}]
    request["headers"] = headers

    new_uri = replace_tenant_placeholder_anywhere(uri, tenant_id)
    if new_uri != uri:
        print(f"Rewrote URI: {uri} => {new_uri}")
        request["uri"] = new_uri

    return request


def get_client_ip(headers):
    """Extract the first (original client) IP from X-Forwarded-For header."""
    xff = headers.get("x-forwarded-for")
    if not xff:
        return ""
    value = xff[0].get("value", "")
    if not value:
        return ""
    return value.split(",")[0].strip()


def decrypt_ip(encrypted_ip, hmac_key):
    """Decrypt HMAC-XOR encrypted IP using only built-in libraries.
    Format: base64url(nonce_16bytes + xor_cipher)
    Keystream: HMAC-SHA256(hmac_key, nonce) → 32 bytes
    """
    try:
        combined = base64url_decode(encrypted_ip)
        nonce = combined[:16]
        cipher_bytes = combined[16:]

        # Same keystream: HMAC-SHA256(key, nonce)
        keystream = hmac.new(hmac_key, nonce, hashlib.sha256).digest()

        # XOR to recover plaintext
        plain = bytes(c ^ keystream[i % len(keystream)] for i, c in enumerate(cipher_bytes))
        return plain.decode("utf-8")
    except Exception:
        return ""


def replace_tenant_placeholder_anywhere(uri: str, tenant_id: str) -> str:
    if not uri or uri == "/":
        return uri

    leading_slash = uri.startswith("/")
    parts = [p for p in uri.split("/") if p != ""]
    if not parts:
        return uri

    changed = False
    for i in range(len(parts)):
        seg = parts[i]
        if is_tenant_placeholder(seg) or is_tenant_placeholder(unquote(seg)):
            parts[i] = tenant_id
            changed = True

    if not changed:
        return uri

    rebuilt = "/".join(parts)
    return f"/{rebuilt}" if leading_slash else rebuilt


def is_tenant_placeholder(value: str) -> bool:
    if not value:
        return False

    v = value.strip()
    v_lower = v.lower()

    if v_lower in ("{tenantid}", "{tenant_id}", "{tenant}", "{tid}"):
        return True
    if v_lower in ("%7btenantid%7d", "%7btenant_id%7d", "%7btenant%7d", "%7btid%7d"):
        return True
    if v.startswith("{") and v.endswith("}"):
        inner = v[1:-1].strip().lower()
        if inner in ("tenantid", "tenant_id", "tenant", "tid"):
            return True

    return False


def get_hmac_key_bytes():
    global _HMAC_BYTES, _HMAC_LOADED_AT

    now = int(time.time())
    if _HMAC_BYTES and (now - _HMAC_LOADED_AT) < _HMAC_CACHE_TTL_SECONDS:
        return _HMAC_BYTES

    sm = boto3.client("secretsmanager", region_name=SECRETS_REGION)
    resp = sm.get_secret_value(SecretId=SECRET_ID)

    secret_str = resp.get("SecretString")
    if secret_str:
        obj = json.loads(secret_str)
        b64_key = obj.get(SECRET_KEY_NAME)
        if not b64_key:
            raise ValueError(f"Secret key '{SECRET_KEY_NAME}' not found in SecretString JSON")
    else:
        secret_bin = resp.get("SecretBinary")
        if not secret_bin:
            raise ValueError("SecretString and SecretBinary both missing")
        obj = json.loads(base64.b64decode(secret_bin).decode("utf-8"))
        b64_key = obj.get(SECRET_KEY_NAME)
        if not b64_key:
            raise ValueError(f"Secret key '{SECRET_KEY_NAME}' not found in SecretBinary JSON")

    secret_bytes = base64.b64decode(b64_key)
    _HMAC_BYTES = secret_bytes
    _HMAC_LOADED_AT = now
    return secret_bytes


def verify_tenant_ctx(token, secret_bytes):
    parts = token.split(".")
    if len(parts) != 2:
        return None

    payload_b64 = parts[0]
    sig_b64url = parts[1]

    expected_sig = hmac.new(
        secret_bytes,
        payload_b64.encode("utf-8"),
        hashlib.sha256
    ).digest()

    provided_sig = base64url_decode(sig_b64url)
    if len(provided_sig) != len(expected_sig):
        return None
    if not hmac.compare_digest(expected_sig, provided_sig):
        return None

    try:
        payload_json = base64url_decode(payload_b64).decode("utf-8")
        payload = json.loads(payload_json)
    except Exception:
        return None

    now = int(time.time())
    if not payload.get("tid") or not payload.get("exp") or int(payload["exp"]) < now:
        return None

    return payload


def base64url_decode(input_str: str) -> bytes:
    s = input_str.replace("-", "+").replace("_", "/")
    pad = len(s) % 4
    if pad == 2:
        s += "=="
    elif pad == 3:
        s += "="
    elif pad != 0:
        s += "=" * (4 - pad)
    return base64.b64decode(s)


def parse_cookie(cookie_header: str, name: str):
    for part in cookie_header.split(";"):
        t = part.strip()
        eq = t.find("=")
        if eq == -1:
            continue
        k = t[:eq].strip()
        v = t[eq + 1:].strip()
        if k == name:
            return v
    return None


def deny(status_code: int, message: str):
    return {
        "status": str(status_code),
        "statusDescription": message,
        "headers": {
            "cache-control": [{"key": "Cache-Control", "value": "no-store"}]
        },
    }
