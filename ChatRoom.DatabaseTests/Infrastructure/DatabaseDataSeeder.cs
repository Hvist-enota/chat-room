using Bogus;
using ChatRoom.Api.Data;
using ChatRoom.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.DatabaseTests.Infrastructure;

public static class DatabaseDataSeeder
{
    public static async Task SeedLargeAsync(ChatRoomDbContext db)
    {
        if (await db.Rooms.AnyAsync())
        {
            return;
        }

        var faker = new Faker("en");
        var random = new Random(12);

        var rooms = new List<Room>();
        var members = new List<Member>();
        var messages = new List<Message>();

        for (var r = 0; r < 160; r++)
        {
            var creatorId = Guid.NewGuid();
            var room = new Room
            {
                Id = Guid.NewGuid(),
                Name = faker.Commerce.ProductName(),
                Description = faker.Lorem.Sentence(12),
                
                CreatedBy = creatorId,

                CreatedAt = faker.Date.PastOffset(2).UtcDateTime,
                IsPrivate = faker.Random.Bool(0.3f),
                MaxMembers = faker.Random.Int(20, 80)
            };

            rooms.Add(room);

            members.Add(new Member
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                UserId = creatorId,
                JoinedAt = room.CreatedAt,
                Role = MemberRole.Admin
            });

            var memberCount = random.Next(6, 22);
            for (var i = 0; i < memberCount; i++)
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

        var byRoom = members.GroupBy(x => x.RoomId).ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var room in rooms)
        {
            var roomUsers = byRoom[room.Id].Select(x => x.UserId).ToArray();
            var count = random.Next(35, 95);
            for (var i = 0; i < count; i++)
            {
                messages.Add(new Message
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    UserId = roomUsers[random.Next(roomUsers.Length)],
                    Content = faker.Lorem.Sentence(8),
                    SentAt = room.CreatedAt.AddSeconds(i),
                    IsEdited = false,
                    EditedAt = null
                });
            }
        }

        while (rooms.Count + members.Count + messages.Count < 11_000)
        {
            var room = rooms[random.Next(rooms.Count)];
            var roomUsers = byRoom[room.Id].Select(x => x.UserId).ToArray();
            messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                UserId = roomUsers[random.Next(roomUsers.Length)],
                Content = faker.Lorem.Sentence(10),
                SentAt = DateTime.UtcNow.AddSeconds(-random.Next(1, 900_000)),
                IsEdited = false,
                EditedAt = null
            });
        }

        db.Rooms.AddRange(rooms);
        db.Members.AddRange(members);
        db.Messages.AddRange(messages);
        await db.SaveChangesAsync();
    }
}
