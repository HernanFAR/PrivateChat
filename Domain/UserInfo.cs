using VSlices.Core.Abstracts.Responses;

namespace Domain;

public class UserInfo
{
    public class System
    {
        public static readonly string Id = Guid.Empty.ToString().Replace("-", "");
        public const string Name = "System";
    }

    public const string CantAddMoreRooms = "No puedes entrar a más de 5 habitaciones a la vez";

    public string Id { get; }

    public string Name { get; }

    public string? ConnectionId { get; private set; }

    public DateTimeOffset LastInteraction { get; private set; }

    public IReadOnlyList<string> Rooms => _rooms;
    private readonly List<string> _rooms;

    public UserInfo(string id, string name, DateTimeOffset lastInteraction)
    {
        Id = id;
        Name = name;
        LastInteraction = lastInteraction;

        _rooms = new List<string>();
    }

    public void UpdateConnectionId(string connectionId)
    {
        ConnectionId = connectionId;
    }

    public Response<Success> AddRoom(string roomId)
    {
        if (_rooms.Count > 4)
        {
            return BusinessFailure.Of.DomainValidation(CantAddMoreRooms);
        }

        if (_rooms.Contains(roomId))
        {
            return new Success();
        }

        _rooms.Add(roomId);

        return new Success();
    }

    public Response<Success> RemoveRoom(string roomId)
    {
        return _rooms.Remove(roomId)
            ? new Success()
            : BusinessFailure.Of.NotFoundResource();
    }

    public void UpdateLastActionTime()
    {
        LastInteraction = DateTimeOffset.UtcNow;
    }
}