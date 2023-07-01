using Fluxor;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.Store.Messages;

[FeatureState] 
public class MessagesState
{
    public RoomMessages[] RoomMessages { get; }

    private MessagesState()
    {
        RoomMessages = Array.Empty<RoomMessages>();
    }

    public MessagesState(RoomMessages[] roomMessages)
    {
        RoomMessages = roomMessages;
    }
}

public record IncomingMessageAction(string FromName, string FromId, string RoomId, string Message);

public static class MessagesReducers
{
    [ReducerMethod]
    public static MessagesState AddIncomingMessage(MessagesState messagesState, IncomingMessageAction action)
    {
        var messages = messagesState.RoomMessages
            .Select(rm => rm.RoomId == action.RoomId 
                ? rm with { Messages = rm.Messages.Append(new MessageInformation(action.FromName, action.FromId, action.Message)).ToArray() } 
                : rm)
            .ToArray();

        return new MessagesState(messages);
    }
}