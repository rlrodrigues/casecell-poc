using CaseCellShop.Domain.Abstractions;

namespace CaseCellShop.Tests.Support;

public sealed class NoopApplicationMetrics : IApplicationMetrics
{
    public void CacheHit() { }
    public void CacheMiss() { }
    public void CheckoutAccepted() { }
    public void CheckoutRejectedOutOfStock() { }
    public void BillingSucceeded() { }
    public void BillingFailed() { }
}
