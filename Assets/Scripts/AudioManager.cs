using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayUiClick() { }
    public void PlayActionFeed() { }
    public void PlayActionPlay() { }
    public void PlayActionSleep() { }
    public void PlayWarning() { }
}
