using CF_Cache_API.Models;

namespace CF_Cache_API.Services;

public class UserEntityService
{
    private readonly List<Entity> _entities = new()
    {
        new Entity { Id = 1, Name = "Cigna", TenantId = "tenant-customer3" },
        new Entity { Id = 2, Name = "HCA", TenantId = "tenant-customer3" },
        new Entity { Id = 3, Name = "Apollo", TenantId = "tenant-customer3" },
        new Entity { Id = 4, Name = "Fortis", TenantId = "tenant-customer3" },
        new Entity { Id = 5, Name = "Sakra", TenantId = "tenant-customer3" },
        new Entity { Id = 6, Name = "Manipal", TenantId = "tenant-customer3" }
    };

    private readonly List<UserEntity> _userEntities = new()
    {
        new UserEntity { Id = 1, Email = "a@customer3.com", EntityId = 1 },
        new UserEntity { Id = 2, Email = "a@customer3.com", EntityId = 3 },
        new UserEntity { Id = 3, Email = "b@customer3.com", EntityId = 2 },
        new UserEntity { Id = 4, Email = "b@customer3.com", EntityId = 4 },
        new UserEntity { Id = 5, Email = "b@customer3.com", EntityId = 5 },
        new UserEntity { Id = 6, Email = "b@customer3.com", EntityId = 6 }
    };

    public List<Entity> GetEntitiesByEmail(string email)
    {
        var entityIds = _userEntities.Where(ue => ue.Email == email).Select(ue => ue.EntityId);
        return _entities.Where(e => entityIds.Contains(e.Id)).ToList();
    }

    public List<Entity> GetEntitiesByTenant(string tenantId)
    {
        return _entities.Where(e => e.TenantId == tenantId).ToList();
    }

    public List<string> GetEntityNamesForUser(string email)
    {
        var entityIds = _userEntities.Where(ue => ue.Email == email).Select(ue => ue.EntityId);
        return _entities.Where(e => entityIds.Contains(e.Id)).Select(e => e.Name).ToList();
    }
}
