using CaseCellShop.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CaseCellShop.Infrastructure.External;

public sealed class FakeErpBillingClient(IConfiguration configuration, ILogger<FakeErpBillingClient> logger) : IErpBillingClient
{
    public async Task BillAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var delayMs = int.TryParse(configuration["Erp:BillingDelayMs"], out var configuredDelayMs)
            ? configuredDelayMs
            : 250;
        logger.LogInformation("erp_billing_started orderId={OrderId} delayMs={DelayMs}", orderId, delayMs);
        await Task.Delay(delayMs, cancellationToken);
        logger.LogInformation("erp_billing_succeeded orderId={OrderId}", orderId);
    }
}
