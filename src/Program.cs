using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using System.Text.Json;

await WebAssemblyHostBuilder.CreateDefault(args).Build().RunAsync();

public static class BrowserApi
{
    [JSInvokable]
    public static string Calculate(string mode, string json)
    {
        var input = JsonSerializer.Deserialize<Input>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var result = input is null ? new { error = "Введите данные." } : mode switch
        {
            "springate" => CalculateSpringate(input),
            "liquidity" => CalculateLiquidity(input),
            _ => new { error = "Неизвестный калькулятор." }
        };
        return JsonSerializer.Serialize(result);
    }
    static object CalculateSpringate(Input input)
    {
        if (input.CurrentAssets is not (>= 0) || input.CurrentLiabilities is not (> 0) ||
            input.Assets is not (> 0) || input.CurrentAssets > input.Assets ||
            input.Sales is not (>= 0) || input.Ebit is null || input.Ebt is null)
            return new { error = "Заполните все поля. Активы и краткосрочные обязательства должны быть больше нуля, оборотные активы не могут превышать общие, выручка не может быть отрицательной." };
    
        try
        {
            var a = (input.CurrentAssets.Value - input.CurrentLiabilities.Value) / input.Assets.Value;
            var b = input.Ebit.Value / input.Assets.Value;
            var c = input.Ebt.Value / input.CurrentLiabilities.Value;
            var d = input.Sales.Value / input.Assets.Value;
            var score = 1.03m * a + 3.07m * b + 0.66m * c + 0.40m * d;
            return new { score, threshold = 0.862m, ratios = new[] { a, b, c, d },
                message = score <= 0.862m ? "Зона финансового риска по модели" : "Выше порога риска по модели" };
        }
        catch (OverflowException)
        {
            return new { error = "Результат выходит за допустимый числовой диапазон." };
        }
    }

    static object CalculateLiquidity(Input input)
    {
        if (input.CurrentAssets is not (>= 0) || input.CurrentLiabilities is not (> 0))
            return new { error = "Введите неотрицательные оборотные активы и краткосрочные обязательства больше нуля." };
    
        try
        {
            var score = input.CurrentAssets.Value / input.CurrentLiabilities.Value;
            return new { score, message = score >= 1
                ? "Оборотные активы покрывают краткосрочные обязательства по балансу"
                : "Оборотных активов меньше, чем краткосрочных обязательств" };
        }
        catch (OverflowException)
        {
            return new { error = "Результат выходит за допустимый числовой диапазон." };
        }
    }
}

public record Input(decimal? CurrentAssets, decimal? CurrentLiabilities, decimal? Assets, decimal? Ebit, decimal? Ebt, decimal? Sales);


