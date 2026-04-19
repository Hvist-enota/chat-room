using ChatRoom.Api.Data;
using ChatRoom.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=chat_room;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ChatRoomDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

public partial class Program
{
}
