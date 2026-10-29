using System.Security.Claims;
using System.Diagnostics;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class FinanceSecurityLoggingTests
{
    [Fact]
    public async Task TenantSemAcessoGeraAvisoSemDadosDoTenant()
    {
        var actorId = Guid.NewGuid();
        var logger = new RecordingLogger<TenantAccessAuthorizationHandler>();
        var handler = new TenantAccessAuthorizationHandler(
            new FixedResolver(null),
            new FixedClock(),
            logger);
        var context = CreateContext(actorId);
        using var activity = new Activity("tenant-denied").Start();

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(actorId, entry.Properties["ActorId"]);
        Assert.Equal(DateTimeOffset.UnixEpoch, entry.Properties["OccurredAtUtc"]);
        Assert.Equal(activity.TraceId.ToString(), entry.Properties["CorrelationId"]);
        Assert.DoesNotContain("tenant-secret", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FalhaDeResolucaoGeraErroEContinuaPropagada()
    {
        var actorId = Guid.NewGuid();
        var exception = new InvalidOperationException("connection string secreta");
        var logger = new RecordingLogger<TenantAccessAuthorizationHandler>();
        var handler = new TenantAccessAuthorizationHandler(
            new FixedResolver(exception),
            new FixedClock(),
            logger);

        var propagated = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(CreateContext(actorId)));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(actorId, entry.Properties["ActorId"]);
        Assert.Equal(DateTimeOffset.UnixEpoch, entry.Properties["OccurredAtUtc"]);
        Assert.Same(exception, entry.Exception);
        Assert.DoesNotContain("connection string secreta", entry.Message, StringComparison.Ordinal);
        Assert.Same(exception, propagated);
    }

    private static AuthorizationHandlerContext CreateContext(Guid userId) => new(
        [new TenantAccessRequirement()],
        new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test")),
        null);

    private sealed class FixedResolver(Exception? exception) : ITenantConnectionResolver
    {
        public Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult<string?>(null);
        }

        public Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(item => item.Key, item => item.Value)
                : [];
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), properties, exception));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> Properties,
        Exception? Exception);
}
