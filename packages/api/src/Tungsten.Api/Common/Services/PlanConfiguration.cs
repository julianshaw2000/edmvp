namespace Tungsten.Api.Common.Services;

public static class PlanConfiguration
{
    public static (int? maxBatches, int? maxUsers) GetLimits(string planName) => planName switch
    {
        "STARTER" => (50, 5),
        "PRO" => (null, null), // Unlimited
        _ => (null, null),
    };

    public static string GetPriceId(string planName, IConfiguration? config) => planName switch
    {
        "STARTER" => config?["Stripe:StarterPriceId"] ?? "price_1TEK1zCvOGA4undoEH4fPTVr",
        "PRO" => config?["Stripe:PriceId"] ?? "price_1TEEQ1CvOGA4undoCj5R57Yd",
        _ => config?["Stripe:PriceId"] ?? "price_1TEEQ1CvOGA4undoCj5R57Yd",
    };
}
