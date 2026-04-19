using ChatRoom.Api.Contracts;
using ChatRoom.Api.Data;
using ChatRoom.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.Api.Services;

public sealed class ChatService(ChatRoomDbContext dbContext)
{
    public async Task<IReadOnlyCollection<RoomResponse>> GetPublicRoomsAsync(CancellationToken ct)
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .Where(x => !x.IsPrivate)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RoomResponse(
                x.Id,
                x.Name,
                x.Description,
                x.CreatedBy,
                x.CreatedAt,
                x.IsPrivate,
                x.MaxMembers,
                x.Members.Count))
            .ToListAsync(ct);
    }

    public async Task<RoomResponse> CreateRoomAsync(Guid userId, CreateRoomRequest request, CancellationToken ct)
    {
        if (request.MaxMembers <= 0)
        {
            throw new BadRequestException("MaxMembers must be greater than zero.");
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            IsPrivate = request.IsPrivate,
            MaxMembers = request.MaxMembers
        };

        if (string.IsNullOrWhiteSpace(room.Name))
        {
            throw new BadRequestException("Room name is required.");
        }

        var creator = new Member
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Role = MemberRole.Admin
        };

        dbContext.Rooms.Add(room);
        dbContext.Members.Add(creator);
        await dbContext.SaveChangesAsync(ct);

        return new RoomResponse(
            room.Id,
            room.Name,
            room.Description,
            room.CreatedBy,
            room.CreatedAt,
            room.IsPrivate,
            room.MaxMembers,
            1);
    }

    public async Task<PagedMessagesResponse> GetMessagesAsync(Guid roomId, Guid userId, Guid? cursor, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 200)
        {
            throw new BadRequestException("Limit must be between 1 and 200.");
        }

        await EnsureMemberAsync(roomId, userId, ct);

        var query = dbContext.Messages
            .AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.SentAt)
            .ThenByDescending(x => x.Id)
            .AsQueryable();

        if (cursor.HasValue)
        {
            var cursorMessage = await dbContext.Messages
                .AsNoTracking()
                .Where(x => x.Id == cursor.Value && x.RoomId == roomId)
                .Select(x => new { x.SentAt, x.Id })
                .SingleOrDefaultAsync(ct);

            if (cursorMessage is null)
            {
                throw new BadRequestException("Cursor message not found for room.");
            }

            query = query.Where(x =>
                x.SentAt < cursorMessage.SentAt
                || (x.SentAt == cursorMessage.SentAt && x.Id.CompareTo(cursorMessage.Id) < 0));
        }

        var items = await query
            .Take(limit)
            .Select(x => new MessageResponse(
                x.Id,
                x.RoomId,
                x.UserId,
                x.Content,
                x.SentAt,
                x.IsEdited,
                x.EditedAt))
            .ToListAsync(ct);

        Guid? nextCursor = items.Count == limit ? items.Last().Id : null;
        return new PagedMessagesResponse(items, nextCursor);
    }

    public async Task<MessageResponse> SendMessageAsync(Guid roomId, Guid userId, SendMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BadRequestException("Message content is required.");
        }

        await EnsureMemberAsync(roomId, userId, ct);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            Content = request.Content.Trim(),
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            EditedAt = null
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        return new MessageResponse(
            message.Id,
            message.RoomId,
            message.UserId,
            message.Content,
            message.SentAt,
            message.IsEdited,
            message.EditedAt);
    }

    public async Task<MessageResponse> EditMessageAsync(Guid messageId, Guid userId, EditMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BadRequestException("Message content is required.");
        }

        var message = await dbContext.Messages.SingleOrDefaultAsync(x => x.Id == messageId, ct)
            ?? throw new NotFoundException("Message not found.");

        if (message.UserId != userId)
        {
            throw new ForbiddenException("Only message author can edit the message.");
        }

        message.Content = request.Content.Trim();
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return new MessageResponse(
            message.Id,
            message.RoomId,
            message.UserId,
            message.Content,
            message.SentAt,
            message.IsEdited,
            message.EditedAt);
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid userId, CancellationToken ct)
    {
        var message = await dbContext.Messages.SingleOrDefaultAsync(x => x.Id == messageId, ct)
            ?? throw new NotFoundException("Message not found.");

        var membership = await dbContext.Members
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RoomId == message.RoomId && x.UserId == userId, ct)
            ?? throw new ForbiddenException("Only room members can manage messages.");

        var canDelete = message.UserId == userId || membership.Role == MemberRole.Admin;
        if (!canDelete)
        {
            throw new ForbiddenException("Only author or room admin can delete this message.");
        }

        dbContext.Messages.Remove(message);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<JoinLeaveResponse> JoinRoomAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var room = await dbContext.Rooms.SingleOrDefaultAsync(x => x.Id == roomId, ct)
            ?? throw new NotFoundException("Room not found.");

        var existing = await dbContext.Members.SingleOrDefaultAsync(x => x.RoomId == roomId && x.UserId == userId, ct);
        if (existing is not null)
        {
            return new JoinLeaveResponse(roomId, userId, true);
        }

        var membersCount = await dbContext.Members.CountAsync(x => x.RoomId == roomId, ct);
        if (membersCount >= room.MaxMembers)
        {
            throw new BadRequestException("Room member limit reached.");
        }

        dbContext.Members.Add(new Member
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Role = MemberRole.Member
        });

        await dbContext.SaveChangesAsync(ct);
        return new JoinLeaveResponse(roomId, userId, true);
    }

    public async Task<JoinLeaveResponse> LeaveRoomAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var membership = await dbContext.Members.SingleOrDefaultAsync(x => x.RoomId == roomId && x.UserId == userId, ct)
            ?? throw new NotFoundException("Membership not found.");

        dbContext.Members.Remove(membership);
        await dbContext.SaveChangesAsync(ct);

        return new JoinLeaveResponse(roomId, userId, false);
    }

    public async Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        await EnsureMemberAsync(roomId, userId, ct);

        return await dbContext.Members
            .AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.JoinedAt)
            .Select(x => new MemberResponse(
                x.Id,
                x.RoomId,
                x.UserId,
                x.JoinedAt,
                x.Role.ToString()))
            .ToListAsync(ct);
    }

    private async Task EnsureMemberAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var roomExists = await dbContext.Rooms.AsNoTracking().AnyAsync(x => x.Id == roomId, ct);
        if (!roomExists)
        {
            throw new NotFoundException("Room not found.");
        }

        var isMember = await dbContext.Members
            .AsNoTracking()
            .AnyAsync(x => x.RoomId == roomId && x.UserId == userId, ct);

        if (!isMember)
        {
            throw new ForbiddenException("Only room members can perform this action.");
        }
    }
}
