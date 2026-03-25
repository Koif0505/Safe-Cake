using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject howToPlayPanel;

    void Start()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    public void GoToLevelSelect()
    {
        SceneManager.LoadScene("2. Level Select");
    }

    public void OpenHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}