using UnityEngine;

public static class SaveSystem
{
    private const string HungerKey = "pet_hunger";
    private const string MoodKey = "pet_mood";
    private const string EnergyKey = "pet_energy";
    private const string HasSaveKey = "pet_has_save";

    public static PetState Load()
    {
        if (PlayerPrefs.GetInt(HasSaveKey, 0) == 0)
            return new PetState(50, 50, 50);

        var state = new PetState(
            PlayerPrefs.GetInt(HungerKey, 50),
            PlayerPrefs.GetInt(MoodKey, 50),
            PlayerPrefs.GetInt(EnergyKey, 50)
        );
        state.ClampAll();
        return state;
    }

    public static void Save(PetState state)
    {
        state.ClampAll();
        PlayerPrefs.SetInt(HungerKey, state.Hunger);
        PlayerPrefs.SetInt(MoodKey, state.Mood);
        PlayerPrefs.SetInt(EnergyKey, state.Energy);
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.Save();
    }
}
