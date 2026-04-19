# ChatRoom (Task 25)

ASP.NET Core Web API for real-time style chat room workflows with PostgreSQL, role-based permissions, cursor pagination, and automated tests.

## Implemented Scope

- Entities: Room, Member, Message
- Endpoints:
  - `GET /api/rooms`
  - `POST /api/rooms`
  - `GET /api/rooms/{id}/messages?cursor={id}&limit={n}`
  - `POST /api/rooms/{id}/messages`
  - `PUT /api/messages/{id}`
  - `DELETE /api/messages/{id}`
  - `POST /api/rooms/{id}/join`
  - `POST /api/rooms/{id}/leave`
  - `GET /api/rooms/{id}/members`
- Business rules:
  - only members can send/read messages
  - only author can edit message
  - only author or admin can delete message
  - creator becomes admin automatically
  - MaxMembers enforced
  - edited messages store `IsEdited = true` and `EditedAt`

## Tech Stack

- .NET 8, ASP.NET Core Web API
- EF Core + PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- xUnit
- Testcontainers (`Testcontainers.PostgreSql`)
- AutoFixture and Bogus for data generation
- k6 for performance tests

## Local Run

1. Start PostgreSQL:

```bash
docker compose up -d
```

2. Run API:

```bash
dotnet run --project ChatRoom.Api
```

3. Use request header `X-User-Id` with a valid GUID for protected endpoints.

## Tests

Run all tests:

```bash
dotnet test ChatRoom.slnx
```

Included test categories:

- Unit: permissions, max members, edit tracking
- Integration (WebApplicationFactory): room lifecycle, message CRUD, access control
- Database (Testcontainers): cursor pagination, role persistence, message ordering

## Performance (k6)

Scripts in [k6](k6):

- `k6/get-messages-pagination.js`
- `k6/concurrent-send-messages.js`

Example run:

```bash
k6 run -e BASE_URL=http://localhost:5189 -e ROOM_ID=<room-guid> -e USER_ID=<user-guid> k6/get-messages-pagination.js
```

## CI

GitHub Actions workflow: `.github/workflows/ci.yml`

It runs restore, build, and all tests on every push and pull request.

## GitHub Process (per assignment)

- Keep this repository public
- Create a dedicated Pull Request for this task
- Ensure CI passes for every push/PR
