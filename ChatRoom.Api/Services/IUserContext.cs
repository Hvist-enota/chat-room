namespace ChatRoom.Api.Services;

public interface IUserContext
{
    Guid GetRequiredUserId();
}
