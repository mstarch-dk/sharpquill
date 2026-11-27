using System.Collections.Concurrent;
using SharpQuill.Api.Infrastructure;
using SharpQuill.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// The background loop of TokenCleanupService on a fake clock: it waits for startup, purges once a day, logs a failed
/// run without stopping, and stops cleanly. The purge itself is tested in TokenLifecycleTests.
/// </summary>
[Collection("Database")]
public class TokenCleanupServiceTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Loop_PurgesAfterTheStartupDelayAndThenDaily()
    {
        var clock = new SignallingClock();
        var logger = new RecordingLogger();
        using var service = new TokenCleanupService(TestDatabase.DataSource, logger, clock);

        await service.StartAsync(CancellationToken.None);
        await clock.NextTimerAsync(Wait);
        clock.Advance(TokenCleanupService.StartupDelay - TimeSpan.FromSeconds(1));
        logger.Entries.Should().BeEmpty("nothing runs before startup has had time to finish");

        clock.Advance(TimeSpan.FromSeconds(1));
        (await logger.NextAsync(Wait)).Level.Should().Be(LogLevel.Information);

        await clock.NextTimerAsync(Wait);
        clock.Advance(TokenCleanupService.Interval);
        var second = await logger.NextAsync(Wait);
        second.Level.Should().Be(LogLevel.Information);
        second.Message.Should().StartWith("Token cleanup removed");

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Loop_LogsAFailedRunAndKeepsGoing()
    {
        // Nothing listens on port 1, so every purge fails to connect
        await using var unreachable = NpgsqlDataSource.Create("Host=127.0.0.1;Port=1;Timeout=2;Username=x;Password=x");
        var clock = new SignallingClock();
        var logger = new RecordingLogger();
        using var service = new TokenCleanupService(unreachable, logger, clock);

        await service.StartAsync(CancellationToken.None);
        await clock.NextTimerAsync(Wait);
        clock.Advance(TokenCleanupService.StartupDelay);
        var first = await logger.NextAsync(Wait);
        await clock.NextTimerAsync(Wait);
        clock.Advance(TokenCleanupService.Interval);
        var second = await logger.NextAsync(Wait);

        first.Level.Should().Be(LogLevel.Error);
        first.Exception.Should().NotBeNull();
        second.Level.Should().Be(LogLevel.Error, "a failed run does not end the loop");

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Stop_DuringTheStartupDelay_EndsWithoutPurging()
    {
        var clock = new SignallingClock();
        var logger = new RecordingLogger();
        using var service = new TokenCleanupService(TestDatabase.DataSource, logger, clock);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCompleted.Should().BeTrue();
        logger.Entries.Should().BeEmpty();
    }

    /// <summary>
    /// A fake clock that reports each timer it creates: ExecuteAsync runs on a background thread, so the test waits
    /// until the loop is actually inside Task.Delay before moving the clock.
    /// </summary>
    private sealed class SignallingClock : FakeTimeProvider
    {
        private readonly SemaphoreSlim _timerCreated = new(0);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _timerCreated.Release();
            return timer;
        }

        public async Task NextTimerAsync(TimeSpan timeout)
        {
            if (!await _timerCreated.WaitAsync(timeout))
                throw new TimeoutException("The cleanup loop started no delay");
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class RecordingLogger : ILogger<TokenCleanupService>
    {
        private readonly SemaphoreSlim _written = new(0);
        private int _read;

        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        /// <summary>The next entry not returned yet, waiting for it to be written.</summary>
        public async Task<LogEntry> NextAsync(TimeSpan timeout)
        {
            if (!await _written.WaitAsync(timeout))
                throw new TimeoutException("The cleanup loop wrote no log entry");
            return Entries.ElementAt(_read++);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
            _written.Release();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
