using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text displayText;

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

    [Header("Mapeamento por jogador")]
    [SerializeField] private KeyCode[] player1Keys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
    [SerializeField] private KeyCode[] player2Keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    [Header("Controle de rodadas")]
    [SerializeField] private int maxRounds = 2;  // Quantas rodadas de sucesso
    private int currentRound = 0;

    private readonly List<KeyCode> sequence = new List<KeyCode>();
    private int currentIndex = 0;
    private int currentPlayer = 1;
    private bool inputEnabled = false;
    public bool minigameStart = false;
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
            StartCoroutine(StartNewTurn(regenerate:true));
        }
    }

    public void StartNewGame()
    {
        StartCoroutine(StartNewTurn(regenerate:true));
    }

    IEnumerator StartNewTurn(bool regenerate)
    {
        // Se já completou maxRounds, encerra o minigame
        if (currentRound >= maxRounds)
        {
            displayText.text = "Minigame Concluído!";
            enabled = false;
            yield break;
        }

        inputEnabled = false;
        displayText.text = $"Prepare P{currentPlayer}...";
        yield return new WaitForSeconds(preTurnDelay);

        if (regenerate || sequence.Count == 0)
            GenerateSequence(sequenceLength);

        currentIndex = 0;
        UpdateDisplay();
        inputEnabled = true;
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
        KeyCode[] activeMap = (currentPlayer == 1) ? player1Keys : player2Keys;
        KeyCode requiredKey = activeMap[expectedIdx];

        KeyCode pressed = GetPressedAmong(activeMap);
        if (pressed == KeyCode.None) return;

        if (pressed == requiredKey)
        {
            currentIndex++;
            if (currentIndex >= sequence.Count)
            {
                inputEnabled = false;

                // Próximo jogador
                currentPlayer = (currentPlayer == 1) ? 2 : 1;

                // Rodada completa (após ambos jogarem)
                if (currentPlayer == 1)
                    currentRound++;

                if (growEachTurn) sequenceLength++;

                StartCoroutine(StartNewTurn(regenerate:true));
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
        displayText.text = $"P{currentPlayer}: MISS!";
        yield return new WaitForSeconds(1f);
        StartCoroutine(StartNewTurn(regenerate:!regenerateOnMiss));
    }

    void UpdateDisplay()
    {
        List<string> remaining = new List<string>(sequence.Count - currentIndex);
        for (int i = currentIndex; i < sequence.Count; i++)
        {
            int idx = System.Array.IndexOf(possibleKeys, sequence[i]);
            remaining.Add(symbols[idx]);
        }
        displayText.text = $"P{currentPlayer} ▶ {string.Join(" ", remaining)}";
    }

    KeyCode GetPressedAmong(KeyCode[] allowed)
    {
        foreach (KeyCode k in allowed)
            if (Input.GetKeyDown(k)) return k;
        return KeyCode.None;
    }
}
