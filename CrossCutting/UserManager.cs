using Domain;
using OneOf;
using OneOf.Types;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using VSlices.Core.Abstracts.Responses;
using Microsoft.AspNetCore.Connections.Features;
using System;

namespace CrossCutting;

public class UserManager
{
    public string Id { get; } = Guid.NewGuid().ToString();

    private readonly HashSet<string> ConnectionsToDisconnect = new();
    private readonly object ConnectionsToDisconnectLock = new();
    private readonly ConcurrentDictionary<string, UserInformation> _userInfos = new();

    public void DisconnectClient(string connectionId)
    {
        lock (ConnectionsToDisconnectLock)
        {
            if (ConnectionsToDisconnect.Contains(connectionId)) return;

            ConnectionsToDisconnect.Add(connectionId);
        }
    }

    public OneOf<Success, BusinessFailure> RegisterUserWithContext(string userId, HubCallerContext context)
    {
        var heartbeatFeature = context.Features.Get<IConnectionHeartbeatFeature>();

        if (heartbeatFeature is null)
        {
            throw new InvalidOperationException(nameof(heartbeatFeature));
        }

        heartbeatFeature.OnHeartbeat(state =>
        {
            lock (ConnectionsToDisconnectLock)
            {
                if (!ConnectionsToDisconnect.Contains(context.ConnectionId)) return;

                context.Abort();
                ConnectionsToDisconnect.Remove(context.ConnectionId);
            }

        }, context.ConnectionId);

        return _userInfos.TryAdd(userId, new UserInformation(userId, context.ConnectionId))
            ? new Success()
            : BusinessFailure.Of.NotAllowedUser();
    }

    public void RemoveUser(string userId)
    {
        _userInfos.TryRemove(userId, out _);
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

    public UserInformation? GetUserOrDefault(string userId)
    {
        _userInfos.TryGetValue(userId, out var userInfo);

        return userInfo;
    }

    public UserInformation GetUser(string userId)
    {
        return GetUserOrDefault(userId) ?? throw new InvalidOperationException(nameof(userId));
    }
}
