using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;

public static partial class BrowserApi
{
    public static void Main() { }

    [JSExport]
    public static string Calculate(string mode, string json)
    {
        var context = CalculatorJsonContext.Default;
        var input = JsonSerializer.Deserialize(json, context.Input);
        Calculation result = input is null ? new(Error: "Введите данные.") : mode switch
        {
            "springate" => CalculateSpringate(input),
            "liquidity" => CalculateLiquidity(input),
            _ => new(Error: "Неизвестный калькулятор.")
        };
        return JsonSerializer.Serialize(result, context.Calculation);
    }

    private static Calculation CalculateSpringate(Input input)
    {
        if (input.CurrentAssets is not (>= 0) || input.CurrentLiabilities is not (> 0) ||
            input.Assets is not (> 0) || input.CurrentAssets > input.Assets ||
            input.Sales is not (>= 0) || input.Ebit is null || input.Ebt is null)
            return new(Error: "Заполните все поля. Активы и краткосрочные обязательства должны быть больше нуля, оборотные активы не могут превышать общие, выручка не может быть отрицательной.");

        try
        {
            var a = (input.CurrentAssets.Value - input.CurrentLiabilities.Value) / input.Assets.Value;
            var b = input.Ebit.Value / input.Assets.Value;
            var c = input.Ebt.Value / input.CurrentLiabilities.Value;
            var d = input.Sales.Value / input.Assets.Value;
            var score = 1.03m * a + 3.07m * b + 0.66m * c + 0.40m * d;
            return new(score, score <= 0.862m
                ? "Зона финансового риска по модели"
                : "Выше порога риска по модели", [a, b, c, d]);
        }
        catch (OverflowException)
        {
            return new(Error: "Результат выходит за допустимый числовой диапазон.");
        }
    }

    private static Calculation CalculateLiquidity(Input input)
    {
        if (input.CurrentAssets is not (>= 0) || input.CurrentLiabilities is not (> 0))
            return new(Error: "Введите неотрицательные оборотные активы и краткосрочные обязательства больше нуля.");

        try
        {
            var score = input.CurrentAssets.Value / input.CurrentLiabilities.Value;
            return new(score, score >= 1
                ? "Оборотные активы покрывают краткосрочные обязательства по балансу"
                : "Оборотных активов меньше, чем краткосрочных обязательств");
        }
        catch (OverflowException)
        {
            return new(Error: "Результат выходит за допустимый числовой диапазон.");
        }
    }
}

internal record Input(decimal? CurrentAssets, decimal? CurrentLiabilities, decimal? Assets, decimal? Ebit, decimal? Ebt, decimal? Sales);
internal record Calculation(decimal? Score = null, string? Message = null, decimal[]? Ratios = null, string? Error = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Input))]
[JsonSerializable(typeof(Calculation))]
internal partial class CalculatorJsonContext : JsonSerializerContext;

