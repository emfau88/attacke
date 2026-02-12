using UnityEngine;

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
}
