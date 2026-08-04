using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Agava.YandexGames;
using GameAnalyticsSDK;
using UnityEngine;
using Billing = Kimicu.YandexGames.Billing;

public static class GaEventProvider
{
    private const int MaxKeySegments = 5;
    private const int MaxSegmentLength = 64;
    private const string DefaultItemType = "IAP";
    private const string DefaultCartType = "Shop";

    public static IEnumerator FirstClickCheck()
    {
        yield return new WaitUntil(() => Input.GetMouseButton(0));

        DesignEvent("Game Started");
    }

    public static void DesignEvent(string key, params MetricData[] metricData)
    {
        if (metricData != null && metricData.Length > 0)
            key += ":" + string.Join(":", metricData.Select(x => x.Invoke()));

        var sanitized = SanitizeKey(key);
        if (sanitized == null)
        {
            Debug.LogWarning($"GaEventProvider: design event key '{key}' is empty after sanitizing, event dropped.");
            return;
        }

        if (sanitized != key)
            Debug.LogWarning($"GaEventProvider: design event key '{key}' is invalid for GameAnalytics, sent as '{sanitized}'.");

        GameAnalytics.NewDesignEvent(sanitized);
    }

    public static void ProgressionEvent(GAProgressionStatus progressionStatus, params MetricData[] metricData)
    {
        // NewProgressionEvent takes at most 3 string levels - the 4th argument of the wider
        // overload is an int score, so there is no 4-level form to forward to.
        switch (metricData?.Length ?? 0)
        {
            case 1:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke());
                break;
            case 2:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke(), metricData[1].Invoke());
                break;
            case 3:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke(), metricData[1].Invoke(), metricData[2].Invoke());
                break;
            default:
                Debug.LogWarning($"GaEventProvider: progression event needs 1..3 levels, got {metricData?.Length ?? 0}. Event dropped.");
                break;
        }
    }

    public static void RewardAdEvent(string placement)
    {
        GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, "Yandex", placement);
    }

    public static void InterstitialAdEvent(string placement)
    {
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, "Yandex", placement);
    }

    // ---------------------------------------------------------------------------------------
    // Воронка уровней. Единый словарь для всех портируемых игр: одинаковые имена событий →
    // дашборды разных игр сравнимы между собой. Вызывать из игрового кода вручную — сам по себе
    // ProgressionEvent нигде не срабатывает.
    // ---------------------------------------------------------------------------------------

    public static void LevelStarted(int level)
    {
        SendLevelProgression(GAProgressionStatus.Start, level, null);
    }

    public static void LevelCompleted(int level, int? score = null)
    {
        SendLevelProgression(GAProgressionStatus.Complete, level, score);
    }

    public static void LevelFailed(int level, int? score = null)
    {
        SendLevelProgression(GAProgressionStatus.Fail, level, score);
    }

    /// <summary>
    /// Имя уровня с ведущим нулём: дашборд GA сортирует прогрессии лексически, и без padding
    /// "Level_10" встаёт раньше "Level_2".
    /// </summary>
    public static string LevelName(int level) => $"Level_{level:00}";

    private static void SendLevelProgression(GAProgressionStatus status, int level, int? score)
    {
        var levelName = LevelName(level);

        if (score.HasValue)
            GameAnalytics.NewProgressionEvent(status, levelName, score.Value);
        else
            GameAnalytics.NewProgressionEvent(status, levelName);
    }

    // ---------------------------------------------------------------------------------------
    // Покупки. Yandex Billing отдаёт цену строками (`priceValue` = "199", `priceCurrencyCode` =
    // "RUB"), а GA хочет ISO-код валюты и сумму в МИНОРНЫХ единицах (копейки/центы) целым числом.
    // Конвертация одинакова во всех яндекс-играх, поэтому живёт здесь, а не в коде игры.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Находит товар в каталоге Yandex Billing по id и шлёт business-событие.
    /// Вызывать в момент РЕАЛЬНОЙ оплаты, а не в общей точке выдачи товара: выдача обычно
    /// повторяется при восстановлении отложенных покупок на старте, и выручка задвоится.
    /// </summary>
    public static void PurchaseById(string productId, string itemType = DefaultItemType, string cartType = DefaultCartType)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            Debug.LogWarning("GaEventProvider: purchase event skipped — product id is empty.");
            return;
        }

        var catalog = Billing.CatalogProducts;
        var product = catalog?.FirstOrDefault(item => item.id == productId);

        if (product == null)
        {
            Debug.LogWarning($"GaEventProvider: purchase '{productId}' skipped — not found in Billing catalog.");
            return;
        }

        Purchase(product, itemType, cartType);
    }

    public static void Purchase(CatalogProduct product, string itemType = DefaultItemType, string cartType = DefaultCartType)
    {
        if (product == null)
        {
            Debug.LogWarning("GaEventProvider: purchase event skipped — product is null.");
            return;
        }

        Purchase(product.priceCurrencyCode, product.priceValue, product.id, itemType, cartType);
    }

    public static void Purchase(string currencyCode, string priceValue, string itemId,
        string itemType = DefaultItemType, string cartType = DefaultCartType)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            Debug.LogWarning($"GaEventProvider: purchase '{itemId}' skipped — currency code is empty.");
            return;
        }

        if (!TryParseMinorUnits(priceValue, out var amount))
        {
            Debug.LogWarning($"GaEventProvider: purchase '{itemId}' skipped — cannot parse price '{priceValue}'.");
            return;
        }

        GameAnalytics.NewBusinessEvent(currencyCode, amount, itemType, itemId, cartType);
    }

    private static bool TryParseMinorUnits(string priceValue, out int minorUnits)
    {
        minorUnits = 0;

        if (string.IsNullOrWhiteSpace(priceValue))
            return false;

        // Яндекс может прислать и "199", и "199.00", и "199,00" — нормализуем разделитель.
        var normalized = priceValue.Replace(',', '.').Trim();

        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value <= 0m)
            return false;

        minorUnits = (int) decimal.Round(value * 100m, System.MidpointRounding.AwayFromZero);
        return true;
    }

    // GameAnalytics silently drops events whose key breaks its format (see GAValidator: up to 5
    // ':'-separated segments, 1-64 chars each, only [A-Za-z0-9 \-_.()!?]) - it only writes a plain
    // Debug.Log, which nobody sees in a build. Repair the key and warn instead of losing the event.
    private static string SanitizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var kept = new List<string>(MaxKeySegments);

        foreach (var segment in key.Split(':'))
        {
            if (kept.Count == MaxKeySegments)
                break;

            var builder = new StringBuilder(segment.Length);
            foreach (var symbol in segment)
            {
                if (builder.Length == MaxSegmentLength)
                    break;

                builder.Append(IsAllowed(symbol) ? symbol : '_');
            }

            if (builder.Length > 0)
                kept.Add(builder.ToString());
        }

        return kept.Count > 0 ? string.Join(":", kept) : null;
    }

    private static bool IsAllowed(char symbol)
    {
        if (symbol >= 'A' && symbol <= 'Z') return true;
        if (symbol >= 'a' && symbol <= 'z') return true;
        if (symbol >= '0' && symbol <= '9') return true;

        return symbol == ' ' || symbol == '-' || symbol == '_' || symbol == '.'
               || symbol == '(' || symbol == ')' || symbol == '!' || symbol == '?';
    }

    public delegate string MetricData();
}
