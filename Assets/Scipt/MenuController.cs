using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void SelectSafeMode()
    {
        GameSession.selectedMode = "Safe";
        SceneManager.LoadScene("SafeMode");
    }

    public void SelectChallengeMode()
    {
        GameSession.selectedMode = "Challenge";
        SceneManager.LoadScene("ChallengeMode");
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("1. MainMenu");
    }
}