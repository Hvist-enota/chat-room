using ChatRoom.Api.Contracts;
using ChatRoom.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatRoom.Api.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController(ChatService chatService, IUserContext userContext) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MessageResponse>> Edit(Guid id, [FromBody] EditMessageRequest request, CancellationToken ct)
    {
        var response = await chatService.EditMessageAsync(id, userContext.GetRequiredUserId(), request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await chatService.DeleteMessageAsync(id, userContext.GetRequiredUserId(), ct);
        return NoContent();
    }
}
