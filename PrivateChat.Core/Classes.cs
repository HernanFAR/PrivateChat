namespace PrivateChat.Core;

public record MessageInformation(string FromName, string FromId, string Message);

public record RoomMessages(string RoomId, MessageInformation[] Messages);

public record RoomInformation(string Id, int UnreadMessages);
