using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PetController : MonoBehaviour
{
    private PetState state;
    private Phase1Config cfg;

    private float driftTimer;
    private float displayedHunger;
    private float displayedMood;
    private float displayedEnergy;

    private Canvas canvas;
    private RectTransform uiRoot;
    private Image bgTint;
    private Image dogImage;
    private Text dogStateText;
    private Text hungerText;
    private Text moodText;
    private Text energyText;
    private Text tickText;
    private Text saveText;
    private Text questText;
    private Text cooldownText;
    private Slider hungerBar;
    private Slider moodBar;
    private Slider energyBar;
    private Button feedBtn;
    private Button playBtn;
    private Button sleepBtn;
    private Button reviveBtn;
    private Button debugResetBtn;

    private bool inFailState;
    private bool onCooldown;
    private float cooldownRemaining;
    private float questProgress;
    private bool testFastTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PetController>() != null) return;
        var go = new GameObject("PetController");
        go.AddComponent<PetController>();
    }

    private void Awake()
    {
        cfg = Phase1Config.CreateRuntimeDefaults();
        state = SaveSystem.Load();
        BuildUi();
        displayedHunger = state.Hunger;
        displayedMood = state.Mood;
        displayedEnergy = state.Energy;
        RefreshVisuals(true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            testFastTime = !testFastTime;
            ShowToast(testFastTime ? "Time x5 ON" : "Time x5 OFF", Color.cyan);
        }
#endif

        var ts = testFastTime ? 5f : 1f;
        driftTimer += Time.deltaTime * ts;

        if (driftTimer >= cfg.driftIntervalSeconds)
        {
            driftTimer = 0f;
            ApplyDelta(cfg.driftHungerDelta, cfg.driftMoodDelta, cfg.driftEnergyDelta, "Tick");
            if (tickText != null) tickText.text = "Tick: stats drifted";
        }

        if (onCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            var remain = Mathf.Max(0f, cooldownRemaining);
            if (cooldownText != null) cooldownText.text = $"Cooldown {remain:0.0}s";
            if (cooldownRemaining <= 0f)
            {
                onCooldown = false;
                cooldownText.text = "";
                SetButtonsInteractable(!inFailState);
            }
        }

        displayedHunger = Mathf.Lerp(displayedHunger, state.Hunger, Time.deltaTime * cfg.uiLerpSpeed);
        displayedMood = Mathf.Lerp(displayedMood, state.Mood, Time.deltaTime * cfg.uiLerpSpeed);
        displayedEnergy = Mathf.Lerp(displayedEnergy, state.Energy, Time.deltaTime * cfg.uiLerpSpeed);
        RefreshBarsOnly();

        if (state.Mood > cfg.questMoodThreshold && !inFailState)
            questProgress = Mathf.Min(cfg.questTargetSeconds, questProgress + Time.deltaTime);
        else
            questProgress = Mathf.Max(0f, questProgress - Time.deltaTime * 0.5f);

        if (questText != null)
        {
            questText.text = $"Mini-Quest: Mood > {cfg.questMoodThreshold} for {cfg.questTargetSeconds:0}s ({questProgress:0.0}s)";
        }
    }

    private void Feed() => UseAction(-15, +2, 0, "Feed", new Color(1f, 0.7f, 0.2f));
    private void Play() => UseAction(+5, +10, -5, "Play", new Color(0.4f, 0.8f, 1f));
    private void Sleep() => UseAction(+5, +2, +15, "Sleep", new Color(0.5f, 0.9f, 0.5f));

    private void UseAction(int dh, int dm, int de, string label, Color color)
    {
        if (onCooldown || inFailState) return;
        AudioManager.Instance?.PlayUiClick();
        AudioManager.Instance?.PlayUiClick();
        AudioManager.Instance?.PlayUiClick();
        ApplyDelta(dh, dm, de, label);
        StartCoroutine(PulseButton(label));
        FloatingText.Spawn(uiRoot, hungerText.font, label, new Vector2(0, -120), color);
        onCooldown = true;
        cooldownRemaining = cfg.cooldownSeconds;
        SetButtonsInteractable(false);
    }

    private void ApplyDelta(int dh, int dm, int de, string reason)
    {
        state.Hunger += dh;
        state.Mood += dm;
        state.Energy += de;
        state.ClampAll();

        SaveSystem.Save(state);
        if (saveText != null)
        {
            saveText.text = "Saved ✓";
            StopCoroutine(nameof(ClearSaveLabel));
            StartCoroutine(nameof(ClearSaveLabel));
        }

        RefreshVisuals();
        ShowToast(reason, Color.white);
    }

    private IEnumerator ClearSaveLabel()
    {
        yield return new WaitForSeconds(1f);
        if (saveText != null) saveText.text = "";
    }

    private IEnumerator PulseButton(string label)
    {
        Button b = label == "Feed" ? feedBtn : label == "Play" ? playBtn : sleepBtn;
        if (b == null) yield break;
        var rt = (RectTransform)b.transform;
        var baseScale = rt.localScale;
        rt.localScale = baseScale * 1.08f;
        yield return new WaitForSeconds(0.15f);
        rt.localScale = baseScale;
    }

    private void RefreshVisuals(bool immediate = false)
    {
        var stateName = state.EvaluateState(cfg.warningThreshold);
        inFailState = stateName == DogMoodState.Sick;
        if (dogStateText != null) dogStateText.text = $"State: {stateName}";

        if (dogImage != null)
        {
            switch (stateName)
            {
                case DogMoodState.Happy: dogImage.color = new Color(1f, 0.9f, 0.4f); break;
                case DogMoodState.Tired: dogImage.color = new Color(0.5f, 0.6f, 1f); break;
                case DogMoodState.Hungry: dogImage.color = new Color(1f, 0.6f, 0.3f); break;
                case DogMoodState.Sick: dogImage.color = new Color(0.5f, 0.5f, 0.5f); break;
                default: dogImage.color = new Color(0.85f, 0.75f, 0.6f); break;
            }
        }

        if (bgTint != null)
        {
            if (state.IsWarning(cfg.warningThreshold))
                bgTint.color = new Color(1f, 0.3f, 0.3f, 0.12f);
            else
                bgTint.color = new Color(0f, 0f, 0f, 0f);

            if (stateName == DogMoodState.Happy) bgTint.color = new Color(1f, 0.8f, 0.4f, 0.08f);
            if (stateName == DogMoodState.Tired) bgTint.color = new Color(0.2f, 0.3f, 0.7f, 0.1f);
            if (stateName == DogMoodState.Hungry) bgTint.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
            if (stateName == DogMoodState.Sick) bgTint.color = new Color(0.2f, 0.2f, 0.2f, 0.28f);
        }

        reviveBtn.gameObject.SetActive(inFailState);
        SetButtonsInteractable(!inFailState && !onCooldown);

        if (immediate)
        {
            displayedHunger = state.Hunger;
            displayedMood = state.Mood;
            displayedEnergy = state.Energy;
        }
        RefreshBarsOnly();
    }

    private void RefreshBarsOnly()
    {
        hungerBar.value = displayedHunger;
        moodBar.value = displayedMood;
        energyBar.value = displayedEnergy;
        hungerText.text = $"{state.Hunger}%";
        moodText.text = $"{state.Mood}%";
        energyText.text = $"{state.Energy}%";
    }

    private void Revive()
    {
        state = new PetState(50, 50, 50);
        SaveSystem.Save(state);
        inFailState = false;
        RefreshVisuals(true);
        ShowToast("Revived", Color.green);
    }

    private void DevReset()
    {
        SaveSystem.ResetToDefault();
        state = SaveSystem.DefaultState();
        questProgress = 0f;
        RefreshVisuals(true);
        ShowToast("Reset", Color.white);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (feedBtn != null) feedBtn.interactable = value;
        if (playBtn != null) playBtn.interactable = value;
        if (sleepBtn != null) sleepBtn.interactable = value;
    }

    private void ShowToast(string message, Color color)
    {
        FloatingText.Spawn(uiRoot, hungerText.font, message, new Vector2(0, -50), color);
    }

    private void BuildUi()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = cgo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        var overlay = new GameObject("WarnOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        bgTint = overlay.GetComponent<Image>();
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;

        var root = new GameObject("Phase1UI", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        uiRoot = root.GetComponent<RectTransform>();
        uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.offsetMin = Vector2.zero; uiRoot.offsetMax = Vector2.zero;

        var title = CreateText(root.transform, font, "Bulldog Buddy", 48, TextAnchor.MiddleCenter, Color.white);
        Anchor(title.rectTransform, 0.5f, 1f, 0.5f, 1f, new Vector2(0, -54), new Vector2(760, 64));

        CreateStatusBar(root.transform, font, "Hunger", new Color(1f, 0.63f, 0.2f), out hungerBar, out hungerText, 1f, -170f, 51);
        CreateStatusBar(root.transform, font, "Mood", new Color(0.3f, 0.76f, 1f), out moodBar, out moodText, 1f, -300f, 75);
        CreateStatusBar(root.transform, font, "Energy", new Color(0.45f, 0.88f, 0.38f), out energyBar, out energyText, 1f, -430f, 73);

        var questPanel = new GameObject("QuestPanel", typeof(RectTransform), typeof(Image));
        questPanel.transform.SetParent(root.transform, false);
        var qp = questPanel.GetComponent<Image>();
        qp.color = new Color(0.22f, 0.24f, 0.38f, 0.95f);
        Anchor(questPanel.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, new Vector2(0, -580), new Vector2(820, 140));

        questText = CreateText(questPanel.transform, font, "Quest", 36, TextAnchor.MiddleCenter, Color.white);
        Anchor(questText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 0), new Vector2(780, 110));

        var dog = new GameObject("Dog", typeof(RectTransform), typeof(Image));
        dog.transform.SetParent(root.transform, false);
        var dr = dog.GetComponent<RectTransform>();
        Anchor(dr, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -80), new Vector2(440, 440));
        dogImage = dog.GetComponent<Image>();
        dogImage.color = new Color(0.85f, 0.75f, 0.6f);

        dogStateText = CreateText(root.transform, font, "State: Idle", 34, TextAnchor.MiddleCenter, Color.white);
        Anchor(dogStateText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -330), new Vector2(860, 56));

        saveText = CreateText(root.transform, font, "", 34, TextAnchor.MiddleCenter, new Color(0.9f, 1f, 0.95f));
        Anchor(saveText.rectTransform, 0.79f, 0.28f, 0.79f, 0.28f, Vector2.zero, new Vector2(260, 70));

        feedBtn = CreateActionButton(root.transform, font, "Feed", "🍖", new Color(0.31f, 0.84f, 0.49f), new Vector2(-300, -760), Feed);
        playBtn = CreateActionButton(root.transform, font, "Play", "🔴", new Color(1f, 0.66f, 0.23f), new Vector2(0, -760), Play);
        sleepBtn = CreateActionButton(root.transform, font, "Sleep", "🌙", new Color(0.39f, 0.56f, 1f), new Vector2(300, -760), Sleep);

        cooldownText = CreateText(root.transform, font, "", 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 1f));
        Anchor(cooldownText.rectTransform, 0.5f, 0f, 0.5f, 0f, new Vector2(0, 170), new Vector2(740, 50));
        tickText = CreateText(root.transform, font, "", 26, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 1f));
        Anchor(tickText.rectTransform, 0.5f, 0f, 0.5f, 0f, new Vector2(0, 126), new Vector2(740, 44));

        reviveBtn = CreateActionButton(root.transform, font, "Revive", "↺", new Color(0.75f, 0.24f, 0.24f), new Vector2(0, -620), Revive);
        reviveBtn.gameObject.SetActive(false);

