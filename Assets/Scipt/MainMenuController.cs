using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject howToPlayPanel;

    void Start()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void StartSafeMode()
    {
        GameSession.selectedMode = "Safe";
        GameSession.selectedLevel = 1;
        SceneManager.LoadScene("SafeMode");
    }

    public void StartChallengeMode()
    {
        GameSession.selectedMode = "Challenge";
        GameSession.selectedLevel = 1;
        SceneManager.LoadScene("ChallengeMode");
    }

    public void OpenHowToPlay()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}