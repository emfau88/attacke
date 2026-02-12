using UnityEngine;

[CreateAssetMenu(menuName = "Bulldog/Phase1Config", fileName = "Phase1Config")]
public class Phase1Config : ScriptableObject
{
    public float driftIntervalSeconds = 28f;
    public int driftHungerDelta = 4;
    public int driftMoodDelta = -2;
    public int driftEnergyDelta = -2;

    public int criticalThreshold = 19;
    public int warningThreshold = 45;

    public float uiLerpSpeed = 6.5f;
    public float cooldownSeconds = 1.8f;
    public float questTargetSeconds = 28f;
    public int questMoodThreshold = 58;

    public static Phase1Config CreateRuntimeDefaults()
    {
        var cfg = CreateInstance<Phase1Config>();
        return cfg;
    }
}
