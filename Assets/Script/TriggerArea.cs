using UnityEngine;
using System.Collections;

public class MinigameTrigger : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameManager minigame; 
    [SerializeField] private string playerTag = "Player"; 
    [SerializeField] private GameObject sequencialObject;

    [Header("Configurações")]
    [SerializeField] private float respawnDelay = 5f; // tempo em segundos até a área reaparecer
    [SerializeField] private GameObject triggerArea;  // objeto visual ou colisor da área

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player entrou na área do minigame!");
            sequencialObject.SetActive(true);
            minigame.StartNewGame();

            // aqui "escutamos" quando o minigame conclui
            StartCoroutine(HandleRespawn());
        }
    }

    private IEnumerator HandleRespawn()
    {
        // desativa a área enquanto o minigame rola
        if (triggerArea != null) triggerArea.SetActive(false);

        // espera até o minigame acabar
        while (minigame.enabled && minigame.gameObject.activeSelf)
        {
            yield return null;
        }

        // espera o tempo configurado
        yield return new WaitForSeconds(respawnDelay);

        // reativa a área
        if (triggerArea != null) triggerArea.SetActive(true);
    }
}
