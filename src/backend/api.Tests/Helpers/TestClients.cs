namespace SharpQuill.Api.Tests.Helpers;

public static class TestClients
{
    private static int _nextAddress;

    /// <summary>
    /// Gives the client its own X-Forwarded-For address. The per-IP rate limits (comments, likes, contact) are
    /// partitioned by client IP, and test requests otherwise all share one, so tests would use up each other's budget.
    /// </summary>
    public static HttpClient WithUniqueClientIp(this HttpClient client)
    {
        var n = Interlocked.Increment(ref _nextAddress);
        var address = $"10.{(n >> 16) & 0xFF}.{(n >> 8) & 0xFF}.{n & 0xFF}";
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", address);
        return client;
    }

    public static string ClientIp(this HttpClient client) =>
        client.DefaultRequestHeaders.GetValues("X-Forwarded-For").Single();
}
