using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Application.Finance.InstallmentPurchases;

public sealed record InstallmentPurchaseInstallmentRecord(
    InstallmentPurchaseInstallment Installment,
    string CategoryName);

public sealed record InstallmentPurchaseRecord(
    InstallmentPurchase Purchase,
    string CategoryName,
    IReadOnlyList<InstallmentPurchaseInstallmentRecord> Installments);
