using UnityEngine;

[CreateAssetMenu(menuName = "Bulldog/Phase1Config", fileName = "Phase1Config")]
public class Phase1Config : ScriptableObject
{
    public float driftIntervalSeconds = 24f;
    public int driftHungerDelta = 4;
    public int driftMoodDelta = -3;
    public int driftEnergyDelta = -3;

    public int criticalThreshold = 15;
    public int warningThreshold = 42;

    public float uiLerpSpeed = 7f;
    public float cooldownSeconds = 1.5f;
    public float questTargetSeconds = 24f;
    public int questMoodThreshold = 55;

    public static Phase1Config CreateRuntimeDefaults()
    {
        var cfg = CreateInstance<Phase1Config>();
        return cfg;
    }
}
