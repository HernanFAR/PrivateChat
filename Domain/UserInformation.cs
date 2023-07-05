using VSlices.Core.Abstracts.Responses;

namespace Domain;

public class UserInformation
{
    public const string CantAddMoreRooms = "No puedes entrar a más de 5 habitaciones a la vez";

    public string Id { get; }
    public string ConnectionId { get; }
    public IReadOnlyList<string> Rooms => _rooms;
    private readonly List<string> _rooms;

    public UserInformation(string id, string connectionId)
    {
        Id = id;
        ConnectionId = connectionId;

        _rooms = new List<string>();
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
}