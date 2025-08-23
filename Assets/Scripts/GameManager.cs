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
    [SerializeField] private float preTurnDelay = 0.7f;        // tempo antes de iniciar o turno
    [SerializeField] private bool growEachTurn = true;         // aumenta a dificuldade ao terminar um turno
    [SerializeField] private bool regenerateOnMiss = false;    // se true, errou -> gera nova sequência; se false, repete a mesma

    [Header("Teclas possíveis (ordem importa)")]
    [SerializeField] private KeyCode[] possibleKeys = {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
    };

    // Símbolos para exibir na UI, no MESMO índice de possibleKeys
    [SerializeField] private string[] symbols = { "↑", "↓", "←", "→" };

    [Header("Mapeamento por jogador (mesmo tamanho de possibleKeys)")]
    [SerializeField] private KeyCode[] player1Keys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
    [SerializeField] private KeyCode[] player2Keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    private readonly List<KeyCode> sequence = new List<KeyCode>();
    private int currentIndex = 0;
    private int currentPlayer = 1; // 1 ou 2
    private bool inputEnabled = false;

    void Start()
    {
        if (displayText == null)
        {
            Debug.LogError("DisplayText não atribuído no Inspector.");
            enabled = false;
            return;
        }

        if (possibleKeys.Length == 0 || symbols.Length != possibleKeys.Length ||
            player1Keys.Length != possibleKeys.Length || player2Keys.Length != possibleKeys.Length)
        {
            Debug.LogError("Arrays de teclas/símbolos com tamanhos incompatíveis.");
            enabled = false;
            return;
        }

        StartCoroutine(StartNewTurn(regenerate:true)); // primeiro turno sempre gera
    }

    IEnumerator StartNewTurn(bool regenerate)
    {
        inputEnabled = false;
        displayText.text = $"Prepare P{currentPlayer}...";
        yield return new WaitForSeconds(preTurnDelay);

        if (regenerate)
            GenerateSequence(sequenceLength);

        currentIndex = 0;
        UpdateDisplay();          // mostra a sequência inteira
        inputEnabled = true;      // libera o input
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

        // Descobre qual tecla o jogador atual PRECISA apertar agora
        int expectedIdx = System.Array.IndexOf(possibleKeys, sequence[currentIndex]);
        KeyCode[] activeMap = (currentPlayer == 1) ? player1Keys : player2Keys;
        KeyCode requiredKey = activeMap[expectedIdx];

        // Captura uma tecla válida que foi pressionada (entre as mapeadas do jogador)
        KeyCode pressed = GetPressedAmong(activeMap);

        if (pressed == KeyCode.None)
            return; // ignorar teclas fora do conjunto permitido

        if (pressed == requiredKey)
        {
            // Acertou -> avança e atualiza a UI removendo a primeira tecla restante
            currentIndex++;
            if (currentIndex >= sequence.Count)
            {
                // Terminou o turno desse jogador
                inputEnabled = false;

                // Próximo jogador
                currentPlayer = (currentPlayer == 1) ? 2 : 1;

                if (growEachTurn) sequenceLength++;

                // Novo turno (gera nova sequência só se for o começo do turno ou se errou e optou por regenerar)
                StartCoroutine(StartNewTurn(regenerate:true));
            }
            else
            {
                UpdateDisplay(); // mostra apenas as teclas restantes
            }
        }
        else
        {
            // Errou -> pausa input e dá feedback
            inputEnabled = false;
            StartCoroutine(HandleMiss());
        }
    }

    IEnumerator HandleMiss()
    {
        displayText.text = $"P{currentPlayer}: MISS!";
        yield return new WaitForSeconds(1f);

        if (regenerateOnMiss)
        {
            // Gera nova sequência para o mesmo jogador
            StartCoroutine(StartNewTurn(regenerate:true));
        }
        else
        {
            // Repete a MESMA sequência para o mesmo jogador
            StartCoroutine(StartNewTurn(regenerate:false));
        }
    }

    void UpdateDisplay()
    {
        // Constrói uma string apenas com as teclas RESTANTES (do currentIndex até o fim)
        List<string> remaining = new List<string>(sequence.Count - currentIndex);
        for (int i = currentIndex; i < sequence.Count; i++)
        {
            int idx = System.Array.IndexOf(possibleKeys, sequence[i]);
            remaining.Add(symbols[idx]);
        }

        displayText.text = $"P{currentPlayer} ▶ {string.Join(" ", remaining)}";
    }

    // Retorna a primeira tecla pressionada que pertence ao conjunto permitido; caso contrário KeyCode.None
    KeyCode GetPressedAmong(KeyCode[] allowed)
    {
        for (int i = 0; i < allowed.Length; i++)
        {
            if (Input.GetKeyDown(allowed[i]))
                return allowed[i];
        }
        return KeyCode.None;
    }
}
