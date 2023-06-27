using OneOf;
using OneOf.Types;
using VSlices.Core.Abstracts.Responses;

namespace Domain;

public class UserInformation
{
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

    public OneOf<Success, BusinessFailure> AddRoom(string roomId)
    {
        if (_rooms.Count > 5)
        {
            return BusinessFailure.Of.DomainValidation("No puedes entrar a más de 5 habitaciones a la vez");
        }

        _rooms.Add(roomId);

        return new Success();
    }

    public OneOf<Success, BusinessFailure> RemoveRoom(string roomId)
    {
        return _rooms.Remove(roomId)
            ? new Success()
            : BusinessFailure.Of.NotFoundResource();
    }
}