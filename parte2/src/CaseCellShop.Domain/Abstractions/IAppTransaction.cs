namespace CaseCellShop.Domain.Abstractions;

public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
