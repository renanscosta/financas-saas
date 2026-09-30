using System.Security.Cryptography;
using System.Text;
using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Platform.Infrastructure.Services;

public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
