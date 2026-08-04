using System;
using Kimicu.YandexGames;
using Newtonsoft.Json;

public class SaveSystem : Singleton<SaveSystem>
{
    private const string CloudSaveKey = "SaveData";
    private const string LocalSaveKey = "IdleDefend.LocalSaveData";
    private const int SavingPeriod = 4;

    private static bool IsDataLoaded { get; set; }

    private static PlayerSaveData cachedSaveData;

    public static ref PlayerSaveData SaveData
    {
        get
        {
            if (!IsDataLoaded)
                cachedSaveData = LoadPlayerData();

            return ref cachedSaveData;
        }
    }

    private static string lastSavedLocalJson;
    private static string lastSavedCloudJson;

    public static int GetUnlockedLevelIndex()
    {
        return SaveData.UnlockedLevelIndex;
    }

    public static int GetLevelStars(int levelId)
    {
        EnsureRuntimeCollections();
        return cachedSaveData.LevelStars.TryGetValue(levelId, out var stars) ? stars : 0;
    }

    /// <summary>
    /// Фиксирует звёзды, не двигая прогресс по карте. Нужно для поражения: продержаться до
    /// второй звезды — результат, который стоит сохранить, но следующий уровень открывать нельзя.
    /// </summary>
    public static void SaveLevelStars(int levelId, int stars)
    {
        EnsureRuntimeCollections();

        var bestStars = cachedSaveData.LevelStars.TryGetValue(levelId, out var existing) ? existing : 0;
        if (stars <= bestStars)
            return;

        cachedSaveData.LevelStars[levelId] = stars;
        Instance.SaveToStorage();
    }

    public static void SaveLevelProgress(int levelId, int stars, int unlockedLevelIndex)
    {
        EnsureRuntimeCollections();

        var bestStars = cachedSaveData.LevelStars.TryGetValue(levelId, out var existing) ? existing : 0;
        if (stars > bestStars)
            cachedSaveData.LevelStars[levelId] = stars;

        if (unlockedLevelIndex > cachedSaveData.UnlockedLevelIndex)
            cachedSaveData.UnlockedLevelIndex = unlockedLevelIndex;

        Instance.SaveToStorage();
    }

    public override void Init()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InvokeRepeating(nameof(SavePlayerData), SavingPeriod, SavingPeriod);
        DontDestroyOnLoad(this);
        base.Init();
    }
    
    private static PlayerSaveData LoadPlayerData()
    {
        string json = LoadJson();

        PlayerSaveData saveData;

        try
        {
            saveData = !string.IsNullOrWhiteSpace(json)
                ? JsonConvert.DeserializeObject<PlayerSaveData>(json)
                : CreateDefaultSave();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[SaveSystem] Save data is invalid. Starting with defaults: {exception.Message}");
            saveData = CreateDefaultSave();
        }

        // It is very imporant to add null-checks for any collections you add in future updates
        // Since NewtonsoftJson is creating nulls when reading jsons with no info about collections
        EnsureRuntimeCollections(ref saveData);

        IsDataLoaded = true;
        return saveData;
    }

    private static string LoadJson()
    {
        string localJson = UnityEngine.PlayerPrefs.GetString(LocalSaveKey, string.Empty);
        lastSavedLocalJson = localJson;

        if (!Cloud.Initialized)
            return localJson;

        try
        {
            string cloudJson = Cloud.GetValue(CloudSaveKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(cloudJson))
            {
                lastSavedCloudJson = cloudJson;
                return cloudJson;
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[SaveSystem] Cloud load failed. Using local data: {exception.Message}");
        }

        return localJson;
    }

    private static PlayerSaveData CreateDefaultSave()
    {
        return new PlayerSaveData
        {
            NoAds = false,
            BestSurvivalTime = 0f,
        };
    }

    public static void EnsureRuntimeCollections()
    {
        var saveData = SaveData;
        EnsureRuntimeCollections(ref saveData);
        cachedSaveData = saveData;
    }

    private static void EnsureRuntimeCollections(ref PlayerSaveData saveData)
    {
        if (saveData.LevelStars == null)
            saveData.LevelStars = new System.Collections.Generic.Dictionary<int, int>();

        if (saveData.PurchasedShopItemIds == null)
            saveData.PurchasedShopItemIds = new System.Collections.Generic.List<string>();

        if (saveData.EquippedShopItemIds == null)
            saveData.EquippedShopItemIds = new System.Collections.Generic.Dictionary<string, string>();

        if (saveData.BoostItemCounts == null)
            saveData.BoostItemCounts = new System.Collections.Generic.Dictionary<string, int>();

        if (saveData.SelectedBoostItemIds == null)
            saveData.SelectedBoostItemIds = new System.Collections.Generic.List<string>();
    }

    /// <summary>
    /// Полный сброс прогресса — для тестирования (см. <see cref="SaveResetCheat"/>).
    /// Чистит локальную копию, облако И кэш в памяти. Сброс кэша обязателен: без него
    /// ближайший автосейв через SavingPeriod секунд просто зальёт старые данные обратно —
    /// ровно поэтому ручная очистка облака в консоли Яндекса выглядит как «ничего не произошло».
    /// </summary>
    public static void ResetAllData()
    {
        cachedSaveData = CreateDefaultSave();
        EnsureRuntimeCollections(ref cachedSaveData);
        IsDataLoaded = true;

        string json = JsonConvert.SerializeObject(cachedSaveData);

        UnityEngine.PlayerPrefs.DeleteKey(LocalSaveKey);
        UnityEngine.PlayerPrefs.Save();
        lastSavedLocalJson = null;
        lastSavedCloudJson = null;

        if (!Cloud.Initialized)
        {
            UnityEngine.Debug.LogWarning("[SaveSystem] Cloud is not initialized — only local data was reset.");
            return;
        }

        try
        {
            Cloud.SetValue(CloudSaveKey, json, true,
                () => UnityEngine.Debug.Log("[SaveSystem] Local and cloud data were reset."),
                error => UnityEngine.Debug.LogWarning($"[SaveSystem] Cloud reset failed: {error}"));
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[SaveSystem] Cloud reset failed: {exception.Message}");
        }
    }

    public void SaveToStorage()
    {
        SavePlayerData();
    }

    private void SavePlayerData()
    {
        string json = JsonConvert.SerializeObject(cachedSaveData);

        if (json != lastSavedLocalJson)
        {
            UnityEngine.PlayerPrefs.SetString(LocalSaveKey, json);
            UnityEngine.PlayerPrefs.Save();
            lastSavedLocalJson = json;
        }

        if (!Cloud.Initialized || json == lastSavedCloudJson)
            return;

        // В редакторе Cloud пишет обычный файл (EditorCloud/Save.txt), и запись изредка падает
        // с Win32 1224 (ERROR_USER_MAPPED_FILE): файл в этот момент замаплен другим процессом.
        // Ронять из-за этого кадр незачем — lastSavedJson остаётся прежним, поэтому следующее
        // периодическое сохранение (раз в SavingPeriod секунд) просто повторит запись.
        try
        {
            Cloud.SetValue(CloudSaveKey, json, true,
                () => lastSavedCloudJson = json,
                error => UnityEngine.Debug.LogWarning($"[SaveSystem] Cloud save failed, retry in {SavingPeriod}s: {error}"));
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[SaveSystem] Сохранение не удалось, повтор через {SavingPeriod} c: {exception.Message}");
        }
    }
}

