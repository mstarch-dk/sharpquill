using Npgsql;

namespace SharpQuill.Api.Infrastructure;

/// <summary>
/// Periodically deletes expired refresh and login tokens so the token tables do not grow without bound.
/// </summary>
public class TokenCleanupService : BackgroundService
{
    public static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeProvider _timeProvider;

    public TokenCleanupService(NpgsqlDataSource dataSource, ILogger<TokenCleanupService> logger, TimeProvider timeProvider)
    {
        _dataSource = dataSource;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup (migrations, backfills) finish before the first run
        await Task.Delay(StartupDelay, _timeProvider, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removed = await PurgeAsync(stoppingToken);
                _logger.LogInformation("Token cleanup removed {Count} expired tokens", removed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Token cleanup failed");
            }

            await Task.Delay(Interval, _timeProvider, stoppingToken);
        }
    }

    /// <summary>Deletes tokens that expired more than a day ago. Returns the number of rows removed.</summary>
    public async Task<int> PurgeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM refresh_tokens WHERE expires_at < now() - interval '1 day';
            DELETE FROM website_refresh_tokens WHERE expires_at < now() - interval '1 day';
            DELETE FROM website_login_tokens WHERE expires_at < now() - interval '1 day';";

        await using var command = _dataSource.CreateCommand(sql);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
