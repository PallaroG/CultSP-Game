using UnityEngine;
using UnityEngine.SceneManagement;
public class Pausemenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    private bool isPaused = false;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    
    public void Pause()
    {
        pauseMenu.SetActive(true);
        isPaused = true;
    }

    public void Titlescreen()
    {
        SceneManager.LoadScene("title screen");
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        isPaused = false;
    }
}
