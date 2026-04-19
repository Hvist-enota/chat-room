namespace ChatRoom.Api.Domain;

public sealed class Member
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public MemberRole Role { get; set; }

    public Room Room { get; set; } = null!;
}
