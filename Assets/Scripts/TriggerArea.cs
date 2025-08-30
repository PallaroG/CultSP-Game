using UnityEngine;

public class MinigameTrigger : MonoBehaviour
{
    [SerializeField] private GameManager minigame; // arraste o GameManager com o script do minigame
    [SerializeField] private string playerTag = "Player"; // certifique-se que o personagem tem essa Tag
    [SerializeField] private GameObject sequencialObject;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player entrou na área do minigame!");
            sequencialObject.SetActive(true);
            minigame.StartNewGame();
        }
    }
}