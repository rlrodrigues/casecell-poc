namespace CaseCellShop.Domain.Abstractions;

public interface IApplicationMetrics
{
    void CacheHit();
    void CacheMiss();
    void CheckoutAccepted();
    void CheckoutRejectedOutOfStock();
    void BillingSucceeded();
    void BillingFailed();
}
