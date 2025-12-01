using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private int selectedPlayerCount = 0;

    public GameObject playerCountPanel;
    public GameObject startGameButton;

    void Start()
    {
        // Hide player selection UI
        playerCountPanel.SetActive(false);
        startGameButton.SetActive(false);
    }

    public void OnPlayPressed()
    {
        // Show player amount options
        playerCountPanel.SetActive(true);
    }

    public void SelectPlayers(int count)
    {
        // Save chosen player amount
        selectedPlayerCount = count;

        // Enable Start Game button
        startGameButton.SetActive(true);
    }

    public void StartGame()
    {
        // Ensure valid player count
        if (selectedPlayerCount < 2)
            return;

        // Pass setting to game
        GameSettings.Instance.playerCount = selectedPlayerCount;

        // Load game scene
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
