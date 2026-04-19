namespace ChatRoom.Api.Contracts;

public sealed record SendMessageRequest(string Content);

public sealed record EditMessageRequest(string Content);

public sealed record MessageResponse(
    Guid Id,
    Guid RoomId,
    Guid UserId,
    string Content,
    DateTime SentAt,
    bool IsEdited,
    DateTime? EditedAt);

public sealed record PagedMessagesResponse(IReadOnlyCollection<MessageResponse> Items, Guid? NextCursor);
