namespace FinancasSaaS.Domain.Finance;

public sealed class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private Category(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Category Create(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O identificador da categoria é obrigatório.", nameof(id));
        }

        return new Category(id, ValidateName(name));
    }

    public void Rename(string name) => Name = ValidateName(name);

    public static string NormalizeName(string name) => ValidateName(name).Trim().ToUpperInvariant();

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("O nome da categoria é obrigatório.", nameof(name));
        }

        if (trimmed.Length > 120)
        {
            throw new ArgumentException("O nome da categoria não pode exceder 120 caracteres.", nameof(name));
        }

        return trimmed;
    }
}
