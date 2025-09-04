using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton

    [Header("UI")]
    [SerializeField] private TMP_Text displayText;

    [Header("Player")]
    [Tooltip("Script de movimento do player (registrado automaticamente no spawn).")]
    private Behaviour playerMovement;

    [Header("Sequência")]
    [SerializeField] private int sequenceLength = 4;
    [SerializeField] private float preTurnDelay = 0.7f;
    [SerializeField] private bool growEachTurn = true;
    [SerializeField] private bool regenerateOnMiss = false;

    [Header("Teclas possíveis (ordem importa)")]
    [SerializeField] private KeyCode[] possibleKeys = {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
    };

    [SerializeField] private string[] symbols = { "↑", "↓", "←", "→" };

    [Header("Mapeamento do jogador")]
    [SerializeField] private KeyCode[] playerKeys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };

    [Header("Controle de rodadas")]
    [SerializeField] private int maxRounds = 2;  // Quantas rodadas de sucesso
    private int currentRound = 0;

    private readonly List<KeyCode> sequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool inputEnabled = false;
    public bool minigameStart = false;

    void Awake()
    {
        // garante singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (displayText == null)
        {
            Debug.LogError("DisplayText não atribuído no Inspector.");
            enabled = false;
            return;
        }

        if (minigameStart)
        {
            StartCoroutine(StartNewTurn(regenerate: true));
        }
    }

    // Chamado pelo player quando spawnar
    public void RegisterPlayerMovement(Behaviour movement)
    {
        playerMovement = movement;
    }

    public void StartNewGame()
    {
        currentRound = 0;
        StartCoroutine(StartNewTurn(regenerate: true));
    }

    IEnumerator StartNewTurn(bool regenerate)
    {
        if (currentRound >= maxRounds)
        {
            StartCoroutine(ShowCompletionAndDisable());
            yield break;
        }

        // trava o movimento do jogador ao iniciar o minigame/turno
        if (playerMovement != null) playerMovement.enabled = false;

        inputEnabled = false;
        displayText.text = $"Prepare...";
        yield return new WaitForSeconds(preTurnDelay);

        if (regenerate || sequence.Count == 0)
            GenerateSequence(sequenceLength);

        currentIndex = 0;
        UpdateDisplay();
        inputEnabled = true;
    }

    IEnumerator ShowCompletionAndDisable()
    {
        displayText.text = "Minigame Concluído!";
        yield return new WaitForSeconds(1f);
        displayText.text = "";

        // restaura o movimento do jogador ao finalizar o minigame
        if (playerMovement != null) playerMovement.enabled = true;

        enabled = false;
    }

    void GenerateSequence(int length)
    {
        sequence.Clear();
        for (int i = 0; i < length; i++)
        {
            int idx = Random.Range(0, possibleKeys.Length);
            sequence.Add(possibleKeys[idx]);
        }
    }

    void Update()
    {
        if (!inputEnabled || sequence.Count == 0) return;

        int expectedIdx = System.Array.IndexOf(possibleKeys, sequence[currentIndex]);
        if (expectedIdx < 0)
        {
            Debug.LogError("Sequência contém uma tecla que não existe em possibleKeys.");
            return;
        }

        if (playerKeys == null || playerKeys.Length != possibleKeys.Length)
        {
            Debug.LogError("playerKeys deve ter o mesmo tamanho e ordem que possibleKeys.");
            return;
        }

        KeyCode requiredKey = playerKeys[expectedIdx];
        KeyCode pressed = GetPressedAmong(playerKeys);
        if (pressed == KeyCode.None) return;

        if (pressed == requiredKey)
        {
            currentIndex++;
            if (currentIndex >= sequence.Count)
            {
                inputEnabled = false;
                currentRound++;

                if (growEachTurn) sequenceLength++;

                StartCoroutine(StartNewTurn(regenerate: true));
            }
            else
            {
                UpdateDisplay();
            }
        }
        else
        {
            inputEnabled = false;
            StartCoroutine(HandleMiss());
        }
    }

    IEnumerator HandleMiss()
    {
        displayText.text = $"MISS!";
        yield return new WaitForSeconds(1f);
        StartCoroutine(StartNewTurn(regenerate: regenerateOnMiss));
    }

    void UpdateDisplay()
    {
        List<string> remaining = new List<string>(sequence.Count - currentIndex);
        for (int i = currentIndex; i < sequence.Count; i++)
        {
            int idx = System.Array.IndexOf(possibleKeys, sequence[i]);
            remaining.Add(symbols[idx]);
        }
        displayText.text = $"Você: {string.Join(" ", remaining)}";
    }

    KeyCode GetPressedAmong(KeyCode[] allowed)
    {
        foreach (KeyCode k in allowed)
            if (Input.GetKeyDown(k)) return k;
        return KeyCode.None;
    }
}
