using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using chat_dotnet.Events;
using chat_dotnet.Messaging;
using chat_dotnet.Models;

namespace chat_dotnet.Actors;

public class ChatRoomManager : ReceivePersistentActor
{
    private readonly Dictionary<string, Room> _rooms = [];
    private readonly Dictionary<string, DateTimeOffset> _closedRooms = [];

    public override string PersistenceId => "chat-room-manager";

    public ChatRoomManager()
    {
        // Command handlers
        Command<CreateChatRoomRequest>(HandleCreateChatRoom);
        Command<CloseChatRoomRequest>(HandleCloseChatRoom);

        // Recovery handlers
        Recover<ChatRoomCreatedEvent>(evt =>
        {
            _rooms[evt.Room.Id] = evt.Room;
        });

        Recover<ChatRoomClosedEvent>(evt =>
        {
            _rooms.Remove(evt.RoomId);
            _closedRooms[evt.RoomId] = evt.ClosedAt;
        });
    }

    private void HandleCreateChatRoom(CreateChatRoomRequest request)
    {
        var (name, ownerUserId) = request;
        var sender = Sender;

        try
        {
            var room = Room.Create(name, ownerUserId);
            var evt = new ChatRoomCreatedEvent(room);

            Persist(evt, persistedEvent =>
            {
                _rooms[room.Id] = room;
                sender.Tell(CreateChatRoomResponse.Success(room.Id));
                Context.GetLogger().Info("Chat room '{0}' created with ID '{1}' by user '{2}'",
                    room.Name, room.Id, room.OwnerUserId);
            });
        }
        catch (ArgumentException ex)
        {
            sender.Tell(CreateChatRoomResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            Context.GetLogger().Error(ex, "Failed to create chat room");
            sender.Tell(CreateChatRoomResponse.Fail("Failed to create chat room"));
        }
    }

    private void HandleCloseChatRoom(CloseChatRoomRequest request)
    {
        var (roomId, userId) = request;
        var sender = Sender;

        try
        {
            // Validate room ID
            if (string.IsNullOrWhiteSpace(roomId))
            {
                sender.Tell(CloseChatRoomResponse.Fail("Room ID cannot be empty"));
                return;
            }

            // Check if room exists
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                sender.Tell(CloseChatRoomResponse.Fail("Room not found"));
                return;
            }

            // Validate that user can close the room (domain validation)
            room.ValidateCanClose(userId);

            var evt = new ChatRoomClosedEvent(roomId, DateTimeOffset.UtcNow);

            Persist(evt, persistedEvent =>
            {
                _rooms.Remove(roomId);
                _closedRooms[roomId] = persistedEvent.ClosedAt;
                sender.Tell(CloseChatRoomResponse.Success());
                Context.GetLogger().Info("Chat room '{0}' closed by user '{1}' at {2}",
                    roomId, userId, persistedEvent.ClosedAt);
            });
        }
        catch (ArgumentException ex)
        {
            sender.Tell(CloseChatRoomResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            sender.Tell(CloseChatRoomResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            Context.GetLogger().Error(ex, "Failed to close chat room {0}", roomId);
            sender.Tell(CloseChatRoomResponse.Fail("Failed to close chat room"));
        }
    }

    /// <summary>
    /// Gets all active rooms (for debugging/admin purposes)
    /// </summary>
    public Dictionary<string, Room> GetActiveRooms() => new(_rooms);

    /// <summary>
    /// Gets a specific room by ID
    /// </summary>
    public Room? GetRoom(string roomId) => _rooms.TryGetValue(roomId, out var room) ? room : null;
}