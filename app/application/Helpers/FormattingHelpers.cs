using System.Globalization;

namespace application.Helpers;

public static class FormattingHelpers
{
    private static readonly CultureInfo UsCulture = new("en-US");

    public static string FormatMoney(decimal val) =>
        val.ToString("C0", UsCulture);

    public static string TipoLabel(string t) => t switch
    {
        "prepayment" => "Prepago",
        _ => "Cotidiano"
    };

    public static string EffectLabel(string? e) => e switch
    {
        "reduceTerm" => "Reducir plazo",
        "reducePayment" => "Reducir cuota",
        _ => "—"
    };

    public static string OnTimeLabel(bool ot) =>
        ot ? "Sí" : "No";

    public static string ProgressClass(decimal pct) =>
        pct >= 100 ? "bg-success" : pct >= 50 ? "bg-primary" : "bg-warning";
}
