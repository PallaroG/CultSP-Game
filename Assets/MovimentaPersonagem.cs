using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    void Start()
    {
        if (!IsLocalPlayer)
        {
            enabled = false;  // Desativa o movimento para jogadores não locais
            return;
        }

        controller = GetComponent<CharacterController>();

         if (IsOwner) // garante só no local
        {
            GameManager.Instance.RegisterPlayerMovement(GetComponent<PlayerMovement>());
        }

    }

    void Update()
    {
        if (!IsLocalPlayer) return;  // Só o jogador local pode controlar o movimento

        // Verifica se está no chão
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Movimento no plano XZ
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Pulo
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Gravidade
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Atualiza a posição no servidor apenas quando necessário
        if (IsOwner)
        {
            // Sincroniza a posição apenas quando o jogador se move (não toda vez que ele está parado)
            if (move != Vector3.zero || isGrounded)
            {
                MovePlayerServerRpc(transform.position); // Envia a posição para o servidor
            }
        }
    }

    // Comando para enviar o movimento do jogador ao servidor
    [ServerRpc]
    void MovePlayerServerRpc(Vector3 position)
    {
        // Sincroniza a posição para todos os clientes
        MovePlayerClientRpc(position);
    }

    // ClientRpc para mover o jogador nos outros clientes
    [ClientRpc]
    void MovePlayerClientRpc(Vector3 position)
    {
        if (!IsLocalPlayer)
        {
            // Atualiza a posição nos clientes que não são o jogador local
            transform.position = position;
        }
    }
}
