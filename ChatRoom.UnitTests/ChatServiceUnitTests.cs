using AutoFixture;
using ChatRoom.Api.Contracts;
using ChatRoom.Api.Data;
using ChatRoom.Api.Domain;
using ChatRoom.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.UnitTests;

public sealed class ChatServiceUnitTests
{
    private static ChatRoomDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ChatRoomDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ChatRoomDbContext(options);
    }

    [Fact]
    public async Task SendMessage_ShouldFail_WhenUserIsNotMember()
    {
        await using var db = CreateContext();
        var fixture = new Fixture();

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IsPrivate = false,
            MaxMembers = 10
        };

        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var service = new ChatService(db);

        var action = () => service.SendMessageAsync(
            room.Id,
            Guid.NewGuid(),
            new SendMessageRequest("Hello"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(action);
    }

    [Fact]
    public async Task EditMessage_ShouldSetEditFlags_ForAuthor()
    {
        await using var db = CreateContext();
        var fixture = new Fixture();

        var userId = Guid.NewGuid();
        var room = await SeedRoomWithCreatorAsync(db, userId);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = userId,
            Content = fixture.Create<string>(),
            SentAt = fixture.Create<DateTime>(),
            IsEdited = false,
            EditedAt = null
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var service = new ChatService(db);
        var updated = await service.EditMessageAsync(message.Id, userId, new EditMessageRequest("Updated"), CancellationToken.None);

        Assert.True(updated.IsEdited);
        Assert.NotNull(updated.EditedAt);
    }

    [Fact]
    public async Task EditMessage_ShouldFail_WhenNotAuthor()
    {
        await using var db = CreateContext();
        var room = await SeedRoomWithCreatorAsync(db, Guid.NewGuid());

        var message = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = Guid.NewGuid(),
            Content = "Original",
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            EditedAt = null
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var service = new ChatService(db);

        var action = () => service.EditMessageAsync(message.Id, Guid.NewGuid(), new EditMessageRequest("Blocked"), CancellationToken.None);
        await Assert.ThrowsAsync<ForbiddenException>(action);
    }

    [Fact]
    public async Task DeleteMessage_ShouldWork_ForRoomAdmin()
    {
        await using var db = CreateContext();

        var adminId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var room = await SeedRoomWithCreatorAsync(db, adminId);

        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = authorId,
            JoinedAt = DateTime.UtcNow,
            Role = MemberRole.Member
        });

        var message = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = authorId,
            Content = "Text",
            SentAt = DateTime.UtcNow,
            IsEdited = false
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var service = new ChatService(db);
        await service.DeleteMessageAsync(message.Id, adminId, CancellationToken.None);

        Assert.False(await db.Messages.AnyAsync(x => x.Id == message.Id));
    }

    [Fact]
    public async Task JoinRoom_ShouldFail_WhenMaxMembersReached()
    {
        await using var db = CreateContext();

        var ownerId = Guid.NewGuid();
        var room = await SeedRoomWithCreatorAsync(db, ownerId, maxMembers: 1);
        var service = new ChatService(db);

        var action = () => service.JoinRoomAsync(room.Id, Guid.NewGuid(), CancellationToken.None);
        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    private static async Task<Room> SeedRoomWithCreatorAsync(ChatRoomDbContext db, Guid creatorId, int maxMembers = 10)
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Room",
            Description = "Desc",
            CreatedBy = creatorId,
            CreatedAt = DateTime.UtcNow,
            IsPrivate = false,
            MaxMembers = maxMembers
        };

        db.Rooms.Add(room);
        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = creatorId,
            JoinedAt = DateTime.UtcNow,
            Role = MemberRole.Admin
        });

        await db.SaveChangesAsync();
        return room;
    }
}
