using System;
using UnityEngine;

public static class SaveSystem
{
    private const string SaveKey = "bulldog_save_v1";

    [Serializable]
    private class SaveEnvelope
    {
        public int version = 1;
        public long timestampUnix;
        public PetState pet;
    }

    public static PetState Load()
    {
        var json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return DefaultState();

        try
        {
            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            if (envelope == null || envelope.pet == null || envelope.version != 1)
                return DefaultState();

            envelope.pet.SaveVersion = envelope.version;
            envelope.pet.LastSavedUnix = envelope.timestampUnix;
            envelope.pet.ClampAll();
            return envelope.pet;
        }
        catch
        {
            return DefaultState();
        }
    }

    public static void Save(PetState state)
    {
        state.ClampAll();
        state.SaveVersion = 1;
        state.LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var envelope = new SaveEnvelope
        {
            version = 1,
            timestampUnix = state.LastSavedUnix,
            pet = state
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(envelope));
        PlayerPrefs.Save();
    }

    public static void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    public static PetState DefaultState() => new PetState(50, 50, 50);
}
