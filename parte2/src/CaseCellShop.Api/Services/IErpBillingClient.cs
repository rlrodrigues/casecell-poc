namespace CaseCellShop.Api.Services;

public interface IErpBillingClient
{
    Task BillAsync(Guid orderId, CancellationToken cancellationToken);
}
