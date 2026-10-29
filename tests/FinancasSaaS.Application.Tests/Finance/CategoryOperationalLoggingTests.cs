using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Categories;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Application.Tests.Finance;

public sealed class CategoryOperationalLoggingTests
{
    [Fact]
    public async Task CriacaoRegistraResultadoEContextoConfiavelSemRegistrarNome()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var logger = new RecordingLogger<CategoryService>();
        var store = new TestCategoryStore();
        var service = new CategoryService(store, new FixedUserContext(actorId, tenantId), new FixedClock(), logger);

        await service.CreateAsync("Nome privado");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("category.create", entry.Properties["Operation"]);
        Assert.Equal("success", entry.Properties["Result"]);
        Assert.Equal(actorId, entry.Properties["ActorId"]);
        Assert.Equal(tenantId, entry.Properties["TenantId"]);
        Assert.Equal(DateTimeOffset.UnixEpoch, entry.Properties["OccurredAtUtc"]);
        Assert.DoesNotContain("Nome privado", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConflitoDeCategoriaERegistradoSemExporNome()
    {
        var logger = new RecordingLogger<CategoryService>();
        var service = new CategoryService(
            new TestCategoryStore { CreateException = new CategoryNameConflictException("duplicada") },
            new FixedUserContext(Guid.NewGuid(), Guid.NewGuid()),
            new FixedClock(),
            logger);

        await Assert.ThrowsAsync<CategoryNameConflictException>(() => service.CreateAsync("Categoria privada"));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("conflict", entry.Properties["Result"]);
        Assert.DoesNotContain("Categoria privada", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidOperationDuranteResolucaoDoContextoERegistradaComoFalhaNaoComoConflito()
    {
        var exception = new InvalidOperationException("O usuário não possui acesso a um tenant pronto.");
        var logger = new RecordingLogger<CategoryService>();
        var service = new CategoryService(
            new TestCategoryStore { CreateException = exception },
            new FixedUserContext(Guid.NewGuid(), Guid.NewGuid()),
            new FixedClock(),
            logger);

        var propagated = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync("Categoria privada"));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("category.create", entry.Properties["Operation"]);
        Assert.Same(exception, entry.Exception);
        Assert.Same(exception, propagated);
        Assert.DoesNotContain("Categoria privada", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExcecaoNaCriacaoERegistradaEPropagadaSemNome()
    {
        var logger = new RecordingLogger<CategoryService>();
        var exception = new IOException("falha técnica");
        var service = new CategoryService(
            new TestCategoryStore { CreateException = exception },
            new FixedUserContext(Guid.NewGuid(), Guid.NewGuid()),
            new FixedClock(),
            logger);

        var propagated = await Assert.ThrowsAsync<IOException>(() => service.CreateAsync("Nome privado"));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("category.create", entry.Properties["Operation"]);
        Assert.Same(exception, entry.Exception);
        Assert.Same(exception, propagated);
        Assert.DoesNotContain("Nome privado", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EdicaoEExclusaoRegistramResultadosSemRegistrarPayload()
    {
        var logger = new RecordingLogger<CategoryService>();
        var category = Category.Create(Guid.NewGuid(), "Nome privado");
        var store = new TestCategoryStore
        {
            RenameResult = category,
            DeleteResult = CategoryDeleteResult.Deleted
        };
        var service = new CategoryService(
            store,
            new FixedUserContext(Guid.NewGuid(), Guid.NewGuid()),
            new FixedClock(),
            logger);

        await service.RenameAsync(category.Id, "Outro nome privado");
        await service.DeleteAsync(category.Id);

        Assert.Collection(
            logger.Entries,
            rename =>
            {
                Assert.Equal("category.rename", rename.Properties["Operation"]);
                Assert.Equal("success", rename.Properties["Result"]);
                Assert.DoesNotContain("Outro nome privado", rename.Message, StringComparison.Ordinal);
            },
            delete =>
            {
                Assert.Equal("category.delete", delete.Properties["Operation"]);
                Assert.Equal(nameof(CategoryDeleteResult.Deleted), delete.Properties["Result"]);
                Assert.DoesNotContain("Nome privado", delete.Message, StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task FalhaDeEdicaoEExclusaoERegistradaEPropagada()
    {
        var logger = new RecordingLogger<CategoryService>();
        var exception = new IOException("falha técnica");
        var store = new TestCategoryStore { RenameException = exception, DeleteException = exception };
        var service = new CategoryService(
            store,
            new FixedUserContext(Guid.NewGuid(), Guid.NewGuid()),
            new FixedClock(),
            logger);

        await Assert.ThrowsAsync<IOException>(() => service.RenameAsync(Guid.NewGuid(), "Privado"));
        await Assert.ThrowsAsync<IOException>(() => service.DeleteAsync(Guid.NewGuid()));

        Assert.Collection(
            logger.Entries,
            rename =>
            {
                Assert.Equal(LogLevel.Error, rename.Level);
                Assert.Equal("category.rename", rename.Properties["Operation"]);
                Assert.Same(exception, rename.Exception);
            },
            delete =>
            {
                Assert.Equal(LogLevel.Error, delete.Level);
                Assert.Equal("category.delete", delete.Properties["Operation"]);
                Assert.Same(exception, delete.Exception);
            });
    }

    private sealed class FixedUserContext(Guid actorId, Guid tenantId) : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(actorId);

        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(tenantId);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class TestCategoryStore : ICategoryStore
    {
        public Exception? CreateException { get; init; }
        public Exception? RenameException { get; init; }
        public Exception? DeleteException { get; init; }
        public Category? RenameResult { get; init; }
        public CategoryDeleteResult DeleteResult { get; init; } = CategoryDeleteResult.NotFound;

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>([]);

        public Task<Category?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Category?>(null);

        public Task<Category> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(Category.Create(Guid.NewGuid(), name));
        }

        public Task<Category?> RenameAsync(
            Guid categoryId,
            string name,
            CancellationToken cancellationToken = default)
        {
            if (RenameException is not null)
            {
                throw RenameException;
            }

            return Task.FromResult(RenameResult);
        }

        public Task<CategoryDeleteResult> DeleteAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            return Task.FromResult(DeleteResult);
        }
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
        Exception? Exception = null);
}
