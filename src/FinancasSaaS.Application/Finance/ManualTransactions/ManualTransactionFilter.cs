using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Application.Finance.ManualTransactions;

public sealed record ManualTransactionFilter(
    DateOnly StartDate,
    DateOnly EndDate,
    ManualTransactionType? Type = null,
    Guid? CategoryId = null,
    ManualTransactionStatus? Status = null);
