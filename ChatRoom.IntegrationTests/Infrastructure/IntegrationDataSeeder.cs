using Bogus;
using ChatRoom.Api.Data;
using ChatRoom.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.IntegrationTests.Infrastructure;

public static class IntegrationDataSeeder
{
    public static async Task SeedLargeAsync(ChatRoomDbContext db)
    {
        if (await db.Rooms.AnyAsync())
        {
            return;
        }

        var random = new Random(42);

        var roomFaker = new Faker<Room>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.Name, f => f.Commerce.Department())
            .RuleFor(x => x.Description, f => f.Lorem.Sentence(10))
            .RuleFor(x => x.CreatedBy, _ => Guid.NewGuid())
            .RuleFor(x => x.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
            .RuleFor(x => x.IsPrivate, f => f.Random.Bool(0.2f))
            .RuleFor(x => x.MaxMembers, f => f.Random.Int(20, 100));

        var rooms = roomFaker.Generate(150);

        var members = new List<Member>();
        foreach (var room in rooms)
        {
            var admin = new Member
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                UserId = room.CreatedBy,
                JoinedAt = room.CreatedAt,
                Role = MemberRole.Admin
            };

            members.Add(admin);

            var extraCount = random.Next(5, 25);
            for (var i = 0; i < extraCount; i++)
            {
                members.Add(new Member
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    UserId = Guid.NewGuid(),
                    JoinedAt = room.CreatedAt.AddMinutes(i + 1),
                    Role = MemberRole.Member
                });
            }
        }

        var roomMembers = members.GroupBy(x => x.RoomId).ToDictionary(x => x.Key, x => x.ToList());

        var messages = new List<Message>();
        foreach (var room in rooms)
        {
            var users = roomMembers[room.Id].Select(x => x.UserId).ToArray();
            var messageCount = random.Next(45, 85);
            for (var i = 0; i < messageCount; i++)
            {
                var sentAt = room.CreatedAt.AddMinutes(i + 1);
                messages.Add(new Message
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    UserId = users[random.Next(users.Length)],
                    Content = $"seed-message-{room.Id:N}-{i}",
                    SentAt = sentAt,
                    IsEdited = false,
                    EditedAt = null
                });
            }
        }

        while (messages.Count < 8200)
        {
            var room = rooms[random.Next(rooms.Count)];
            var users = roomMembers[room.Id].Select(x => x.UserId).ToArray();
            messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                UserId = users[random.Next(users.Length)],
                Content = $"seed-extra-{Guid.NewGuid():N}",
                SentAt = DateTime.UtcNow.AddSeconds(-random.Next(1, 1_000_000)),
                IsEdited = false
            });
        }

        db.Rooms.AddRange(rooms);
        db.Members.AddRange(members);
        db.Messages.AddRange(messages);
        await db.SaveChangesAsync();
    }
}
