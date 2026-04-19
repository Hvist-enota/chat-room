using System.Net;
using System.Net.Http.Json;
using ChatRoom.Api.Contracts;
using ChatRoom.IntegrationTests.Infrastructure;

namespace ChatRoom.IntegrationTests;

public sealed class ChatRoomApiIntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public ChatRoomApiIntegrationTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RoomLifecycle_ShouldCreateJoinLeaveAndListMembers()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", ownerId.ToString());

        var createResponse = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Integration Room", "Desc", false, 3));
        createResponse.EnsureSuccessStatusCode();

        var room = await createResponse.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(room);

        var memberId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", memberId.ToString());

        var joinResponse = await _client.PostAsync($"/api/rooms/{room!.Id}/join", null);
        joinResponse.EnsureSuccessStatusCode();

        var leaveResponse = await _client.PostAsync($"/api/rooms/{room.Id}/leave", null);
        leaveResponse.EnsureSuccessStatusCode();

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", ownerId.ToString());

        var membersResponse = await _client.GetAsync($"/api/rooms/{room.Id}/members");
        membersResponse.EnsureSuccessStatusCode();
        var members = await membersResponse.Content.ReadFromJsonAsync<List<MemberResponse>>();

        Assert.NotNull(members);
        Assert.Single(members!);
    }

    [Fact]
    public async Task MessageCrud_ShouldCreateEditAndDelete()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());

        var createRoom = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("CRUD", "Messages", false, 10));
        createRoom.EnsureSuccessStatusCode();
        var room = await createRoom.Content.ReadFromJsonAsync<RoomResponse>();

        var createMessage = await _client.PostAsJsonAsync($"/api/rooms/{room!.Id}/messages", new SendMessageRequest("first"));
        createMessage.EnsureSuccessStatusCode();
        var message = await createMessage.Content.ReadFromJsonAsync<MessageResponse>();

        var editMessage = await _client.PutAsJsonAsync($"/api/messages/{message!.Id}", new EditMessageRequest("edited"));
        editMessage.EnsureSuccessStatusCode();

        var edited = await editMessage.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.NotNull(edited);
        Assert.True(edited!.IsEdited);

        var deleteResponse = await _client.DeleteAsync($"/api/messages/{message.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AccessControl_ShouldBlockNonMemberFromReadingMessages()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", ownerId.ToString());

        var createResponse = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Private", "Room", false, 10));
        createResponse.EnsureSuccessStatusCode();
        var room = await createResponse.Content.ReadFromJsonAsync<RoomResponse>();

        var outsider = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", outsider.ToString());

        var messages = await _client.GetAsync($"/api/rooms/{room!.Id}/messages");
        Assert.Equal(HttpStatusCode.Forbidden, messages.StatusCode);
    }
}
