﻿using Fluxor;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.Store.Rooms;

[FeatureState]
public class RoomsState
{
    public RoomInformation[] LoggedRooms { get; }

    private RoomsState()
    {
        LoggedRooms = Array.Empty<RoomInformation>();
    }

    public RoomsState(RoomInformation[] loggedRooms) : base()
    {
        LoggedRooms = loggedRooms;
    }
}

public record RoomIncomingMessageAction(string Id);
public record ClearUnreadMessagesAction(string Id);

public static class RoomsReducers
{
    [ReducerMethod]
    public static RoomsState IncomingMessageReducer(RoomsState state, RoomIncomingMessageAction action)
    {
        var room = state.LoggedRooms
            .Single(e => e.Id == action.Id);

        var otherRooms = state.LoggedRooms
            .Where(e => e.Id != action.Id);

        var newRoomInfo = room with { UnreadMessages = room.UnreadMessages + 1 };


        return new RoomsState(otherRooms.Prepend(newRoomInfo).ToArray());

    }

    [ReducerMethod]
    public static RoomsState ClearUnreadMessagesReducer(RoomsState state, ClearUnreadMessagesAction action)
    {
        var loggedRooms = state.LoggedRooms
            .Select(room => room.Id == action.Id ? room with { UnreadMessages = 0 } : room)
            .ToArray();
        
        return new RoomsState(loggedRooms);
    }
}

