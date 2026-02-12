using UnityEngine;

public enum DogMoodState
{
    Happy,
    Idle,
    Tired,
    Hungry,
    Sick
}

[System.Serializable]
public class PetState
{
    public int Hunger = 50;
    public int Mood = 50;
    public int Energy = 50;
    public long LastSavedUnix = 0;
    public int SaveVersion = 1;

    public PetState() { }

    public PetState(int hunger, int mood, int energy)
    {
        Hunger = hunger;
        Mood = mood;
        Energy = energy;
        ClampAll();
    }

    public void ClampAll()
    {
        Hunger = Mathf.Clamp(Hunger, 0, 100);
        Mood = Mathf.Clamp(Mood, 0, 100);
        Energy = Mathf.Clamp(Energy, 0, 100);
    }

    public bool IsCritical(int threshold) => Hunger < threshold || Mood < threshold || Energy < threshold;
    public bool IsWarning(int threshold) => Hunger < threshold || Mood < threshold || Energy < threshold;

    public DogMoodState EvaluateState(int warning)
    {
        if (Hunger <= 0 || Mood <= 0 || Energy <= 0) return DogMoodState.Sick;
        if (Hunger < warning) return DogMoodState.Hungry;
        if (Energy < warning) return DogMoodState.Tired;
        if (Mood > 70) return DogMoodState.Happy;
        return DogMoodState.Idle;
    }
}
