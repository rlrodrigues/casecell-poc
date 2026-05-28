using System.Diagnostics.Metrics;
using CaseCellShop.Domain.Abstractions;

namespace CaseCellShop.Api.Observability;

public sealed class ApplicationMetrics : IApplicationMetrics
{
    public const string MeterName = "CaseCellShop.Api";

    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _checkoutAccepted;
    private readonly Counter<long> _checkoutRejectedOutOfStock;
    private readonly Counter<long> _billingSucceeded;
    private readonly Counter<long> _billingFailed;

    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _cacheHits = meter.CreateCounter<long>("casecellshop_cache_hits_total");
        _cacheMisses = meter.CreateCounter<long>("casecellshop_cache_misses_total");
        _checkoutAccepted = meter.CreateCounter<long>("casecellshop_checkout_accepted_total");
        _checkoutRejectedOutOfStock = meter.CreateCounter<long>("casecellshop_checkout_rejected_out_of_stock_total");
        _billingSucceeded = meter.CreateCounter<long>("casecellshop_billing_succeeded_total");
        _billingFailed = meter.CreateCounter<long>("casecellshop_billing_failed_total");
    }

    public void CacheHit() => _cacheHits.Add(1);
    public void CacheMiss() => _cacheMisses.Add(1);
    public void CheckoutAccepted() => _checkoutAccepted.Add(1);
    public void CheckoutRejectedOutOfStock() => _checkoutRejectedOutOfStock.Add(1);
    public void BillingSucceeded() => _billingSucceeded.Add(1);
    public void BillingFailed() => _billingFailed.Add(1);
}
