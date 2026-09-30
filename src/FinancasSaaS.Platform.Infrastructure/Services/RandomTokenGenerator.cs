using System.Security.Cryptography;
using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Platform.Infrastructure.Services;

public sealed class RandomTokenGenerator : ITokenGenerator
{
    public string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
