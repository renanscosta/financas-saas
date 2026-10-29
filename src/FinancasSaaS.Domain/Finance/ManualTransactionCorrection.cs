namespace FinancasSaaS.Domain.Finance;

public sealed record ManualTransactionCorrection(
    ManualTransactionType? Type,
    string? Description,
    decimal? Amount,
    Guid? CategoryId,
    DateOnly? CompetenceDate,
    DateOnly? ExpectedDate,
    string Reason);
