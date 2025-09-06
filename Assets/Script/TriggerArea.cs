using UnityEngine;
using Unity.Netcode;

public class TriggerArea : NetworkBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameManager minigame;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject sequencialObject; // optional visual

    // indica que um jogador local iniciou o minigame neste trigger
    public bool IsInUse { get; private set; } = false;

    private void OnTriggerEnter(Collider other)
    {
        var pm = other.GetComponent<PlayerMovement>();
        if (other.CompareTag(playerTag) && pm != null && pm.IsOwner && !IsInUse)
        {
            Debug.Log($"[TriggerArea] Player owner entrou no trigger '{gameObject.name}'");

            IsInUse = true;

            if (sequencialObject != null) sequencialObject.SetActive(true);

            if (minigame != null)
            {
                // chama overload que recebe o trigger que iniciou
                minigame.StartNewGame(this.gameObject);
            }
            else
            {
                Debug.LogWarning("[TriggerArea] GameManager não atribuído no inspector.");
            }
        }
    }

    // usado pelo GameManager para marcar como não em uso quando o minigame termina
    public void SetInUse(bool value)
    {
        IsInUse = value;
        if (!value && sequencialObject != null) sequencialObject.SetActive(false);
    }
}
