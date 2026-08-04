using UnityEngine;

/// <summary>
/// Отладочный сброс прогресса: зажать H + U + Y и держать ~2 секунды — чистится локальный
/// сейв, облако и кэш в памяти. Нужен, потому что очистка облака в консоли Яндекса сама по себе
/// ничего не даёт: автосейв через несколько секунд заливает старые данные обратно.
///
/// Почему удержание, а не одно нажатие: чит остаётся и в релизной сборке (иначе им нельзя
/// воспользоваться на черновике Яндекса), поэтому случайное срабатывание должно быть исключено.
/// Зажать три конкретные клавиши и продержать 2 секунды случайно невозможно, а результат виден
/// на экране — сброс не происходит молча.
///
/// ⚠️ WebGL: клавиатура доходит до Unity только когда канвас в фокусе. Если комбо не срабатывает
/// на загрузочном экране — кликнуть по игре и повторить. Объект живёт через все сцены, так что
/// сбросить можно из любого места, не обязательно в Boot.
///
/// Ставится сам, без префабов и правки сцен — специально, чтобы работать в любой игре из коробки.
/// </summary>
public class SaveResetCheat : MonoBehaviour
{
    private const KeyCode FirstKey = KeyCode.H;
    private const KeyCode SecondKey = KeyCode.U;
    private const KeyCode ThirdKey = KeyCode.Y;

    private const float HoldSeconds = 2f;
    private const float MessageSeconds = 4f;

    private float _heldSeconds;
    private float _messageHideTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Spawn()
    {
        var runner = new GameObject("[SaveResetCheat]");
        runner.AddComponent<SaveResetCheat>();
        DontDestroyOnLoad(runner);
    }

    private void Update()
    {
        if (!Input.GetKey(FirstKey) || !Input.GetKey(SecondKey) || !Input.GetKey(ThirdKey))
        {
            _heldSeconds = 0f;
            return;
        }

        // Отрицательное значение = уже сработало на этом удержании; ждём, пока клавиши отпустят.
        if (_heldSeconds < 0f)
            return;

        _heldSeconds += Time.unscaledDeltaTime;
        if (_heldSeconds < HoldSeconds)
            return;

        _heldSeconds = -1f;
        SaveSystem.ResetAllData();
        _messageHideTime = Time.unscaledTime + MessageSeconds;
    }

    private void OnGUI()
    {
        if (Time.unscaledTime > _messageHideTime)
            return;

        const int width = 320;
        const int height = 60;

        var area = new Rect((Screen.width - width) * 0.5f, 20f, width, height);

        GUI.Box(area, GUIContent.none);
        GUI.Label(new Rect(area.x + 10f, area.y + 10f, width - 20f, height - 20f),
            "Saves cleared (local + cloud).\nReload the page to start fresh.");
    }
}
