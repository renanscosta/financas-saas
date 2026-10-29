using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Categories;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using EntityCategory = FinancasSaaS.Finance.Infrastructure.Persistence.Entities.Category;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancasSaaS.Finance.Infrastructure.Categories;

public sealed class CategoryStore(ITenantFinanceContextFactory contextFactory) : ICategoryStore
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categories = await db.Categories
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return categories.Select(item => Category.Create(item.Id, item.Name)).ToArray();
    }

    public async Task<Category?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        return category is null ? null : Category.Create(category.Id, category.Name);
    }

    public async Task<Category> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var category = Category.Create(Guid.NewGuid(), name);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.Categories.Add(new EntityCategory { Id = category.Id, Name = category.Name, IsActive = true });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueNameViolation(exception))
        {
            throw new CategoryNameConflictException("Já existe uma categoria com esse nome neste tenant.", exception);
        }

        return category;
    }

    public async Task<Category?> RenameAsync(Guid categoryId, string name, CancellationToken cancellationToken = default)
    {
        var category = Category.Create(categoryId, name);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = category.Name;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueNameViolation(exception))
        {
            throw new CategoryNameConflictException("Já existe uma categoria com esse nome neste tenant.", exception);
        }

        return category;
    }

    public async Task<CategoryDeleteResult> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "LOCK TABLE \"Categories\" IN SHARE ROW EXCLUSIVE MODE",
            cancellationToken);

        var entity = await db.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (entity is null)
        {
            return CategoryDeleteResult.NotFound;
        }

        if (await db.ManualTransactions.AnyAsync(item => item.CategoryId == categoryId, cancellationToken))
        {
            return CategoryDeleteResult.InUse;
        }

        if (await db.Categories.CountAsync(cancellationToken) <= 1)
        {
            return CategoryDeleteResult.LastCategory;
        }

        db.Categories.Remove(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CategoryDeleteResult.Deleted;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return CategoryDeleteResult.LastCategory;
        }
    }

    private static bool IsUniqueNameViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
