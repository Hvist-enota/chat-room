namespace ChatRoom.DatabaseTests.Infrastructure;

public static class TestTime
{
    public static DateTime UtcNow() => DateTime.UtcNow;

    public static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
