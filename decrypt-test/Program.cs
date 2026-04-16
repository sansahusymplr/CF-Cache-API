using System.Security.Cryptography;
using System.Text;

var hmacKey = Convert.FromBase64String("BIHCI8+3F9MQw3u5YeVTI4BR93suH6fLHj7gw2vVNdw=");

var encrypted = "a8KojBLJTyNpPkS9o6DjeqpEh-6Oqf86tyjJeA";

var b64 = encrypted.Replace('-', '+').Replace('_', '/');
switch (b64.Length % 4)
{
    case 2: b64 += "=="; break;
    case 3: b64 += "="; break;
}
var combined = Convert.FromBase64String(b64);

var nonce = new byte[16];
var cipherBytes = new byte[combined.Length - 16];
Array.Copy(combined, 0, nonce, 0, 16);
Array.Copy(combined, 16, cipherBytes, 0, cipherBytes.Length);

using var hmac = new HMACSHA256(hmacKey);
var keystream = hmac.ComputeHash(nonce);

var plain = new byte[cipherBytes.Length];
for (int i = 0; i < cipherBytes.Length; i++)
    plain[i] = (byte)(cipherBytes[i] ^ keystream[i % keystream.Length]);

Console.WriteLine("Decrypted IP: " + Encoding.UTF8.GetString(plain));
