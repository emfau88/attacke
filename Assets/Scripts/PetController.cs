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
            questText.text = $"Quest: Keep Mood > {cfg.questMoodThreshold} for {cfg.questTargetSeconds:0}s ({questProgress:0.0}s)";
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
        hungerText.text = $"Hunger {state.Hunger}/100";
        moodText.text = $"Mood {state.Mood}/100";
        energyText.text = $"Energy {state.Energy}/100";
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

        var root = new GameObject("Phase1UI", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(canvas.transform, false);
        uiRoot = root.GetComponent<RectTransform>();
        uiRoot.anchorMin = new Vector2(0.5f, 0.5f); uiRoot.anchorMax = new Vector2(0.5f, 0.5f); uiRoot.sizeDelta = new Vector2(840, 1480);
        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14; layout.padding = new RectOffset(16,16,16,16); layout.childAlignment = TextAnchor.UpperCenter;

        CreateText(root.transform, font, "Bulldog Core Loop", 46);
        dogStateText = CreateText(root.transform, font, "State: Idle", 30);

        var dog = new GameObject("Dog", typeof(RectTransform), typeof(Image));
        dog.transform.SetParent(root.transform, false);
        var dr = dog.GetComponent<RectTransform>(); dr.sizeDelta = new Vector2(360, 360);
        dogImage = dog.GetComponent<Image>(); dogImage.color = new Color(0.85f,0.75f,0.6f);

        hungerBar = CreateBar(root.transform, new Color(1f, 0.6f, 0.2f));
        hungerText = CreateText(root.transform, font, "Hunger", 28);
        moodBar = CreateBar(root.transform, new Color(0.35f, 0.65f, 1f));
        moodText = CreateText(root.transform, font, "Mood", 28);
        energyBar = CreateBar(root.transform, new Color(0.35f, 0.85f, 0.4f));
        energyText = CreateText(root.transform, font, "Energy", 28);

        var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonRow.transform.SetParent(root.transform, false);
        var h = buttonRow.GetComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.childAlignment = TextAnchor.MiddleCenter;

        feedBtn = CreateButton(buttonRow.transform, font, "Feed", Feed);
        playBtn = CreateButton(buttonRow.transform, font, "Play", Play);
        sleepBtn = CreateButton(buttonRow.transform, font, "Sleep", Sleep);

        cooldownText = CreateText(root.transform, font, "", 24);
        tickText = CreateText(root.transform, font, "", 24);
        questText = CreateText(root.transform, font, "Quest", 24);
        saveText = CreateText(root.transform, font, "", 22);

        reviveBtn = CreateButton(root.transform, font, "Revive", Revive);
        reviveBtn.gameObject.SetActive(false);

#if UNITY_EDITOR
        debugResetBtn = CreateButton(root.transform, font, "Dev Reset", DevReset);
#endif
    }

    private static Text CreateText(Transform parent, Font font, string text, int size)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 52);
        return t;
    }

    private static Slider CreateBar(Transform parent, Color fill)
    {
        var sgo = new GameObject("Bar", typeof(RectTransform), typeof(Slider));
        sgo.transform.SetParent(parent, false);
        var s = sgo.GetComponent<Slider>(); s.minValue = 0; s.maxValue = 100; s.interactable = false;
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image)); bg.transform.SetParent(sgo.transform,false);
        bg.GetComponent<Image>().color = new Color(0.2f,0.2f,0.2f,0.9f);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(sgo.transform,false);
        var fg = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fg.transform.SetParent(fillArea.transform,false);
        fg.GetComponent<Image>().color = fill;
        s.targetGraphic = fg.GetComponent<Image>();
        s.fillRect = fg.GetComponent<RectTransform>();

        var r = sgo.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(700, 26);
        Stretch(bg.GetComponent<RectTransform>()); Stretch(fillArea.GetComponent<RectTransform>()); Stretch(fg.GetComponent<RectTransform>());
        return s;
    }

    private static Button CreateButton(Transform parent, Font font, string label, UnityEngine.Events.UnityAction action)
    {
        var bgo = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        bgo.transform.SetParent(parent, false);
        bgo.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);
        bgo.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        var b = bgo.GetComponent<Button>();
        b.onClick.AddListener(action);
        var t = new GameObject("Label", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(bgo.transform, false);
        var tt = t.GetComponent<Text>(); tt.font = font; tt.text = label; tt.fontSize = 30; tt.alignment = TextAnchor.MiddleCenter; tt.color = Color.white;
        t.GetComponent<RectTransform>().sizeDelta = new Vector2(190, 70);
        return b;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
