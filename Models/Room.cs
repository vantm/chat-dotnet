namespace chat_dotnet.Models;

public class Room
{
    public string Id { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string OwnerUserId { get; private set; } = default!;
    public List<string> UserIds { get; private set; } = [];

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void AddUser(string userId)
    {
        if (userId == OwnerUserId)
        {
            throw new InvalidOperationException("Cannot add the owner to the room.");
        }
        if (UserIds.Contains(userId))
        {
            throw new InvalidOperationException("User already in the room.");
        }
        UserIds.Add(userId);
    }

    public void RemoveUser(string userId)
    {
        if (userId == OwnerUserId)
        {
            throw new InvalidOperationException("Cannot remove the owner from the room.");
        }
        UserIds.Remove(userId);
    }

    public static Room Create(string name, string ownerUserId)
    {
        var id = Guid.NewGuid().ToString();
        return new Room
        {
            Id = id,
            Name = name,
            OwnerUserId = ownerUserId,
            UserIds = [ownerUserId]
        };
    }
}