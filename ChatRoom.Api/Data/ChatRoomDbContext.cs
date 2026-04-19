using ChatRoom.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.Api.Data;

public sealed class ChatRoomDbContext(DbContextOptions<ChatRoomDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.MaxMembers).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("ck_rooms_max_members", "\"MaxMembers\" > 0"));
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("members");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.JoinedAt).IsRequired();
            entity.Property(x => x.Role).IsRequired();
            entity.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Room)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.SentAt).IsRequired();
            entity.Property(x => x.IsEdited).IsRequired();
            entity.HasIndex(x => new { x.RoomId, x.SentAt, x.Id });
            entity.HasOne(x => x.Room)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
