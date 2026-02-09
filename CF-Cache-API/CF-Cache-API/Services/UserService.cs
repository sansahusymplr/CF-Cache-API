using CF_Cache_API.Models;

namespace CF_Cache_API.Services;

public class UserService
{
    private readonly List<User> _users = new()
    {
        new User { Email = "a@customer1.com", Password = "abc", TenantId = "tenant-customer1" },
        new User { Email = "b@customer1.com", Password = "abc", TenantId = "tenant-customer1" },
        new User { Email = "c@customer1.com", Password = "abc", TenantId = "tenant-customer1" },
        new User { Email = "a@customer2.com", Password = "abc", TenantId = "tenant-customer2" },
        new User { Email = "b@customer2.com", Password = "abc", TenantId = "tenant-customer2" },
        new User { Email = "b@customer3.com", Password = "abc", TenantId = "tenant-customer3" }
    };

    public User? Authenticate(string email, string password)
    {
        return _users.FirstOrDefault(u => u.Email == email && u.Password == password);
    }

    public List<User> GetAllUsers()
    {
        return _users;
    }
}
