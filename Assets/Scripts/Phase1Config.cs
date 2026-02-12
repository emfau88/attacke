using UnityEngine;

[CreateAssetMenu(menuName = "Bulldog/Phase1Config", fileName = "Phase1Config")]
public class Phase1Config : ScriptableObject
{
    public float driftIntervalSeconds = 28f;
    public int driftHungerDelta = 3;
    public int driftMoodDelta = -2;
    public int driftEnergyDelta = -2;

    public int criticalThreshold = 20;
    public int warningThreshold = 40;

    public float uiLerpSpeed = 6f;
    public float cooldownSeconds = 2f;
    public float questTargetSeconds = 30f;
    public int questMoodThreshold = 60;

    public static Phase1Config CreateRuntimeDefaults()
    {
        var cfg = CreateInstance<Phase1Config>();
        return cfg;
    }
}
