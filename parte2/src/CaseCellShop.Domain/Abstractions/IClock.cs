namespace CaseCellShop.Domain.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
