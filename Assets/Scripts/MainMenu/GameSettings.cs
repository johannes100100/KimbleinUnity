using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    // Number of players chosen in the main menu
    [HideInInspector] public int playerCount = 4;

    void Awake()
    {
        // Create singleton instance and keep it across scene loads
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);  // Prevent duplicates
        }
    }
}
