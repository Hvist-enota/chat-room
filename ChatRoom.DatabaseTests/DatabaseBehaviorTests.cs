using AutoFixture;
using ChatRoom.Api.Contracts;
using ChatRoom.Api.Domain;
using ChatRoom.Api.Services;
using ChatRoom.DatabaseTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.DatabaseTests;

public sealed class DatabaseBehaviorTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task CursorPagination_ShouldReturnOrderedNonOverlappingPages()
    {
        await using var db = fixture.CreateDbContext();
        var service = new ChatService(db);

        var room = await db.Rooms.AsNoTracking().FirstAsync();
        var member = await db.Members.AsNoTracking().FirstAsync(x => x.RoomId == room.Id);

        var page1 = await service.GetMessagesAsync(room.Id, member.UserId, null, 20, CancellationToken.None);
        Assert.Equal(20, page1.Items.Count);
        Assert.NotNull(page1.NextCursor);

        var page2 = await service.GetMessagesAsync(room.Id, member.UserId, page1.NextCursor, 20, CancellationToken.None);
        Assert.Equal(20, page2.Items.Count);

        var overlap = page1.Items.Select(x => x.Id).Intersect(page2.Items.Select(x => x.Id));
        Assert.Empty(overlap);

        var ordered = page1.Items.Zip(page1.Items.Skip(1), (a, b) => a.SentAt >= b.SentAt).All(x => x);
        Assert.True(ordered);
    }

    [Fact]
    public async Task MemberRoles_ShouldPersistAdminAndMember()
    {
        await using var db = fixture.CreateDbContext();

        var fixtureData = new Fixture();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = fixtureData.Create<string>(),
            Description = fixtureData.Create<string>(),
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            IsPrivate = false,
            MaxMembers = 5
        };

        db.Rooms.Add(room);
        db.Members.AddRange(
            new Member
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                UserId = adminId,
                JoinedAt = fixtureData.Create<DateTime>(),
                Role = MemberRole.Admin
            },
            new Member
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                UserId = memberId,
                JoinedAt = fixtureData.Create<DateTime>(),
                Role = MemberRole.Member
            });

        await db.SaveChangesAsync();

        var roles = await db.Members
            .AsNoTracking()
            .Where(x => x.RoomId == room.Id)
            .Select(x => x.Role)
            .ToListAsync();

        Assert.Contains(MemberRole.Admin, roles);
        Assert.Contains(MemberRole.Member, roles);
    }

    [Fact]
    public async Task MessageOrder_ShouldRemainDescendingBySentAtAndId()
    {
        await using var db = fixture.CreateDbContext();
        var service = new ChatService(db);

        var room = await db.Rooms.AsNoTracking().FirstAsync();
        var member = await db.Members.AsNoTracking().FirstAsync(x => x.RoomId == room.Id);

        var firstTime = DateTime.UtcNow;
        var sameTimestampContentA = new SendMessageRequest("same-time-a");
        var sameTimestampContentB = new SendMessageRequest("same-time-b");

        await service.SendMessageAsync(room.Id, member.UserId, sameTimestampContentA, CancellationToken.None);
        await Task.Delay(10);
        await service.SendMessageAsync(room.Id, member.UserId, sameTimestampContentB, CancellationToken.None);

        var page = await service.GetMessagesAsync(room.Id, member.UserId, null, 5, CancellationToken.None);

        var ordered = page.Items.Zip(page.Items.Skip(1), (a, b) =>
            a.SentAt > b.SentAt || (a.SentAt == b.SentAt && a.Id.CompareTo(b.Id) > 0)).All(x => x);

        Assert.True(ordered);
        Assert.NotEqual(default, firstTime);
    }
}
