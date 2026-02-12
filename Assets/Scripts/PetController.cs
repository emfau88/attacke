using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PetController : MonoBehaviour
{
    [Header("Drift")]
    [SerializeField] private float driftIntervalSeconds = 60f;
    [SerializeField] private float editorTimeScale = 1f;
    [SerializeField] private int maxOfflineHours = 12;

    private PetState state;
    private float driftTimer;
    private bool inputLocked;

    private Slider hungerBar;
    private Slider moodBar;
    private Slider energyBar;
    private Text hungerText;
    private Text moodText;
    private Text energyText;
    private Text toastText;
    private Button feedButton;
    private Button playButton;
    private Button sleepButton;

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
        ApplyOfflineProgression();
        BuildUiIfMissing();
        RefreshUi();
        SaveSystem.Save(state);
    }

    private void Update()
    {
        float scale = Application.isEditor ? Mathf.Max(0.01f, editorTimeScale) : 1f;
        driftTimer += Time.deltaTime * scale;

        if (driftTimer >= driftIntervalSeconds)
        {
            driftTimer = 0f;
            ApplyDelta(+2, -1, -1, "Time passes quietly.");
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
        {
            SaveSystem.ResetToDefault();
            state = SaveSystem.DefaultState();
            RefreshUi();
            ShowToast("Save reset to defaults.");
            SaveSystem.Save(state);
        }
#endif
    }

    public void Feed()
    {
        if (inputLocked) return;
        ApplyDelta(-15, +2, 0, RandomFrom(new[]
        {
            "Bulldog enjoyed that meal.",
            "A small snack, big trust.",
            "Bowl empty, tail calm."
        }));
        StartCoroutine(ButtonCooldown());
    }

    public void Play()
    {
        if (inputLocked) return;
        ApplyDelta(+5, +10, -5, RandomFrom(new[]
        {
            "A quick game lifted the mood.",
            "Playtime brought happy steps.",
            "Bulldog had fun and slowed down."
        }));
        StartCoroutine(ButtonCooldown());
    }

    public void Sleep()
    {
        if (inputLocked) return;
        ApplyDelta(+5, +2, +15, RandomFrom(new[]
        {
            "A good nap restored energy.",
            "Quiet rest made things better.",
            "Bulldog woke up refreshed."
        }));
        StartCoroutine(ButtonCooldown());
    }

    private void ApplyDelta(int hungerDelta, int moodDelta, int energyDelta, string toast)
    {
        state.Hunger += hungerDelta;
        state.Mood += moodDelta;
        state.Energy += energyDelta;
        state.ClampAll();

        SaveSystem.Save(state);
        RefreshUi();
        ShowToast(toast);
    }

    private void ApplyOfflineProgression()
    {
        if (state.LastSavedUnix <= 0) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsedSeconds = Mathf.Max(0, (int)(now - state.LastSavedUnix));
        long cappedSeconds = Mathf.Min(elapsedSeconds, maxOfflineHours * 3600L);
        int steps = Mathf.FloorToInt(cappedSeconds / Mathf.Max(1f, driftIntervalSeconds));

        if (steps <= 0) return;

        state.Hunger += 2 * steps;
        state.Mood -= 1 * steps;
        state.Energy -= 1 * steps;
        state.ClampAll();
    }

    private void RefreshUi()
    {
        if (hungerBar != null) hungerBar.value = state.Hunger;
        if (moodBar != null) moodBar.value = state.Mood;
        if (energyBar != null) energyBar.value = state.Energy;

        if (hungerText != null) hungerText.text = $"Hunger: {state.Hunger}/100";
        if (moodText != null) moodText.text = $"Mood: {state.Mood}/100";
        if (energyText != null) energyText.text = $"Energy: {state.Energy}/100";
    }

    private void ShowToast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        StopCoroutine("ToastFade");
        StartCoroutine("ToastFade");
    }

    private IEnumerator ToastFade()
    {
        if (toastText == null) yield break;

        toastText.enabled = true;
        var color = toastText.color;
        color.a = 1f;
        toastText.color = color;

        yield return new WaitForSeconds(1.4f);

        color.a = 0f;
        toastText.color = color;
    }

    private IEnumerator ButtonCooldown()
    {
        inputLocked = true;
        SetButtonsInteractable(false);
        yield return new WaitForSeconds(0.2f);
        SetButtonsInteractable(true);
        inputLocked = false;
    }

    private void SetButtonsInteractable(bool value)
    {
        if (feedButton != null) feedButton.interactable = value;
        if (playButton != null) playButton.interactable = value;
        if (sleepButton != null) sleepButton.interactable = value;
    }

    private static string RandomFrom(string[] options)
    {
        if (options == null || options.Length == 0) return string.Empty;
        return options[UnityEngine.Random.Range(0, options.Length)];
    }

    private void OnApplicationQuit()
    {
        SaveSystem.Save(state);
    }

    private void BuildUiIfMissing()
    {
        EnsureEventSystem();

        var existingRoot = GameObject.Find("TamagotchiUI");
        if (existingRoot != null)
        {
            BindExistingReferences(existingRoot.transform);
            return;
        }

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

        var root = new GameObject("TamagotchiUI", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(canvas.transform, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(850, 1450);

        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(20, 20, 20, 20);

        CreateText("Header", root.transform, font, new Vector2(760, 90), "Bulldog Tamagotchi", 52);

        var petImageGo = new GameObject("BulldogPlaceholder", typeof(RectTransform), typeof(Image));
        petImageGo.transform.SetParent(root.transform, false);
        var petImageRect = petImageGo.GetComponent<RectTransform>();
        petImageRect.sizeDelta = new Vector2(420, 420);
        var petImage = petImageGo.GetComponent<Image>();
        petImage.color = new Color(0.75f, 0.55f, 0.35f, 1f);

        var sprite = Resources.Load<Sprite>("bulldog_placeholder");
        if (sprite != null) petImage.sprite = sprite;

        CreateText("BulldogLabel", petImageGo.transform, font, new Vector2(320, 80), "BULLDOG", 36);

        CreateStatRow(root.transform, font, "Hunger", out hungerBar, out hungerText);
        CreateStatRow(root.transform, font, "Mood", out moodBar, out moodText);
        CreateStatRow(root.transform, font, "Energy", out energyBar, out energyText);

        var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonRow.transform.SetParent(root.transform, false);
        var buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 16;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;

        feedButton = CreateButton("FeedButton", buttonRow.transform, font, "Feed", Feed);
        playButton = CreateButton("PlayButton", buttonRow.transform, font, "Play", Play);
        sleepButton = CreateButton("SleepButton", buttonRow.transform, font, "Sleep", Sleep);

        toastText = CreateText("Toast", root.transform, font, new Vector2(760, 80), "Ready.", 30);
        var c = toastText.color;
        c.a = 0f;
        toastText.color = c;
    }

    private void BindExistingReferences(Transform root)
    {
        hungerBar = root.Find("Hunger/Bar")?.GetComponent<Slider>();
        moodBar = root.Find("Mood/Bar")?.GetComponent<Slider>();
        energyBar = root.Find("Energy/Bar")?.GetComponent<Slider>();

        hungerText = root.Find("Hunger/Label")?.GetComponent<Text>();
        moodText = root.Find("Mood/Label")?.GetComponent<Text>();
        energyText = root.Find("Energy/Label")?.GetComponent<Text>();
        toastText = root.Find("Toast")?.GetComponent<Text>();

        feedButton = root.Find("Buttons/FeedButton")?.GetComponent<Button>();
        playButton = root.Find("Buttons/PlayButton")?.GetComponent<Button>();
        sleepButton = root.Find("Buttons/SleepButton")?.GetComponent<Button>();
    }

    private static void CreateStatRow(Transform parent, Font font, string statName, out Slider slider, out Text label)
    {
        var row = new GameObject(statName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        row.transform.SetParent(parent, false);
        var rowLayout = row.GetComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 6;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;

        label = CreateText("Label", row.transform, font, new Vector2(700, 60), statName + ": 50/100", 34);

        var sliderGo = new GameObject("Bar", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.interactable = false;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderGo.transform, false);
        var bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.2f, 0.75f, 0.35f, 1f);

        slider.targetGraphic = fillImage;
        slider.fillRect = fill.GetComponent<RectTransform>();

        var rect = sliderGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(680, 28);

        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var faRect = fillArea.GetComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0, 0);
        faRect.anchorMax = new Vector2(1, 1);
        faRect.offsetMin = Vector2.zero;
        faRect.offsetMax = Vector2.zero;

        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private static Text CreateText(string name, Transform parent, Font font, Vector2 size, string textValue, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = textValue;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonGo.transform.SetParent(parent, false);

        var layout = buttonGo.GetComponent<LayoutElement>();
        layout.preferredWidth = 220;
        layout.preferredHeight = 90;

        var image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        var button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = CreateText("Label", buttonGo.transform, font, new Vector2(180, 70), label, 34);
        text.raycastTarget = false;
        return button;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
