namespace ChatRoom.Api.Domain;

public sealed class Room
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsPrivate { get; set; }

    public int MaxMembers { get; set; }

    public ICollection<Member> Members { get; set; } = new List<Member>();

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
