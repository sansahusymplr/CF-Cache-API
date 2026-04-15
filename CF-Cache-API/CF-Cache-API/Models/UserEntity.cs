namespace CF_Cache_API.Models;

public class UserEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int EntityId { get; set; }
}
