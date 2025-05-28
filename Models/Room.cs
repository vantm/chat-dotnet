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

    /// <summary>
    /// Validates that the specified user can close this room
    /// </summary>
    /// <param name="userId">The user ID attempting to close the room</param>
    /// <exception cref="ArgumentException">Thrown when user ID is invalid</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authorized to close the room</exception>
    public void ValidateCanClose(string userId)
    {
        // Validate user ID
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        // Check if user is the owner
        if (OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the room owner can close the room");
        }
    }

    public static Room Create(string name, string ownerUserId)
    {
        // Validate room name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Room name cannot be empty", nameof(name));
        }

        if (name.Length < 2 || name.Length > 100)
        {
            throw new ArgumentException("Room name must be between 2 and 100 characters", nameof(name));
        }

        // Validate owner user ID
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("Owner user ID cannot be empty", nameof(ownerUserId));
        }

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