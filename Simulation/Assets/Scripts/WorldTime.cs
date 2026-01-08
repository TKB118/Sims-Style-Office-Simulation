using UnityEngine;

public class WorldTime : MonoBehaviour
{
    public static WorldTime Instance { get; private set; }

    public float GlobalTime { get; private set; } // Holds the global time in seconds
    public float TimeScale { get; set; } = 1f; // Can be used to adjust the time speed

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensures persistence across scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    private void Update()
    {
        // Increment global time based on Time.deltaTime and TimeScale
        GlobalTime += Time.deltaTime * TimeScale;
    }

    public void ResetTime()
    {
        GlobalTime = 0;
    }
}
