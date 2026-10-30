using System.Globalization;

namespace FinancasSaaS.Domain.Finance;

public sealed record InstallmentPurchaseInstallment(
    Guid TransactionId,
    int Number,
    string Description,
    decimal Amount,
    Guid CategoryId,
    DateOnly DueDate,
    DateOnly CompetenceDate,
    ManualTransactionStatus Status,
    DateOnly? EffectiveDate,
    ManualTransactionStatus? StatusBeforeCancellation,
    long Version);

public sealed class InstallmentPurchase
{
    private readonly List<InstallmentPurchaseInstallment> installments;

    public Guid Id { get; }
    public string Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public int InstallmentCount { get; }
    public DateOnly OriginalFirstDueDate { get; }
    public DateTimeOffset CreatedAt { get; }
    public long Version { get; private set; }
    public IReadOnlyList<InstallmentPurchaseInstallment> Installments => installments.AsReadOnly();

    private InstallmentPurchase(
        Guid id,
        string description,
        Guid categoryId,
        decimal totalAmount,
        int installmentCount,
        DateOnly originalFirstDueDate,
        DateTimeOffset createdAt,
        long version,
        List<InstallmentPurchaseInstallment> installments)
    {
        Id = id;
        Description = description;
        CategoryId = categoryId;
        TotalAmount = totalAmount;
        InstallmentCount = installmentCount;
        OriginalFirstDueDate = originalFirstDueDate;
        CreatedAt = createdAt;
        Version = version;
        this.installments = installments;
    }

    public static InstallmentPurchase Create(
        Guid id,
        string description,
        Guid categoryId,
        decimal totalAmount,
        int installmentCount,
        DateOnly firstDueDate,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O identificador da compra é obrigatório.", nameof(id));
        }

