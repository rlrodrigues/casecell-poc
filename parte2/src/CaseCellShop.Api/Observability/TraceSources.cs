using System.Diagnostics;

namespace CaseCellShop.Api.Observability;

public static class TraceSources
{
    public const string Name = "CaseCellShop.Api";
    public static readonly ActivitySource ActivitySource = new(Name);
}
