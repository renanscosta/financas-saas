using System.Globalization;

namespace FinancasSaaS.Domain.Finance;

public sealed class ManualTransaction
{
    public const string MaximumAmountText = "9999999999999999.99";
    public const decimal MaximumAmount = 9999999999999999.99m;

    public Guid Id { get; }
    public ManualTransactionType Type { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateOnly CompetenceDate { get; private set; }
    public DateOnly ExpectedDate { get; private set; }
    public ManualTransactionStatus Status { get; private set; }
    public DateOnly? EffectiveDate { get; private set; }
    public ManualTransactionStatus? StatusBeforeCancellation { get; private set; }

    private ManualTransaction(
        Guid id,
        ManualTransactionType type,
        string description,
        decimal amount,
        Guid categoryId,
        DateOnly competenceDate,
        DateOnly expectedDate)
    {
        Id = id;
        Type = type;
        Description = description;
        Amount = amount;
        CategoryId = categoryId;
        CompetenceDate = competenceDate;
        ExpectedDate = expectedDate;
        Status = ManualTransactionStatus.Planned;
    }

    public static ManualTransaction Create(
        Guid id,
        ManualTransactionType type,
        string description,
        decimal amount,
        Guid categoryId,
        DateOnly competenceDate,
        DateOnly expectedDate)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O identificador do lançamento é obrigatório.", nameof(id));
        }

