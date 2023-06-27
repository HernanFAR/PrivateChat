using Domain;
using OneOf;
using OneOf.Types;
using System.Collections.Concurrent;
using VSlices.Core.Abstracts.Responses;

namespace CrossCutting;

public class UserManager
{
    private readonly ConcurrentDictionary<string, UserInformation> _userInfos = new();

    public void RegisterUserWithConnectionId(string userId, string connectionId)
    {
        _userInfos.TryAdd(userId, new UserInformation(userId, connectionId));
    }

    public void RemoveUser(string userId)
    {
        _userInfos.TryRemove(userId, out _);
    }

    public OneOf<Success, BusinessFailure> RegisterUserInRoom(string userId, string roomId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        if (userInfo is null)
        {
            throw new InvalidOperationException(nameof(userInfo));
        }

        return userInfo.AddRoom(roomId);
    }

    public OneOf<Success, BusinessFailure> RemoveUserOfRoom(string userId, string roomId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        if (userInfo is null)
        {
            throw new InvalidOperationException(nameof(userInfo));
        }

        return userInfo.RemoveRoom(roomId);
    }

    public string[] GetRoomsOfUser(string userId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        if (userInfo is null)
        {
            throw new InvalidOperationException(nameof(userInfo));
        }

        return userInfo.Rooms.ToArray();
    }

    public UserInformation GetUser(string userId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        if (userInfo is null)
        {
            throw new InvalidOperationException(nameof(userInfo));
        }

        return userInfo;
    }
}
