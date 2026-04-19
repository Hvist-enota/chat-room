using ChatRoom.Api.Contracts;
using ChatRoom.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatRoom.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(ChatService chatService, IUserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoomResponse>>> GetPublicRooms(CancellationToken ct)
    {
        var rooms = await chatService.GetPublicRoomsAsync(ct);
        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var room = await chatService.CreateRoomAsync(userContext.GetRequiredUserId(), request, ct);
        return CreatedAtAction(nameof(GetMembers), new { id = room.Id }, room);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<PagedMessagesResponse>> GetMessages(Guid id, [FromQuery] Guid? cursor, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var page = await chatService.GetMessagesAsync(id, userContext.GetRequiredUserId(), cursor, limit, ct);
        return Ok(page);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageResponse>> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var message = await chatService.SendMessageAsync(id, userContext.GetRequiredUserId(), request, ct);
        return Ok(message);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<JoinLeaveResponse>> Join(Guid id, CancellationToken ct)
    {
        var response = await chatService.JoinRoomAsync(id, userContext.GetRequiredUserId(), ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<ActionResult<JoinLeaveResponse>> Leave(Guid id, CancellationToken ct)
    {
        var response = await chatService.LeaveRoomAsync(id, userContext.GetRequiredUserId(), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyCollection<MemberResponse>>> GetMembers(Guid id, CancellationToken ct)
    {
        var members = await chatService.GetMembersAsync(id, userContext.GetRequiredUserId(), ct);
        return Ok(members);
    }
}
