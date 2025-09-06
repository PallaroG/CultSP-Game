using UnityEngine;
using UnityEngine.SceneManagement;
public class Pausemenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    public void Pause()
    {
        pauseMenu.SetActive(true);
    }

    public void Titlescreen()
    {
        SceneManager.LoadScene("title screen");
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
    }
}