        ValidateType(type);
        ValidateDescription(description);
        ValidateAmount(amount);
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("A categoria é obrigatória.", nameof(categoryId));
        }

        return new ManualTransaction(id, type, description.Trim(), amount, categoryId, competenceDate, expectedDate);
    }

    public static ManualTransaction Restore(
        Guid id,
        ManualTransactionType type,
        string description,
        decimal amount,
        Guid categoryId,
        DateOnly competenceDate,
        DateOnly expectedDate,
        ManualTransactionStatus status,
        DateOnly? effectiveDate,
        ManualTransactionStatus? statusBeforeCancellation)
    {
        var transaction = Create(id, type, description, amount, categoryId, competenceDate, expectedDate);
        if (status == ManualTransactionStatus.Realized)
        {
            if (effectiveDate is null || statusBeforeCancellation is not null)
            {
                throw new ArgumentException("O lançamento realizado possui estado inconsistente.", nameof(status));
            }

            transaction.Realize(effectiveDate.Value);
        }
        else if (status == ManualTransactionStatus.Cancelled)
        {
            if (effectiveDate is null && statusBeforeCancellation == ManualTransactionStatus.Realized ||
                statusBeforeCancellation == ManualTransactionStatus.Planned && effectiveDate is not null ||
                statusBeforeCancellation is not (ManualTransactionStatus.Planned or ManualTransactionStatus.Realized))
            {
                throw new ArgumentException("O lançamento cancelado possui estado inconsistente.", nameof(status));
            }

            if (statusBeforeCancellation == ManualTransactionStatus.Realized)
            {
                transaction.Realize(effectiveDate!.Value);
            }

            transaction.StatusBeforeCancellation = statusBeforeCancellation;
            transaction.Status = ManualTransactionStatus.Cancelled;
        }
        else if (status != ManualTransactionStatus.Planned || effectiveDate is not null || statusBeforeCancellation is not null)
        {
            throw new ArgumentException("O lançamento previsto possui estado inconsistente.", nameof(status));
        }

        return transaction;
    }

    public void Realize(DateOnly effectiveDate)
    {
        if (Status != ManualTransactionStatus.Planned)
        {
            throw new InvalidOperationException("Somente lançamentos previstos podem ser realizados.");
        }

        EffectiveDate = effectiveDate;
        Status = ManualTransactionStatus.Realized;
    }

    public ManualTransactionAuditEvent Correct(
        ManualTransactionCorrection correction,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(correction);
        if (Status == ManualTransactionStatus.Cancelled)
        {
            throw new InvalidOperationException("Lançamentos cancelados não podem ser corrigidos.");
        }

        var reason = ValidateReason(correction.Reason);
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("O responsável pela correção é obrigatório.", nameof(actorId));
        }

        ValidateUtc(occurredAt);

        var changes = new List<ManualTransactionChange>();
        var type = Type;
        var description = Description;
        var amount = Amount;
        var categoryId = CategoryId;
        var competenceDate = CompetenceDate;
        var expectedDate = ExpectedDate;

        if (correction.Type is { } newType && newType != Type)
        {
            ValidateType(newType);
            changes.Add(new("Type", Type.ToString(), newType.ToString()));
            type = newType;
        }

        if (correction.Description is { } newDescription && newDescription.Trim() != Description)
        {
            ValidateDescription(newDescription);
            changes.Add(new("Description", Description, newDescription.Trim()));
            description = newDescription.Trim();
        }

        if (correction.Amount is { } newAmount && newAmount != Amount)
        {
            ValidateAmount(newAmount);
            changes.Add(new("Amount", Format(Amount), Format(newAmount)));
            amount = newAmount;
        }

        if (correction.CategoryId is { } newCategoryId && newCategoryId != CategoryId)
        {
            if (newCategoryId == Guid.Empty)
            {
                throw new ArgumentException("A categoria é obrigatória.", nameof(correction));
            }

            changes.Add(new("CategoryId", CategoryId.ToString(), newCategoryId.ToString()));
            categoryId = newCategoryId;
        }

        if (correction.CompetenceDate is { } newCompetenceDate && newCompetenceDate != CompetenceDate)
        {
            changes.Add(new(
                "CompetenceDate",
                CompetenceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                newCompetenceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            competenceDate = newCompetenceDate;
        }

        if (correction.ExpectedDate is { } newExpectedDate && newExpectedDate != ExpectedDate)
        {
            changes.Add(new(
                "ExpectedDate",
                ExpectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                newExpectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            expectedDate = newExpectedDate;
        }

        if (changes.Count == 0)
        {
            throw new ArgumentException("A correção deve alterar ao menos um campo.", nameof(correction));
        }

        Type = type;
        Description = description;
        Amount = amount;
        CategoryId = categoryId;
        CompetenceDate = competenceDate;
        ExpectedDate = expectedDate;

        return new ManualTransactionAuditEvent(
            Guid.NewGuid(),
            Id,
            "Correction",
            changes,
            occurredAt,
            actorId,
            reason);
    }

    public ManualTransactionAuditEvent Cancel(DateTimeOffset occurredAt, Guid actorId, string reason)
    {
        if (Status == ManualTransactionStatus.Cancelled)
        {
            throw new InvalidOperationException("O lançamento já está cancelado.");
        }

        var validatedReason = ValidateReason(reason);
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("O responsável pelo cancelamento é obrigatório.", nameof(actorId));
        }

        ValidateUtc(occurredAt);
        var previousStatus = Status;
        StatusBeforeCancellation = previousStatus;
        Status = ManualTransactionStatus.Cancelled;

        return new ManualTransactionAuditEvent(
            Guid.NewGuid(),
            Id,
            "Cancellation",
            [new("Status", previousStatus.ToString(), ManualTransactionStatus.Cancelled.ToString())],
            occurredAt,
            actorId,
            validatedReason);
    }

    public DateOnly FilterDate => Status switch
    {
        ManualTransactionStatus.Planned => ExpectedDate,
        ManualTransactionStatus.Realized => EffectiveDate!.Value,
        ManualTransactionStatus.Cancelled when StatusBeforeCancellation == ManualTransactionStatus.Planned => ExpectedDate,
        ManualTransactionStatus.Cancelled when StatusBeforeCancellation == ManualTransactionStatus.Realized => EffectiveDate!.Value,
        _ => throw new InvalidOperationException("O lançamento cancelado não possui estado anterior válido.")
    };

    public static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "O valor deve ser positivo.");
        }

        if (amount > MaximumAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "O valor excede o limite suportado.");
        }

        if (decimal.Round(amount, 2, MidpointRounding.ToEven) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "O valor não pode ter mais de duas casas decimais.");
        }
    }

    private static void ValidateType(ManualTransactionType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "O tipo de lançamento é inválido.");
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

    private static void ValidateUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("O instante de auditoria deve estar em UTC.", nameof(value));
        }
    }

    private static string Format(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
