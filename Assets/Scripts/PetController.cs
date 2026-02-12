using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PetController : MonoBehaviour
{
    [Header("Drift")]
    [SerializeField] private float driftIntervalSeconds = 60f;
    [SerializeField] private float editorTimeScale = 1f;

    private PetState state;
    private float driftTimer;

    private Text hungerText;
    private Text moodText;
    private Text energyText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PetController>() != null) return;

        var go = new GameObject("PetController");
        go.AddComponent<PetController>();
    }

    private void Awake()
    {
        state = SaveSystem.Load();
        BuildUiIfMissing();
        RefreshUi();
    }

    private void Update()
    {
        float scale = Application.isEditor ? Mathf.Max(0.01f, editorTimeScale) : 1f;
        driftTimer += Time.deltaTime * scale;

        if (driftTimer >= driftIntervalSeconds)
        {
            driftTimer = 0f;
            ApplyDelta(hungerDelta: +2, moodDelta: -1, energyDelta: -1);
        }
    }

    public void Feed()
    {
        ApplyDelta(hungerDelta: -15, moodDelta: +2, energyDelta: 0);
    }

    public void Play()
    {
        ApplyDelta(hungerDelta: +5, moodDelta: +10, energyDelta: -5);
    }

    public void Sleep()
    {
        ApplyDelta(hungerDelta: +5, moodDelta: +2, energyDelta: +15);
    }

    private void ApplyDelta(int hungerDelta, int moodDelta, int energyDelta)
    {
        state.Hunger += hungerDelta;
        state.Mood += moodDelta;
        state.Energy += energyDelta;
        state.ClampAll();

        SaveSystem.Save(state);
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (hungerText != null) hungerText.text = $"Hunger: {state.Hunger}/100";
        if (moodText != null) moodText.text = $"Mood: {state.Mood}/100";
        if (energyText != null) energyText.text = $"Energy: {state.Energy}/100";
    }

    private void OnApplicationQuit()
    {
        SaveSystem.Save(state);
    }

    private void BuildUiIfMissing()
    {
        EnsureEventSystem();

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var root = new GameObject("TamagotchiUI", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(900, 1500);

        hungerText = CreateText("HungerText", root.transform, font, new Vector2(0, 550), "Hunger: 50/100");
        moodText = CreateText("MoodText", root.transform, font, new Vector2(0, 470), "Mood: 50/100");
        energyText = CreateText("EnergyText", root.transform, font, new Vector2(0, 390), "Energy: 50/100");

        var petImage = new GameObject("BulldogPlaceholder", typeof(RectTransform), typeof(Image));
        petImage.transform.SetParent(root.transform, false);
        var petRect = petImage.GetComponent<RectTransform>();
        petRect.sizeDelta = new Vector2(480, 480);
        petRect.anchoredPosition = new Vector2(0, 80);
        var image = petImage.GetComponent<Image>();
        image.color = new Color(0.75f, 0.55f, 0.35f, 1f);

        CreateText("BulldogLabel", petImage.transform, font, Vector2.zero, "BULLDOG");

        CreateButton("FeedButton", root.transform, font, new Vector2(-220, -420), "Feed", Feed);
        CreateButton("PlayButton", root.transform, font, new Vector2(0, -420), "Play", Play);
        CreateButton("SleepButton", root.transform, font, new Vector2(220, -420), "Sleep", Sleep);
    }

    private static Text CreateText(string name, Transform parent, Font font, Vector2 anchoredPos, string textValue)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 70);
        rect.anchoredPosition = anchoredPos;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = textValue;
        return text;
    }

    private static void CreateButton(string name, Transform parent, Font font, Vector2 anchoredPos, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 90);
        rect.anchoredPosition = anchoredPos;

        var image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        var button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = CreateText(name + "Label", buttonGo.transform, font, Vector2.zero, label);
        text.fontSize = 34;
        text.raycastTarget = false;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
    }
}
