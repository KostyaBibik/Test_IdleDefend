using System;
using Kimicu.YandexGames;
using Newtonsoft.Json;

public class SaveSystem : Singleton<SaveSystem>
{
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

    private static string lastSavedJson;

    private const int SavingPeriod = 4;

    public static int GetUnlockedLevelIndex()
    {
        return SaveData.UnlockedLevelIndex;
    }

    public static int GetLevelStars(int levelId)
    {
        EnsureRuntimeCollections();
        return cachedSaveData.LevelStars.TryGetValue(levelId, out var stars) ? stars : 0;
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
        string json = Cloud.GetValue("SaveData", "");

        PlayerSaveData saveData;

        if (!string.IsNullOrEmpty(json) && !string.IsNullOrWhiteSpace(json))
            saveData = JsonConvert.DeserializeObject<PlayerSaveData>(json);
        else
            saveData = new PlayerSaveData
            {
                Money = 0,
                NoAds = false,
                ProgressInitialized = false,
                BestSurvivalTime = 0f,
            };

        // It is very imporant to add null-checks for any collections you add in future updates
        // Since NewtonsoftJson is creating nulls when reading jsons with no info about collections
        EnsureRuntimeCollections(ref saveData);

        IsDataLoaded = true;
        return saveData;
    }

    private static void EnsureRuntimeCollections()
    {
        var saveData = SaveData;
        EnsureRuntimeCollections(ref saveData);
        cachedSaveData = saveData;
    }

    private static void EnsureRuntimeCollections(ref PlayerSaveData saveData)
    {
        if (saveData.LevelStars == null)
            saveData.LevelStars = new System.Collections.Generic.Dictionary<int, int>();
    }

    public void SaveToStorage()
    {
        SavePlayerData();
    }

    private void SavePlayerData()
    {
        string json = JsonConvert.SerializeObject(cachedSaveData);

        if (json == lastSavedJson)
            return;
        Cloud.SetValue("SaveData", json, true, () => lastSavedJson = json);
    }
}

[Serializable]
public struct PlayerSaveData
{
    // Fill content of your SaveData, it can be anything that Newtonsoft can serialize
    // Example of reactive data:
    private int _cachedMoney;
    public static event Action OnMoneyChanged;

    public int Money
    {
        get => _cachedMoney;
        set
        {
            _cachedMoney = value;
            OnMoneyChanged?.Invoke();
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

    // --- Test_IdleDefend ---
    // Выставляется один раз при самом первом запуске, чтобы отличить «новый игрок»
    // от «игрок потратил все монеты» (в обоих случаях Money == 0).
    private bool _progressInitialized;

    public bool ProgressInitialized
    {
        get => _progressInitialized;
        set => _progressInitialized = value;
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
}