[Serializable]
public struct PlayerSaveData
{
    // Fill content of your SaveData, it can be anything that Newtonsoft can serialize

    // --- Test_IdleDefend ---
    // Мета-валюта: персистентна между уровнями и сценами (в отличие от внутриуровневых монет,
    // которые живут только в рамках одного забега и не сохраняются — см. CoinService).
    private int _cachedEmeralds;
    public static event Action OnEmeraldsChanged;

    public int Emeralds
    {
        get => _cachedEmeralds;
        set
        {
            _cachedEmeralds = value;
            OnEmeraldsChanged?.Invoke();
        }
    }

    private bool _noAds;
    public static event Action OnBuyNoAds;

    public bool NoAds
    {
        get => _noAds;
        set
        {
            _noAds = value;
            OnBuyNoAds?.Invoke();
        }
    }

    // Лучшее время выживания за забег, в секундах.
    private float _bestSurvivalTime;
    public static event Action OnBestSurvivalTimeChanged;

    public float BestSurvivalTime
    {
        get => _bestSurvivalTime;
        set
        {
            _bestSurvivalTime = value;
            OnBestSurvivalTimeChanged?.Invoke();
        }
    }

    // Индекс уровня (по позиции в LevelsConfig), следующий за последним пройденным. 0 — открыт только первый уровень.
    private int _unlockedLevelIndex;

    public int UnlockedLevelIndex
    {
        get => _unlockedLevelIndex;
        set => _unlockedLevelIndex = value;
    }

    // Лучший результат по звёздам (1-3) на каждом пройденном уровне, по LevelDefinition.LevelId.
    public System.Collections.Generic.Dictionary<int, int> LevelStars;

    public static event Action OnShopInventoryChanged;

    public static void NotifyShopInventoryChanged()
    {
        OnShopInventoryChanged?.Invoke();
    }

    public System.Collections.Generic.List<string> PurchasedShopItemIds;
    public System.Collections.Generic.Dictionary<string, string> EquippedShopItemIds;
    public System.Collections.Generic.Dictionary<string, int> BoostItemCounts;
    public System.Collections.Generic.List<string> SelectedBoostItemIds;

    // Version of the deterministic first-level tutorial completed by this player.
    public int TutorialVersion;

    // Set when Boot routes a genuinely new player directly into the first level.
    public bool HasStartedFirstGame;
}
