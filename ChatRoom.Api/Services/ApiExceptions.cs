namespace ChatRoom.Api.Services;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ForbiddenException(string message) : Exception(message);

public sealed class BadRequestException(string message) : Exception(message);