        ValidateDescription(description);
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("A categoria é obrigatória.", nameof(categoryId));
        }

        var amounts = InstallmentAllocation.Allocate(totalAmount, installmentCount);
        ValidateUtc(createdAt);
        var normalizedDescription = description.Trim();
        var items = new List<InstallmentPurchaseInstallment>(installmentCount);
        for (var index = 0; index < installmentCount; index++)
        {
            var number = index + 1;
            var dueDate = InstallmentSchedule.GetDueDate(firstDueDate, number);
            items.Add(new InstallmentPurchaseInstallment(
                Guid.NewGuid(),
                number,
                normalizedDescription,
                amounts[index],
                categoryId,
                dueDate,
                FirstDayOfMonth(dueDate),
                ManualTransactionStatus.Planned,
                null,
                null,
                1));
        }

        return new InstallmentPurchase(
            id,
            normalizedDescription,
            categoryId,
            totalAmount,
            installmentCount,
            firstDueDate,
            createdAt,
            1,
            items);
    }

    public static InstallmentPurchase Restore(
        Guid id,
        string description,
        Guid categoryId,
        decimal totalAmount,
        int installmentCount,
        DateOnly originalFirstDueDate,
        DateTimeOffset createdAt,
        long version,
        IEnumerable<InstallmentPurchaseInstallment> installments)
    {
        ArgumentNullException.ThrowIfNull(installments);
        if (id == Guid.Empty || categoryId == Guid.Empty || version < 1)
        {
            throw new ArgumentException("A compra parcelada persistida possui identificadores ou versão inválidos.");
        }

        ValidateDescription(description);
        ManualTransaction.ValidateAmount(totalAmount);
        ValidateUtc(createdAt);
        if (installmentCount is < InstallmentAllocation.MinimumInstallments or > InstallmentAllocation.MaximumInstallments)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentCount));
        }

        var items = installments.OrderBy(item => item.Number).ToList();
        if (items.Count != installmentCount ||
            items.Where((item, index) => item.Number != index + 1).Any() ||
            items.Sum(item => item.Amount) != totalAmount ||
            items.Zip(items.Skip(1)).Any(pair => pair.First.DueDate >= pair.Second.DueDate))
        {
            throw new ArgumentException("A compra parcelada persistida possui parcelas inconsistentes.");
        }

        foreach (var item in items)
        {
            if (item.TransactionId == Guid.Empty || item.CategoryId == Guid.Empty || item.Version < 1)
            {
                throw new ArgumentException("Uma parcela persistida possui identificadores ou versão inválidos.");
            }

            var transaction = ManualTransaction.Restore(
                item.TransactionId,
                ManualTransactionType.Expense,
                item.Description,
                item.Amount,
                item.CategoryId,
                item.CompetenceDate,
                item.DueDate,
                item.Status,
                item.EffectiveDate,
                item.StatusBeforeCancellation);
            if (transaction.CompetenceDate.Year != item.DueDate.Year ||
                transaction.CompetenceDate.Month != item.DueDate.Month)
            {
                throw new ArgumentException("A competência da parcela não corresponde ao mês do vencimento.");
            }
        }

        return new InstallmentPurchase(
            id,
            description.Trim(),
            categoryId,
            totalAmount,
            installmentCount,
            originalFirstDueDate,
            createdAt,
            version,
            items);
    }

    public InstallmentPurchaseAuditEvent CreateInitialAuditEvent(DateTimeOffset occurredAt, Guid actorId)
    {
        ValidateAuditActorAndTime(actorId, occurredAt);
        var changes = new List<InstallmentPurchaseAuditChange>
        {
            new(null, "Description", null, Description),
            new(null, "CategoryId", null, CategoryId.ToString()),
            new(null, "TotalAmount", null, Format(TotalAmount)),
            new(null, "InstallmentCount", null, InstallmentCount.ToString(CultureInfo.InvariantCulture)),
            new(null, "OriginalFirstDueDate", null, Format(OriginalFirstDueDate))
        };
        foreach (var item in installments)
        {
            changes.Add(new(item.Number, "Description", null, item.Description));
            changes.Add(new(item.Number, "CategoryId", null, item.CategoryId.ToString()));
            changes.Add(new(item.Number, "Amount", null, Format(item.Amount)));
            changes.Add(new(item.Number, "DueDate", null, Format(item.DueDate)));
            changes.Add(new(item.Number, "CompetenceDate", null, Format(item.CompetenceDate)));
            changes.Add(new(item.Number, "Status", null, item.Status.ToString()));
        }

        return new InstallmentPurchaseAuditEvent(
            Guid.NewGuid(),
            Id,
            "Creation",
            changes,
            occurredAt,
            actorId,
            string.Empty);
    }

    public InstallmentPurchaseAuditEvent Realize(
        int installmentNumber,
        DateOnly effectiveDate,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        ValidateAuditActorAndTime(actorId, occurredAt);
        var index = FindInstallmentIndex(installmentNumber);
        var item = installments[index];
        if (item.Status != ManualTransactionStatus.Planned)
        {
            throw new InvalidOperationException("Somente parcelas previstas podem ser realizadas.");
        }

        installments[index] = item with
        {
            Status = ManualTransactionStatus.Realized,
            EffectiveDate = effectiveDate,
            Version = checked(item.Version + 1)
        };
        Version = checked(Version + 1);
        return CreateEvent(
            "Realization",
            [new(item.Number, "Status", ManualTransactionStatus.Planned.ToString(), ManualTransactionStatus.Realized.ToString()),
             new(item.Number, "EffectiveDate", null, Format(effectiveDate))],
            occurredAt,
            actorId,
            string.Empty);
    }

    public InstallmentPurchaseAuditEvent Edit(
        string description,
        Guid categoryId,
        decimal totalAmount,
        DateOnly? firstEligibleDueDate,
        DateOnly operationDate,
        string reason,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        ValidateAuditActorAndTime(actorId, occurredAt);
        var validatedReason = ValidateReason(reason);
        ValidateDescription(description);
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("A categoria é obrigatória.", nameof(categoryId));
        }

        ManualTransaction.ValidateAmount(totalAmount);
        var eligible = installments
            .Where(item => item.Status == ManualTransactionStatus.Planned && item.DueDate > operationDate)
            .OrderBy(item => item.Number)
            .ToArray();
        if (eligible.Length == 0)
        {
            throw new InvalidOperationException("Não há parcelas futuras previstas elegíveis para edição.");
        }

        var preservedAmount = installments
            .Except(eligible)
            .Sum(item => item.Amount);
        if (totalAmount < preservedAmount)
        {
            throw new InvalidOperationException("O novo total não pode ser inferior ao valor das parcelas preservadas.");
        }

        var normalizedDescription = description.Trim();
        var amountChanged = totalAmount != TotalAmount;
        var updatedAmounts = amountChanged
            ? InstallmentAllocation.Allocate(totalAmount - preservedAmount, eligible.Length)
            : eligible.Select(item => item.Amount).ToArray();
        var dueDates = new DateOnly[eligible.Length];
        for (var index = 0; index < eligible.Length; index++)
        {
            dueDates[index] = firstEligibleDueDate is { } referenceDate
                ? InstallmentSchedule.GetDueDate(referenceDate, eligible[index].Number - eligible[0].Number + 1)
                : eligible[index].DueDate;
            if (dueDates[index] <= operationDate)
            {
                throw new InvalidOperationException("Todo novo vencimento precisa ser posterior à data da operação.");
            }
        }

        EnsureScheduleDoesNotCrossProtectedInstallments(eligible, dueDates);

        var changes = new List<InstallmentPurchaseAuditChange>();
        AddPurchaseChange(changes, "Description", Description, normalizedDescription);
        AddPurchaseChange(changes, "CategoryId", CategoryId.ToString(), categoryId.ToString());
        AddPurchaseChange(changes, "TotalAmount", Format(TotalAmount), Format(totalAmount));

        var replacements = new List<InstallmentPurchaseInstallment>(eligible.Length);
        for (var index = 0; index < eligible.Length; index++)
        {
            var item = eligible[index];
            var dueDate = dueDates[index];
            var competenceDate = FirstDayOfMonth(dueDate);
            AddInstallmentChange(changes, item.Number, "Description", item.Description, normalizedDescription);
            AddInstallmentChange(changes, item.Number, "CategoryId", item.CategoryId.ToString(), categoryId.ToString());
            AddInstallmentChange(changes, item.Number, "Amount", Format(item.Amount), Format(updatedAmounts[index]));
            AddInstallmentChange(changes, item.Number, "DueDate", Format(item.DueDate), Format(dueDate));
            AddInstallmentChange(changes, item.Number, "CompetenceDate", Format(item.CompetenceDate), Format(competenceDate));

            var changed = item.Description != normalizedDescription ||
                          item.CategoryId != categoryId ||
                          item.Amount != updatedAmounts[index] ||
                          item.DueDate != dueDate ||
                          item.CompetenceDate != competenceDate;
            replacements.Add(item with
            {
                Description = normalizedDescription,
                CategoryId = categoryId,
                Amount = updatedAmounts[index],
                DueDate = dueDate,
                CompetenceDate = competenceDate,
                Version = changed ? checked(item.Version + 1) : item.Version
            });
        }

        if (changes.Count == 0)
        {
            throw new ArgumentException("A edição deve alterar ao menos um dado.", nameof(description));
        }

        for (var index = 0; index < eligible.Length; index++)
        {
            var originalIndex = FindInstallmentIndex(eligible[index].Number);
            installments[originalIndex] = replacements[index];
        }

        Description = normalizedDescription;
        CategoryId = categoryId;
        TotalAmount = totalAmount;
        Version = checked(Version + 1);
        return CreateEvent("Edit", changes, occurredAt, actorId, validatedReason);
    }

    public InstallmentPurchaseAuditEvent Cancel(
        DateOnly operationDate,
        string reason,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        ValidateAuditActorAndTime(actorId, occurredAt);
        var validatedReason = ValidateReason(reason);
        var eligible = installments
            .Where(item => item.Status == ManualTransactionStatus.Planned && item.DueDate > operationDate)
            .OrderBy(item => item.Number)
            .ToArray();
        if (eligible.Length == 0)
        {
            throw new InvalidOperationException("Não há parcelas futuras previstas elegíveis para cancelamento.");
        }

        var changes = new List<InstallmentPurchaseAuditChange>();
        foreach (var item in eligible)
        {
            changes.Add(new(item.Number, "Status", item.Status.ToString(), ManualTransactionStatus.Cancelled.ToString()));
            var index = FindInstallmentIndex(item.Number);
            installments[index] = item with
            {
                Status = ManualTransactionStatus.Cancelled,
                StatusBeforeCancellation = ManualTransactionStatus.Planned,
                Version = checked(item.Version + 1)
            };
        }

        Version = checked(Version + 1);
        return CreateEvent("Cancellation", changes, occurredAt, actorId, validatedReason);
    }

    private void EnsureScheduleDoesNotCrossProtectedInstallments(
        InstallmentPurchaseInstallment[] eligible,
        DateOnly[] dueDates)
    {
        var eligibleNumbers = eligible.Select(item => item.Number).ToHashSet();
        var protectedItems = installments.Where(item => !eligibleNumbers.Contains(item.Number)).ToArray();
        for (var index = 0; index < eligible.Length; index++)
        {
            foreach (var protectedItem in protectedItems)
            {
                if (protectedItem.Number < eligible[index].Number && protectedItem.DueDate >= dueDates[index] ||
                    protectedItem.Number > eligible[index].Number && protectedItem.DueDate <= dueDates[index])
                {
                    throw new InvalidOperationException("A nova agenda cruza o vencimento de uma parcela preservada.");
                }
            }
        }
    }

    private int FindInstallmentIndex(int installmentNumber)
    {
        var index = installments.FindIndex(item => item.Number == installmentNumber);
        return index >= 0
            ? index
            : throw new ArgumentOutOfRangeException(nameof(installmentNumber), "O número da parcela não pertence à compra.");
    }

    private InstallmentPurchaseAuditEvent CreateEvent(
        string eventType,
        IReadOnlyList<InstallmentPurchaseAuditChange> changes,
        DateTimeOffset occurredAt,
        Guid actorId,
        string reason) =>
        new(Guid.NewGuid(), Id, eventType, changes, occurredAt, actorId, reason);

    private static void AddPurchaseChange(
        List<InstallmentPurchaseAuditChange> changes,
        string field,
        string previous,
        string next)
    {
        if (previous != next)
        {
            changes.Add(new(null, field, previous, next));
        }
    }

    private static void AddInstallmentChange(
        List<InstallmentPurchaseAuditChange> changes,
        int number,
        string field,
        string previous,
        string next)
    {
        if (previous != next)
        {
            changes.Add(new(number, field, previous, next));
        }
    }

    private static void ValidateDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A descrição é obrigatória.", nameof(description));
        }
    }

    private static string ValidateReason(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("O motivo é obrigatório.", nameof(reason));
        }

        return reason.Trim();
    }

    private static void ValidateAuditActorAndTime(Guid actorId, DateTimeOffset occurredAt)
    {
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("O responsável pela operação é obrigatório.", nameof(actorId));
        }

        ValidateUtc(occurredAt);
    }

    private static void ValidateUtc(DateTimeOffset value)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("O instante da operação deve estar em UTC.", nameof(value));
        }
    }

    private static DateOnly FirstDayOfMonth(DateOnly value) => new(value.Year, value.Month, 1);

    private static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Format(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
