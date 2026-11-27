using System.Diagnostics;
using SharpQuill.Application.Articles.Suggestions;
using SharpQuill.Core.Articles.Suggestions;
using Npgsql;

namespace SharpQuill.Api.Infrastructure;

/// <summary>
/// Keeps the in-memory search-suggestion index (<see cref="SuggestionIndexHolder"/>) current. It loads everything at
/// startup, then listens on the Postgres channel <c>cms_suggest</c>. The triggers of migration AddSuggestionNotify
/// send a payload such as <c>article:42</c> there on every relevant write. For each batch of payloads the listener
/// reads the current rows of just those ids and swaps in an updated index.
/// <para>
/// LISTEN runs before the full load, so a change made during the load still arrives afterwards. Re-applying it is
/// harmless, because a delta always reads the current row. The listener reconnects with backoff if its connection
/// drops, and then does a full reload, because notifications sent while it was disconnected are lost. A full reload
/// also runs every <see cref="FullReloadInterval"/> as a safety net.
/// </para>
/// </summary>
public class SuggestionIndexListener : BackgroundService
{
    public const string Channel = "cms_suggest";

    /// <summary>How long to keep collecting notifications after the first one, so a bulk write becomes one batch.</summary>
    private static readonly TimeSpan CoalesceDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FullReloadInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly NpgsqlDataSource _dataSource;
    private readonly ISuggestionIndexRepository _repository;
    private readonly SuggestionIndexHolder _holder;
    private readonly ILogger<SuggestionIndexListener> _logger;

    public SuggestionIndexListener(
        NpgsqlDataSource dataSource,
        ISuggestionIndexRepository repository,
        SuggestionIndexHolder holder,
        ILogger<SuggestionIndexListener> logger)
    {
        _dataSource = dataSource;
        _repository = repository;
        _holder = holder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var backoff = TimeSpan.FromSeconds(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
                var pending = new PendingChanges();
                connection.Notification += (_, e) => pending.Add(e.Payload);

                await using (var listen = new NpgsqlCommand($"LISTEN {Channel}", connection))
                {
                    await listen.ExecuteNonQueryAsync(stoppingToken);
                }

                await FullReloadAsync(stoppingToken);
                backoff = TimeSpan.FromSeconds(1);
                var nextFullReload = DateTime.UtcNow + FullReloadInterval;

                while (!stoppingToken.IsCancellationRequested)
                {
                    var untilFullReload = nextFullReload - DateTime.UtcNow;
                    if (untilFullReload <= TimeSpan.Zero)
                    {
                        await FullReloadAsync(stoppingToken);
                        pending.Clear();
                        nextFullReload = DateTime.UtcNow + FullReloadInterval;
                        continue;
                    }

                    // Notifications are raised (into `pending`) while WaitAsync runs, including ones queued meanwhile.
                    if (!await connection.WaitAsync(untilFullReload, stoppingToken) && pending.IsEmpty)
                    {
                        continue;
                    }

                    var collectUntil = DateTime.UtcNow + CoalesceDelay;
                    for (var left = CoalesceDelay; left > TimeSpan.Zero; left = collectUntil - DateTime.UtcNow)
                    {
                        if (!await connection.WaitAsync(left, stoppingToken))
                        {
                            break;
                        }
                    }

                    await ApplyAsync(pending, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search suggestion listener failed; reconnecting in {Delay}", backoff);
                try
                {
                    await Task.Delay(backoff, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }
    }

    private async Task FullReloadAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = await _repository.LoadAllAsync(cancellationToken);
        var index = SuggestionIndex.Build(data);
        _holder.Replace(index);
        _logger.LogInformation(
            "Search suggestion index loaded: {Articles} articles, {Terms} tags/categories/series in {Elapsed} ms",
            index.ArticleCount, index.TermCount, stopwatch.ElapsedMilliseconds);
    }

    private async Task ApplyAsync(PendingChanges pending, CancellationToken cancellationToken)
    {
        if (pending.ReloadRequested)
        {
            await FullReloadAsync(cancellationToken);
            pending.Clear();
            return;
        }

        var articleIds = pending.Ids(SuggestionKind.Article);
        var articles = await _repository.LoadArticlesAsync(articleIds, cancellationToken);

        var changedTerms = new Dictionary<SuggestionKind, IReadOnlyCollection<SuggestionTerm>>();
        var deletedTerms = new Dictionary<SuggestionKind, IReadOnlyCollection<int>>();
        foreach (var kind in new[] { SuggestionKind.Tag, SuggestionKind.Category, SuggestionKind.Series })
        {
            var ids = pending.Ids(kind);
            if (ids.Count == 0)
            {
                continue;
            }
            var terms = await _repository.LoadTermsAsync(kind, ids, cancellationToken);
            changedTerms[kind] = terms;
            deletedTerms[kind] = ids.Except(terms.Select(t => t.Id)).ToList();
        }

        var delta = new SuggestionDelta
        {
            ChangedArticles = articles,
            DeletedArticleIds = articleIds.Except(articles.Select(a => a.Id)).ToList(),
            ChangedTerms = changedTerms,
            DeletedTermIds = deletedTerms
        };
        _holder.Replace(_holder.Current.Apply(delta));
        _logger.LogDebug("Search suggestion index updated: {Count} change(s)", pending.Count);
        pending.Clear();
    }

    /// <summary>The ids named by the notifications received since the last update, by kind.</summary>
    private sealed class PendingChanges
    {
        private readonly Dictionary<SuggestionKind, HashSet<int>> _ids = new();

        public bool ReloadRequested { get; private set; }

        public bool IsEmpty => !ReloadRequested && _ids.Values.All(s => s.Count == 0);

        public int Count => _ids.Values.Sum(s => s.Count);

        public void Add(string payload)
        {
            var separator = payload.IndexOf(':');
            var kindName = separator < 0 ? payload : payload[..separator];
            if (kindName == "reload")
            {
                ReloadRequested = true;
                return;
            }

            SuggestionKind? kind = kindName switch
            {
                "article" => SuggestionKind.Article,
                "tag" => SuggestionKind.Tag,
                "category" => SuggestionKind.Category,
                "series" => SuggestionKind.Series,
                _ => null
            };

            if (kind is null || !int.TryParse(payload.AsSpan(separator + 1), out var id))
            {
                // Unknown payloads are not ours to interpret; a full reload covers anything we might miss.
                ReloadRequested = true;
                return;
            }

            if (!_ids.TryGetValue(kind.Value, out var set))
            {
                set = [];
                _ids[kind.Value] = set;
            }
            set.Add(id);
        }

        public IReadOnlyCollection<int> Ids(SuggestionKind kind) =>
            _ids.TryGetValue(kind, out var set) ? set.ToList() : [];

        public void Clear()
        {
            ReloadRequested = false;
            _ids.Clear();
        }
    }
}
