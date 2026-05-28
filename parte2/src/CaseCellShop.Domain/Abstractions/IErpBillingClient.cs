namespace CaseCellShop.Domain.Abstractions;

public interface IErpBillingClient
{
    Task BillAsync(Guid orderId, CancellationToken cancellationToken);
}
