using CaseCellShop.Domain.Abstractions;

namespace CaseCellShop.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
