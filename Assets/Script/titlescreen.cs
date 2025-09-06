using UnityEngine;
using UnityEngine.SceneManagement;

public class titlescreen : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGmae()
    {
        Application.Quit();
    }
}
