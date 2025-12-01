using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Players")]
    [Tooltip("How many players are in the game (2–4)")]
    public int playerCount = 4;
    public Player[] players;

    [Header("Dice")]
    public Dice dice;

    [Header("UI")]
    public TMP_Text turnText;
    public TMP_Text rollListText;
    public TMP_Text lastRollText;

    private bool hideLastRollAutomatically = true;

    [Header("Win UI")]
    public GameObject winnerPanel;
    public TMP_Text winnerText;

    // Stores starting roll results
    private Dictionary<int, int> rollResults = new Dictionary<int, int>();

    private bool waitingForPlayerRoll = false;
    private bool startingPhase = true;

    private int currentRoller = 0;
    private int currentTurnIndex = 0;
    private int lastDiceResult = 0;

    private Coroutine lastRollRoutine;

    private List<Piece> movablePieces = new List<Piece>();

    // Turn state flags
    private bool pieceIsMoving = false;
    private bool diceJustRolled = false;
    [HideInInspector] public bool diceAnimating = false;

    [HideInInspector] public bool gameStarted = false;

    [Header("Board Rotation")]
    public Transform boardRoot;
    public float rotateSpeed = 200f;

    private bool waitingToSkipMessage = false;
    [HideInInspector] public bool boardRotating = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Load player count from menu
        if (GameSettings.Instance != null)
            playerCount = GameSettings.Instance.playerCount;

        StartCoroutine(DetermineStartingPlayer());
    }

    void Update()
    {
        // Allow skipping "no movable pieces" message by clicking
        if (waitingToSkipMessage && Input.GetMouseButtonDown(0))
        {
            StopAllCoroutines();

            if (lastRollText != null)
                lastRollText.gameObject.SetActive(false);

            waitingToSkipMessage = false;
            EndTurn();
        }
    }

    // -------------------------
    // STARTING PHASE
    // -------------------------

    public IEnumerator DetermineStartingPlayer()
    {
        rollResults.Clear();

        for (int p = 0; p < playerCount; p++)
        {
            currentRoller = p;

            HighlightStartingPlayer(currentRoller);
            UpdateTurnText(players[p].playerName + " heittää");
            UpdateRollListUI();

            waitingForPlayerRoll = true;

            // Wait for the player to roll
            while (waitingForPlayerRoll)
                yield return null;

            yield return new WaitForSeconds(0.4f);
        }

        ResolveStarter();
    }

    private void ResolveStarter()
    {
        int highest = rollResults.Values.Max();

        var winners = rollResults
            .Where(kv => kv.Value == highest)
            .Select(kv => kv.Key)
            .ToList();

        // One clear winner
        if (winners.Count == 1)
        {
            currentTurnIndex = winners[0];

            UpdateTurnText("Aloittaja: " + players[currentTurnIndex].playerName);
            HighlightCurrentPlayer();

            startingPhase = false;
            StartCoroutine(StartGameAfterDelay());
        }
        else
        {
            // Tie → reroll
            UpdateTurnText("Tasapeli! Uusintaheitto.");
            StartCoroutine(TieBreaker(winners));
        }
    }

    private IEnumerator StartGameAfterDelay()
    {
        string starterName = players[currentTurnIndex].playerName;
        if (turnText != null)
            turnText.text = "Aloittaja: " + starterName;

        yield return new WaitForSeconds(2f);

        gameStarted = true;

        if (rollListText != null)
            rollListText.gameObject.SetActive(false);

        if (lastRollText != null)
            lastRollText.gameObject.SetActive(false);

        StartTurn();
    }

    private IEnumerator TieBreaker(List<int> contestants)
    {
        rollResults.Clear();

        foreach (int p in contestants)
        {
            currentRoller = p;
            currentTurnIndex = p;

            HighlightCurrentPlayer();
            UpdateTurnText(players[p].playerName + " heittää (uusinta)");
            UpdateRollListUI();

            waitingForPlayerRoll = true;
            while (waitingForPlayerRoll)
                yield return null;

            yield return new WaitForSeconds(0.4f);
        }

        ResolveStarter();
    }

    // -------------------------
    // NORMAL GAMEPLAY
    // -------------------------

    public void PlayerRoll()
    {
        // Block rolling during animations or transitions
        if (boardRotating || waitingToSkipMessage || diceAnimating || pieceIsMoving)
        {
            waitingForPlayerRoll = true;
            diceJustRolled = false;
            return;
        }

        // Invalid roll request
        if (!waitingForPlayerRoll || diceAnimating || diceJustRolled || pieceIsMoving || waitingToSkipMessage || boardRotating)
            return;

        // Ignore if game hasn't started and not in starting phase
        if (!gameStarted && !startingPhase)
        {
            waitingForPlayerRoll = true;
            return;
        }

        diceJustRolled = true;

        // Dice animation
        StartCoroutine(dice.PlayDiceAnimation());

        int result = dice.Roll();
        lastDiceResult = result;

        rollResults[currentRoller] = result;
        players[currentRoller].SetRollResult(result);

        UpdateRollListUI();
        waitingForPlayerRoll = false;

        // During starting phase stop here
        if (startingPhase)
        {
            diceJustRolled = false;
            return;
        }

        // Example: "Red heitti 3"
        if (gameStarted)
            ShowLastRoll(players[currentTurnIndex].playerName + " heitti " + result);

        DetermineMovablePieces();

        // Rule: 6 gives extra roll
        if (result == 6)
        {
            if (movablePieces.Count == 0)
            {
                ShowLastRoll(players[currentTurnIndex].playerName + " sai 6! Heitä uudestaan.");
                waitingForPlayerRoll = true;
                diceJustRolled = false;
                return;
            }

            ShowMovablePieceIndicators();
            return;
        }

        // Normal roll
        if (movablePieces.Count == 0)
            StartCoroutine(NoMovablePiecesRoutine());
        else
            ShowMovablePieceIndicators();
    }

    void StartTurn()
    {
        gameStarted = true;

        waitingToSkipMessage = false;
        diceJustRolled = false;
        diceAnimating = false;
        pieceIsMoving = false;

        waitingForPlayerRoll = true;

        if (lastRollText != null)
            lastRollText.gameObject.SetActive(false);

        UpdateTurnText("Vuorossa " + players[currentTurnIndex].playerName);
        HighlightCurrentPlayer();

        RotateBoardToCurrentPlayer();
    }

    public void EndTurn()
    {
        diceJustRolled = false;

        currentTurnIndex++;
        if (currentTurnIndex >= playerCount)
            currentTurnIndex = 0;

        StartTurn();
    }

    // -------------------------
    // UI HELPERS
    // -------------------------

    private void UpdateTurnText(string txt)
    {
        if (turnText != null)
            turnText.text = txt;
    }

    private void UpdateRollListUI()
    {
        if (rollListText == null)
            return;

        if (rollResults.Count == 0)
        {
            rollListText.text = "Heittojen tulokset:";
            return;
        }

        var sorted = rollResults.OrderByDescending(x => x.Value);

        string txt = "Heittojen tulokset:\n";
        foreach (var r in sorted)
            txt += players[r.Key].playerName + ": " + r.Value + "\n";

        rollListText.text = txt;
    }

    public void ShowLastRoll(string message)
    {
        if (lastRollRoutine != null)
            StopCoroutine(lastRollRoutine);

        hideLastRollAutomatically = startingPhase;
        lastRollRoutine = StartCoroutine(ShowLastRollRoutine(message));
    }

    private IEnumerator ShowLastRollRoutine(string msg)
    {
        lastRollText.gameObject.SetActive(true);
        lastRollText.text = msg;

        // Auto-hide only in starting phase
        if (hideLastRollAutomatically)
        {
            yield return new WaitForSeconds(1.8f);
            lastRollText.gameObject.SetActive(false);
        }
    }

    // -------------------------
    // MOVEMENT LOGIC
    // -------------------------

    private void DetermineMovablePieces()
    {
        movablePieces.Clear();

        Player p = players[currentTurnIndex];
        foreach (Piece piece in p.pieces)
        {
            if (piece.CanMove(lastDiceResult))
                movablePieces.Add(piece);
        }
    }

    private void ShowMovablePieceIndicators()
    {
        Player p = players[currentTurnIndex];

        foreach (Piece piece in p.pieces)
            piece.EnableSelection(false);

        foreach (Piece piece in movablePieces)
            piece.EnableSelection(true);
    }

    private IEnumerator NoMovablePiecesRoutine()
    {
        ShowLastRoll("Ei siirrettäviä nappuloita");

        waitingToSkipMessage = true;
        float timer = 2.5f;

        // Wait or allow skip
        while (timer > 0f && waitingToSkipMessage)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        if (lastRollText != null)
            lastRollText.gameObject.SetActive(false);

        waitingToSkipMessage = false;

        EndTurn();
    }

    public void PieceSelected(Piece piece)
    {
        foreach (Piece p in movablePieces)
            p.EnableSelection(false);

        pieceIsMoving = true;

        StartCoroutine(AnimatePieceAndContinue(piece));
    }

    private IEnumerator AnimatePieceAndContinue(Piece piece)
    {
        // Move piece with animation
        yield return StartCoroutine(piece.MoveAnimated(lastDiceResult));

        pieceIsMoving = false;

        // Check win after moving
        CheckWinCondition(players[currentTurnIndex]);

        // Extra roll on 6
        if (lastDiceResult == 6)
        {
            diceJustRolled = false;
            waitingForPlayerRoll = true;
            yield break;
        }

        // Rotate board after movement
        RotateBoardToCurrentPlayer();

        // End turn
        EndTurn();
    }

    // -------------------------
    // TILE CHECKS
    // -------------------------

    public bool IsTileOccupiedByOwnPiece(Piece mover, Transform targetTile)
    {
        Player p = players[currentTurnIndex];

        foreach (Piece piece in p.pieces)
        {
            if (piece == mover)
                continue;

            if (piece.routeIndex >= 0 &&
                piece.routeIndex < piece.routeTiles.Length &&
                piece.routeTiles[piece.routeIndex] == targetTile)
            {
                return true;
            }
        }

        return false;
    }

    public Piece GetEnemyPieceOnTile(Piece mover, Transform targetTile)
    {
        for (int i = 0; i < playerCount; i++)
        {
            Player player = players[i];

            if (player == players[currentTurnIndex])
                continue;

            foreach (Piece piece in player.pieces)
            {
                if (piece.routeIndex >= 0 &&
                    piece.routeIndex < piece.routeTiles.Length &&
                    piece.routeTiles[piece.routeIndex] == targetTile)
                {
                    return piece;
                }
            }
        }

        return null;
    }

    // -------------------------
    // GLOW / HIGHLIGHTS
    // -------------------------

    private void HighlightCurrentPlayer()
    {
        for (int i = 0; i < playerCount; i++)
            foreach (Piece p in players[i].pieces)
                p.SetGlow(false);

        foreach (Piece p in players[currentTurnIndex].pieces)
            p.SetGlow(true);
    }

    private void HighlightStartingPlayer(int index)
    {
        for (int i = 0; i < playerCount; i++)
            foreach (Piece piece in players[i].pieces)
                piece.SetGlow(false);

        foreach (Piece piece in players[index].pieces)
            piece.SetGlow(true);
    }

    // -------------------------
    // WIN LOGIC
    // -------------------------

    public void CheckWinCondition(Player player)
    {
        int goalCount = 0;

        foreach (Piece piece in player.pieces)
        {
            if (piece.IsInGoal())
                goalCount++;
        }

        if (goalCount == 4)
            StartCoroutine(ShowWinner(player));
    }

    private IEnumerator ShowWinner(Player player)
    {
        gameStarted = false;
        waitingForPlayerRoll = false;

        yield return new WaitForSeconds(0.6f);

        // Hide active UI
        if (turnText != null) turnText.gameObject.SetActive(false);
        if (lastRollText != null) lastRollText.gameObject.SetActive(false);

        winnerPanel.SetActive(true);
        winnerText.text = player.playerName + " voitti pelin!";
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // -------------------------
    // BOARD ROTATION
    // -------------------------

    private Coroutine rotateRoutine;

    private void RotateBoardToCurrentPlayer()
    {
        boardRotating = true;

        if (boardRoot == null)
            return;

        float targetAngle = players[currentTurnIndex].boardAngle;

        if (rotateRoutine != null)
            StopCoroutine(rotateRoutine);

        rotateRoutine = StartCoroutine(RotateBoardRoutine(targetAngle));

        boardRotating = false;
    }

    private IEnumerator RotateBoardRoutine(float angle)
    {
        boardRotating = true;

        Quaternion startRot = boardRoot.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, angle);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            boardRoot.rotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        boardRoot.rotation = endRot;

        boardRotating = false;
    }

    // Back to main menu
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
