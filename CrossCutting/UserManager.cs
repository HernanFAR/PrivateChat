using Domain;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using VSlices.Core.Abstracts.Responses;

namespace CrossCutting;

public class UserManager
{
    private readonly ConcurrentDictionary<string, UserInfo> _userInfos = new();
    
    public UserInfo[] Users => _userInfos.Values.ToArray();

    public Response<Success> RegisterUser(string userId, string name, DateTimeOffset created)
    {
        return _userInfos.TryAdd(userId, new UserInfo(userId, name, created))
            ? new Success()
            : BusinessFailure.Of.UserNotAllowed();
    }

    public Response<Success> Remove(string userId)
    {
        var removed = _userInfos.TryRemove(userId, out _);

        return removed
            ? new Success()
            : BusinessFailure.Of.NotFoundResource();
    }

    public Response<string[]> GetRoomsOfUser(string userId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        return userInfo is null 
            ? BusinessFailure.Of.NotFoundResource() 
            : userInfo.Rooms.ToArray();
    }

    public Response<UserInfo> Get(string userId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        return userInfo is null
            ? BusinessFailure.Of.NotFoundResource()
            : userInfo;
    }

    public Response<Success> UpdateConnectionIdOfUser(string getNameIdentifier, HubCallerContext context)
    {
        var response = Get(getNameIdentifier);

        if (response.IsFailure) return BusinessFailure.Of.NotFoundResource();

        response.SuccessValue.UpdateConnectionId(context.ConnectionId);

        return new Success();
    }
}
