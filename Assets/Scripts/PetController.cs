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
    private Image dogEarLeft;
    private Image dogEarRight;
    private Image dogInnerEarLeft;
    private Image dogInnerEarRight;
    private Image dogMuzzle;
    private Image dogNose;
    private Image dogMouth;
    private Image dogEyeLeft;
    private Image dogEyeRight;
    private RectTransform dogRoot;
    private Image saveBadgeBg;
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
            if (saveBadgeBg != null) saveBadgeBg.gameObject.SetActive(true);
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
        if (saveBadgeBg != null) saveBadgeBg.gameObject.SetActive(false);
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

        UpdateDogVisualState(stateName);

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

    private void UpdateDogVisualState(DogMoodState stateName)
    {
        if (dogImage == null) return;

        var baseColor = new Color(0.85f, 0.75f, 0.6f);
        var muzzleColor = new Color(0.95f, 0.89f, 0.8f);
        var innerEarColor = new Color(0.95f, 0.64f, 0.62f);
        var eyeColor = new Color(0.1f, 0.12f, 0.18f);
        var noseColor = new Color(0.18f, 0.14f, 0.17f);
        var mouthColor = new Color(0.52f, 0.24f, 0.24f);
        var yOffset = -80f;
        var earScale = 1f;

        switch (stateName)
        {
            case DogMoodState.Happy:
                baseColor = new Color(0.95f, 0.75f, 0.42f);
                muzzleColor = new Color(1f, 0.93f, 0.82f);
                innerEarColor = new Color(1f, 0.7f, 0.68f);
                eyeColor = new Color(0.07f, 0.08f, 0.14f);
                noseColor = new Color(0.12f, 0.1f, 0.12f);
                mouthColor = new Color(0.7f, 0.26f, 0.3f);
                yOffset = -70f;
                earScale = 1.05f;
                break;
            case DogMoodState.Tired:
                baseColor = new Color(0.65f, 0.66f, 0.86f);
                muzzleColor = new Color(0.86f, 0.88f, 0.95f);
                innerEarColor = new Color(0.84f, 0.74f, 0.82f);
                eyeColor = new Color(0.2f, 0.22f, 0.34f);
                noseColor = new Color(0.22f, 0.22f, 0.28f);
                mouthColor = new Color(0.42f, 0.32f, 0.42f);
                yOffset = -95f;
                earScale = 0.92f;
                break;
            case DogMoodState.Hungry:
                baseColor = new Color(0.98f, 0.62f, 0.35f);
                muzzleColor = new Color(0.95f, 0.86f, 0.75f);
                innerEarColor = new Color(0.94f, 0.58f, 0.5f);
                eyeColor = new Color(0.15f, 0.1f, 0.08f);
                noseColor = new Color(0.22f, 0.15f, 0.12f);
                mouthColor = new Color(0.58f, 0.25f, 0.2f);
                yOffset = -88f;
                earScale = 0.95f;
                break;
            case DogMoodState.Sick:
                baseColor = new Color(0.54f, 0.56f, 0.58f);
                muzzleColor = new Color(0.76f, 0.77f, 0.78f);
                innerEarColor = new Color(0.68f, 0.66f, 0.66f);
                eyeColor = new Color(0.2f, 0.2f, 0.2f);
                noseColor = new Color(0.2f, 0.2f, 0.2f);
                mouthColor = new Color(0.32f, 0.28f, 0.28f);
                yOffset = -102f;
                earScale = 0.86f;
                break;
        }

        dogImage.color = baseColor;
        if (dogMuzzle != null) dogMuzzle.color = muzzleColor;
        if (dogInnerEarLeft != null) dogInnerEarLeft.color = innerEarColor;
        if (dogInnerEarRight != null) dogInnerEarRight.color = innerEarColor;
        if (dogEyeLeft != null) dogEyeLeft.color = eyeColor;
        if (dogEyeRight != null) dogEyeRight.color = eyeColor;
        if (dogNose != null) dogNose.color = noseColor;
        if (dogMouth != null) dogMouth.color = mouthColor;

        if (dogRoot != null) dogRoot.anchoredPosition = new Vector2(0f, yOffset);
        if (dogEarLeft != null) dogEarLeft.rectTransform.localScale = Vector3.one * earScale;
        if (dogEarRight != null) dogEarRight.rectTransform.localScale = Vector3.one * earScale;
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

        CreateStatusBar(root.transform, font, "Hunger", "🍖", new Color(1f, 0.63f, 0.2f), out hungerBar, out hungerText, 1f, -170f, 51);
        CreateStatusBar(root.transform, font, "Mood", "♥", new Color(0.3f, 0.76f, 1f), out moodBar, out moodText, 1f, -300f, 75);
        CreateStatusBar(root.transform, font, "Energy", "⚡", new Color(0.45f, 0.88f, 0.38f), out energyBar, out energyText, 1f, -430f, 73);

        var questPanel = new GameObject("QuestPanel", typeof(RectTransform), typeof(Image));
        questPanel.transform.SetParent(root.transform, false);
        var qp = questPanel.GetComponent<Image>();
        qp.color = new Color(0.22f, 0.24f, 0.38f, 0.95f);
        Anchor(questPanel.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, new Vector2(0, -580), new Vector2(820, 140));

        questText = CreateText(questPanel.transform, font, "Quest", 36, TextAnchor.MiddleCenter, Color.white);
        Anchor(questText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 0), new Vector2(780, 110));

        var dogContainer = new GameObject("DogContainer", typeof(RectTransform));
        dogContainer.transform.SetParent(root.transform, false);
        dogRoot = dogContainer.GetComponent<RectTransform>();
        Anchor(dogRoot, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -80), new Vector2(520, 520));

        var earL = new GameObject("DogEarLeft", typeof(RectTransform), typeof(Image));
        earL.transform.SetParent(dogContainer.transform, false);
        dogEarLeft = earL.GetComponent<Image>();
        dogEarLeft.color = new Color(0.72f, 0.48f, 0.31f);
        Anchor(earL.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-120, 120), new Vector2(92, 150));

        var earR = new GameObject("DogEarRight", typeof(RectTransform), typeof(Image));
        earR.transform.SetParent(dogContainer.transform, false);
        dogEarRight = earR.GetComponent<Image>();
        dogEarRight.color = new Color(0.72f, 0.48f, 0.31f);
        Anchor(earR.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(120, 120), new Vector2(92, 150));

        var earInnerL = new GameObject("DogInnerEarLeft", typeof(RectTransform), typeof(Image));
        earInnerL.transform.SetParent(earL.transform, false);
        dogInnerEarLeft = earInnerL.GetComponent<Image>();
        dogInnerEarLeft.color = new Color(0.95f, 0.64f, 0.62f);
        Anchor(earInnerL.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -12), new Vector2(52, 96));

        var earInnerR = new GameObject("DogInnerEarRight", typeof(RectTransform), typeof(Image));
        earInnerR.transform.SetParent(earR.transform, false);
        dogInnerEarRight = earInnerR.GetComponent<Image>();
        dogInnerEarRight.color = new Color(0.95f, 0.64f, 0.62f);
        Anchor(earInnerR.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -12), new Vector2(52, 96));

        var dog = new GameObject("DogBody", typeof(RectTransform), typeof(Image));
        dog.transform.SetParent(dogContainer.transform, false);
        dogImage = dog.GetComponent<Image>();
        dogImage.color = new Color(0.85f, 0.75f, 0.6f);
        Anchor(dog.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(360, 360));

        var muzzle = new GameObject("DogMuzzle", typeof(RectTransform), typeof(Image));
        muzzle.transform.SetParent(dogContainer.transform, false);
        dogMuzzle = muzzle.GetComponent<Image>();
        dogMuzzle.color = new Color(0.95f, 0.89f, 0.8f);
        Anchor(muzzle.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -30), new Vector2(200, 130));

        var eyeL = new GameObject("DogEyeLeft", typeof(RectTransform), typeof(Image));
        eyeL.transform.SetParent(dogContainer.transform, false);
        dogEyeLeft = eyeL.GetComponent<Image>();
        dogEyeLeft.color = new Color(0.1f, 0.12f, 0.18f);
        Anchor(eyeL.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-62, 34), new Vector2(34, 34));

        var eyeR = new GameObject("DogEyeRight", typeof(RectTransform), typeof(Image));
        eyeR.transform.SetParent(dogContainer.transform, false);
        dogEyeRight = eyeR.GetComponent<Image>();
        dogEyeRight.color = new Color(0.1f, 0.12f, 0.18f);
        Anchor(eyeR.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(62, 34), new Vector2(34, 34));

        var nose = new GameObject("DogNose", typeof(RectTransform), typeof(Image));
        nose.transform.SetParent(dogContainer.transform, false);
        dogNose = nose.GetComponent<Image>();
        dogNose.color = new Color(0.18f, 0.14f, 0.17f);
        Anchor(nose.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -6), new Vector2(58, 36));

        var mouth = new GameObject("DogMouth", typeof(RectTransform), typeof(Image));
        mouth.transform.SetParent(dogContainer.transform, false);
        dogMouth = mouth.GetComponent<Image>();
        dogMouth.color = new Color(0.52f, 0.24f, 0.24f);
        Anchor(mouth.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -52), new Vector2(74, 18));

        dogStateText = CreateText(root.transform, font, "State: Idle", 34, TextAnchor.MiddleCenter, Color.white);
        Anchor(dogStateText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -330), new Vector2(860, 56));

        var saveBadge = new GameObject("SaveBadge", typeof(RectTransform), typeof(Image));
        saveBadge.transform.SetParent(root.transform, false);
        saveBadgeBg = saveBadge.GetComponent<Image>();
        saveBadgeBg.color = new Color(0.12f, 0.2f, 0.28f, 0.92f);
        Anchor(saveBadge.GetComponent<RectTransform>(), 0.8f, 0.28f, 0.8f, 0.28f, Vector2.zero, new Vector2(280, 86));

        saveText = CreateText(saveBadge.transform, font, "", 34, TextAnchor.MiddleCenter, new Color(0.9f, 1f, 0.95f));
        Stretch(saveText.rectTransform);
        saveBadgeBg.gameObject.SetActive(false);

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

    private static void CreateStatusBar(Transform parent, Font font, string label, string icon, Color fillColor, out Slider slider, out Text valueText, float anchorX, float y, int seedValue)
    {
        var bar = new GameObject($"{label}Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        var barBg = bar.GetComponent<Image>();
        barBg.color = new Color(0.13f, 0.16f, 0.3f, 0.95f);
        Anchor(bar.GetComponent<RectTransform>(), anchorX, 1f, anchorX, 1f, new Vector2(0, y), new Vector2(940, 110));

        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(bar.transform, false);
        var frameImg = frame.GetComponent<Image>();
        frameImg.color = new Color(1f, 1f, 1f, 0.18f);
        Anchor(frame.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, Vector2.zero, new Vector2(-8, -8));

        var fillRoot = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillRoot.transform.SetParent(bar.transform, false);
        var fillImg = fillRoot.GetComponent<Image>();
        fillImg.color = fillColor;
        Anchor(fillRoot.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, new Vector2(8, 0), new Vector2(620, -14));

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(bar.transform, false);
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 100; slider.interactable = false; slider.value = seedValue;
        Stretch(sliderGo.GetComponent<RectTransform>());
        slider.fillRect = fillRoot.GetComponent<RectTransform>();
        slider.targetGraphic = fillImg;

        var iconBadge = new GameObject("IconBadge", typeof(RectTransform), typeof(Image));
        iconBadge.transform.SetParent(bar.transform, false);
        iconBadge.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);
        Anchor(iconBadge.GetComponent<RectTransform>(), 0f, 0.5f, 0f, 0.5f, new Vector2(54, 0), new Vector2(64, 64));

        var iconText = CreateText(iconBadge.transform, font, icon, 38, TextAnchor.MiddleCenter, Color.white);
        Stretch(iconText.rectTransform);

        var labelText = CreateText(bar.transform, font, label, 46, TextAnchor.MiddleLeft, Color.white);
        Anchor(labelText.rectTransform, 0f, 0.5f, 0f, 0.5f, new Vector2(96, 0), new Vector2(420, 70));

        valueText = CreateText(bar.transform, font, "0%", 46, TextAnchor.MiddleRight, Color.white);
        Anchor(valueText.rectTransform, 1f, 0.5f, 1f, 0.5f, new Vector2(-24, 0), new Vector2(240, 70));
    }

    private static Text CreateText(Transform parent, Font font, string text, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.12f, 0.22f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
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

        var gloss = new GameObject("Gloss", typeof(RectTransform), typeof(Image));
        gloss.transform.SetParent(btnGo.transform, false);
        var glossImg = gloss.GetComponent<Image>();
        glossImg.color = new Color(1f, 1f, 1f, 0.2f);
        Anchor(gloss.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, new Vector2(0, -32), new Vector2(180, 60));

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
