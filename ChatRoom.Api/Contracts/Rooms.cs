namespace ChatRoom.Api.Contracts;

public sealed record CreateRoomRequest(string Name, string Description, bool IsPrivate, int MaxMembers);

public sealed record RoomResponse(
    Guid Id,
    string Name,
    string Description,
    Guid CreatedBy,
    DateTime CreatedAt,
    bool IsPrivate,
    int MaxMembers,
    int MemberCount);

public sealed record JoinLeaveResponse(Guid RoomId, Guid UserId, bool IsMember);

public sealed record MemberResponse(Guid Id, Guid RoomId, Guid UserId, DateTime JoinedAt, string Role);
