namespace FinancasSaaS.Application.Finance.Categories;

public sealed class CategoryNameConflictException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