#if UNITY_EDITOR
        debugResetBtn = CreateActionButton(root.transform, font, "Reset", "⚙", new Color(0.35f, 0.35f, 0.35f), new Vector2(0, -900), DevReset);
#endif
    }

    private static void CreateStatusBar(Transform parent, Font font, string label, Color fillColor, out Slider slider, out Text valueText, float anchorX, float y, int seedValue)
    {
        var bar = new GameObject($"{label}Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        var barBg = bar.GetComponent<Image>();
        barBg.color = new Color(0.13f, 0.16f, 0.3f, 0.95f);
        Anchor(bar.GetComponent<RectTransform>(), anchorX, 1f, anchorX, 1f, new Vector2(0, y), new Vector2(940, 110));

        var fillRoot = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillRoot.transform.SetParent(bar.transform, false);
        var fillImg = fillRoot.GetComponent<Image>();
        fillImg.color = fillColor;
        Anchor(fillRoot.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f, new Vector2(12, 12), new Vector2(620, 86));

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(bar.transform, false);
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 100; slider.interactable = false; slider.value = seedValue;
        Stretch(sliderGo.GetComponent<RectTransform>());
        slider.fillRect = fillRoot.GetComponent<RectTransform>();
        slider.targetGraphic = fillImg;

        var labelText = CreateText(bar.transform, font, label, 46, TextAnchor.MiddleLeft, Color.white);
        Anchor(labelText.rectTransform, 0f, 0.5f, 0f, 0.5f, new Vector2(96, 0), new Vector2(420, 70));

        valueText = CreateText(bar.transform, font, "0%", 46, TextAnchor.MiddleRight, Color.white);
        Anchor(valueText.rectTransform, 1f, 0.5f, 1f, 0.5f, new Vector2(-24, 0), new Vector2(240, 70));
    }

    private static Text CreateText(Transform parent, Font font, string text, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    private static Button CreateActionButton(Transform parent, Font font, string label, string icon, Color color, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(label + "Action", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Anchor(root.GetComponent<RectTransform>(), 0.5f, 0f, 0.5f, 0f, pos, new Vector2(260, 360));

        var btnGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(root.transform, false);
        var img = btnGo.GetComponent<Image>();
        img.color = color;
        var rt = btnGo.GetComponent<RectTransform>();
        Anchor(rt, 0.5f, 1f, 0.5f, 1f, new Vector2(0, -130), new Vector2(240, 240));

        var b = btnGo.GetComponent<Button>();
        b.onClick.AddListener(action);

        var iconText = CreateText(btnGo.transform, font, icon, 72, TextAnchor.MiddleCenter, Color.white);
        Anchor(iconText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(170, 170));

        var labelBg = new GameObject("LabelBg", typeof(RectTransform), typeof(Image));
        labelBg.transform.SetParent(root.transform, false);
        labelBg.GetComponent<Image>().color = new Color(0.17f, 0.2f, 0.35f, 0.95f);
        Anchor(labelBg.GetComponent<RectTransform>(), 0.5f, 0f, 0.5f, 0f, new Vector2(0, 24), new Vector2(220, 78));

        var text = CreateText(labelBg.transform, font, label, 44, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        return b;
    }

    private static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
