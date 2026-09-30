using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Platform.Infrastructure.Services;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
