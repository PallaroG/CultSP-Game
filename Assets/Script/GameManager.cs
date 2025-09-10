using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text displayText;

    [Header("Player")]
    private Behaviour playerMovement;

    [Header("Sequência (mini-game)")]
    [SerializeField] private int sequenceLength = 4;
    [SerializeField] private float preTurnDelay = 0.7f;
    [SerializeField] private bool growEachTurn = true;
    [SerializeField] private bool regenerateOnMiss = false;

    [Header("Teclas possíveis (ordem importa)")]
    [SerializeField] private KeyCode[] possibleKeys = {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
    };
    [SerializeField] private string[] symbols = { "↑", "↓", "←", "→" };
    [SerializeField] private KeyCode[] playerKeys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };

    [Header("Controle do mini-game")]
    [SerializeField] private int maxRounds = 2;
    private int currentRound = 0;
    private readonly List<KeyCode> sequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool inputEnabled = false;
    public bool minigameStart = false;

    // === Triggers embutidos ===
    [Header("Triggers disponíveis (arraste os GameObjects)")]
    [SerializeField] private GameObject[] triggers; // arraste até 4 triggers no inspector

    [Header("Configuração de fases")]
    [SerializeField] private int repeatsPerPhase = 3;
    [SerializeField] private float baseDuration = 5f;
    [SerializeField] private float extraDurationPerPhase = 1f;
    [SerializeField] private float respawnDelay = 2f; // tempo entre desaparecer e reaparecer na mesma fase

    [Tooltip("Delay antes de iniciar a próxima fase (segundos)")]
    [SerializeField] private float phaseTransitionDelay = 2f;

    private int currentRepeats = 0;         // quantas repetições já foram concluídas na fase atual
    private int currentActiveCount = 0;     // quantos triggers estão ativos nesta fase
    private bool respawnScheduled = false;
    private GameObject currentInitiatingTrigger = null;

    // guarda índices ativados na fase atual (para debug/controle opcional)
    private List<int> lastActivatedIndices = new List<int>();

    void Awake()
    {
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

        HideAllTriggers();
        // inicializa com 1 ativo
        currentActiveCount = 1;
        currentRepeats = 0; // reset inicial
        StartPhase(currentActiveCount);
    }

    public void RegisterPlayerMovement(Behaviour movement) => playerMovement = movement;

    // overload usado pelo TriggerArea para passar o trigger que iniciou
    public void StartNewGame(GameObject initiatingTrigger)
    {
        currentInitiatingTrigger = initiatingTrigger;
        StartNewGame();
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
            StartCoroutine(ShowCompletionAndNotify());
            yield break;
        }

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

    IEnumerator ShowCompletionAndNotify()
    {
        displayText.text = "Minigame Concluído!";
        yield return new WaitForSeconds(1f);
        displayText.text = "";

        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log("[GameManager] Minigame concluído -> HandleTriggerCompletion");
        HandleTriggerCompletion(currentInitiatingTrigger);
        currentInitiatingTrigger = null;
    }

    void GenerateSequence(int length)
    {
        sequence.Clear();
        for (int i = 0; i < length; i++)
        {
            int idx = UnityEngine.Random.Range(0, possibleKeys.Length);
            sequence.Add(possibleKeys[idx]);
        }
    }

    void Update()
    {
        if (!inputEnabled || sequence.Count == 0) return;

        int expectedIdx = System.Array.IndexOf(possibleKeys, sequence[currentIndex]);
        if (expectedIdx < 0) return;

        if (playerKeys == null || playerKeys.Length != possibleKeys.Length) return;

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

    // === Controle de fases embutido com aleatorização ===
    private void HideAllTriggers()
    {
        foreach (var t in triggers) if (t != null) t.SetActive(false);
    }

    private void StartPhase(int amount)
    {
        Debug.Log($"[GameManager] StartPhase -> amount={amount}");
        StopRespawnSchedule();
        HideAllTriggers();
        // NÃO resetar currentRepeats aqui, pois isso zera o progresso entre respawns
        currentActiveCount = Mathf.Clamp(amount, 1, triggers.Length);

        // escolhe índices únicos aleatórios
        lastActivatedIndices.Clear();
        List<int> available = new List<int>();
        for (int i = 0; i < triggers.Length; i++) available.Add(i);

        for (int n = 0; n < currentActiveCount && available.Count > 0; n++)
        {
            int pickIndex = UnityEngine.Random.Range(0, available.Count);
            int triggerIdx = available[pickIndex];
            available.RemoveAt(pickIndex);
            lastActivatedIndices.Add(triggerIdx);
        }

        // define qual dos ativados receberá a extraDuration (escolha aleatória entre ativados)
        int extraAssignedIdx = -1;
        if (lastActivatedIndices.Count > 0)
        {
            int pick = UnityEngine.Random.Range(0, lastActivatedIndices.Count);
            extraAssignedIdx = lastActivatedIndices[pick];
        }

        // ativa os triggers escolhidos
        foreach (int idx in lastActivatedIndices)
        {
            var trigger = triggers[idx];
            if (trigger == null) continue;

            var ta = trigger.GetComponent<TriggerArea>();
            if (ta != null) ta.SetInUse(false);

            trigger.SetActive(true);

            float lifetime = baseDuration;
            if (idx == extraAssignedIdx) lifetime += extraDurationPerPhase;

            StartCoroutine(DisableAfter(trigger, lifetime));
            Debug.Log($"[GameManager] ativado trigger idx={idx} name={trigger.name} lifetime={lifetime}");
        }
    }

    private IEnumerator DisableAfter(GameObject trigger, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (trigger == null) yield break;

        var ta = trigger.GetComponent<TriggerArea>();
        if (ta != null && ta.IsInUse)
        {
            Debug.Log($"[GameManager] DisableAfter: trigger '{trigger.name}' está em uso -> não desativar");
            yield break;
        }

        trigger.SetActive(false);
        Debug.Log($"[GameManager] trigger '{trigger.name}' desativado por timeout");

        if (!respawnScheduled && CountActiveTriggers() == 0 && currentRepeats < repeatsPerPhase)
        {
            Debug.Log("[GameManager] nenhum trigger ativo -> agendando respawn da mesma fase");
            StartCoroutine(RespawnSamePhase(respawnDelay));
        }
    }

    private int CountActiveTriggers()
    {
        int count = 0;
        foreach (var t in triggers)
            if (t != null && t.activeInHierarchy) count++;
        return count;
    }

    // chamado quando um minigame é concluído para o trigger que iniciou
    private void HandleTriggerCompletion(GameObject trigger)
    {
        // libera e desativa o trigger que iniciou (se existir)
        if (trigger != null)
        {
            var ta = trigger.GetComponent<TriggerArea>();
            if (ta != null) ta.SetInUse(false);

            trigger.SetActive(false);
            Debug.Log($"[GameManager] trigger '{trigger.name}' desativado por conclusão do jogador");
        }

        currentRepeats++;
        Debug.Log($"[GameManager] HandleTriggerCompletion called. currentRepeats={currentRepeats}");

        // se completou suficientes repetições, aumenta quantidade ativa
        if (currentRepeats >= repeatsPerPhase)
        {
            // incrementa quantos triggers ficarão ativos na próxima fase
            currentActiveCount = Mathf.Min(currentActiveCount + 1, triggers.Length);
            Debug.Log($"[GameManager] fase completa -> next active count = {currentActiveCount}");

            // reseta o contador para a nova fase
            currentRepeats = 0;

            // inicia próxima fase após delay configurável
            StartCoroutine(AdvancePhaseAfterDelay(currentActiveCount));
            return;
        }

        // ainda na mesma fase: agenda respawn da mesma quantidade
        if (!respawnScheduled)
        {
            Debug.Log("[GameManager] agendando respawn (conclusão) da mesma fase");
            StartCoroutine(RespawnSamePhase(respawnDelay));
        }
    }

    private IEnumerator AdvancePhaseAfterDelay(int amount)
    {
        Debug.Log($"[GameManager] aguardando {phaseTransitionDelay}s antes de iniciar próxima fase");
        yield return new WaitForSeconds(phaseTransitionDelay);
        StartPhase(amount);
    }

    private IEnumerator RespawnSamePhase(float delay)
    {
        respawnScheduled = true;
        Debug.Log($"[GameManager] RespawnSamePhase começando. delay={delay}");
        yield return new WaitForSeconds(delay);
        respawnScheduled = false;

        Debug.Log($"[GameManager] RespawnSamePhase reativando {currentActiveCount} triggers");
        StartPhase(currentActiveCount);
    }

    private void StopRespawnSchedule() => respawnScheduled = false;
}
