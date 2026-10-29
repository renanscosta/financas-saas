using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Application.Abstractions;

public interface ICategoryStore
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Category> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<Category?> RenameAsync(Guid categoryId, string name, CancellationToken cancellationToken = default);
    Task<CategoryDeleteResult> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public enum CategoryDeleteResult
{
    Deleted,
    NotFound,
    InUse,
    LastCategory
}
