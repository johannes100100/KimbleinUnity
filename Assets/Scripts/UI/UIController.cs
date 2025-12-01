using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Header("Dice Reference")]
    public Dice dice;

    [Header("Pause Menu UI")]
    public GameObject pauseMenuUI;

    private bool isPaused = false;

    void Update()
    {
        // ESC opens and closes pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    // --- DICE ROLL BUTTON ---
    public void OnRollDice()
    {
        GameManager.Instance.PlayerRoll();
    }

    // --- PAUSE MENU FUNCTIONS ---
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
