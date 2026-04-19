using ChatRoom.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ChatRoom.DatabaseTests.Infrastructure;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("chat_room_db_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await DatabaseDataSeeder.SeedLargeAsync(db);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public ChatRoomDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChatRoomDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ChatRoomDbContext(options);
    }
}
