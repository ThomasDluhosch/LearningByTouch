using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public enum GameMode
{
    SinglePlayer,
    MultiPlayer
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Config")]
    [SerializeField] private GameMode gameMode = GameMode.MultiPlayer;
    private const float ACCURACY_NEEDED_TO_CONTINUE = 70f;
    private const int ROUNDS_PER_PLAYER = 5;

    [Space(10)]
    [Header("Script References")]
    [SerializeField] private AvatarManager avatarManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameUIManager gameUIManager;
    [SerializeField] private CSVLogger csvLogger;

    [Space(10)]
    [Header("Asset References")]
    [SerializeField] private MuscleDatabase muscleDB;
    [SerializeField] private GameObject submitButton;
    [SerializeField] private UIManager _uiManager;

    // Game
    private MuscleData currentMuscle;
    private int painterCorrect = 0;
    private int canvasCorrect = 0;
    private int hintStep = 0;
    private float accuracy = 0;
    private bool studyFinished = false;
    private float showFeedbackFlashTime = 1f;
    private bool[] musclesShown;
    private int roundsPlayedByPainter = 0;
    private bool rolesHaveBeenSwapped = false;

    // TODO switch to GameState

    #region Setup

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        csvLogger.Initialize();
        gameUIManager.PlayIntroduction();

        musclesShown = new bool[muscleDB.muscles.Count()];
    }

    public MuscleDatabase getMuscleDB() => muscleDB;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    #endregion

    #region Client Connection

    private void OnClientConnect(ulong clientId)
    {
        Debug.Log($"[GameManager] Client connected: {clientId}");
        var clientCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (gameMode == GameMode.SinglePlayer)
        {
            if (clientCount == 1) StartCoroutine(DelayedStartRound());
        }
        else // gameMode == GameMode.Multiplayer
        {
            if (clientCount == 1) gameUIManager.PlayWaitForPlayerTwo();
            else if (clientCount == 2) StartCoroutine(DelayedStartRound());
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log($"[GameManager] Client disconnected: {clientId}");

        if (studyFinished) return;

        if (gameMode == GameMode.MultiPlayer && NetworkManager.Singleton.ConnectedClientsList.Count < 2)
        {
            Debug.LogError("[GameManager] Client disconnected before finish!");
        }
    }

    #endregion

    #region Round Logic

    private IEnumerator DelayedStartRound()
    {
        gameUIManager.SetSubmitButtonEnabled(false);
        gameUIManager.PlayBackgroundMusic();
        playerManager.TeleportAllPlayers();
        yield return new WaitForSeconds(1f);
        yield return gameUIManager.PlayTutorial(gameMode);

        playerManager.AssignRoles(gameMode);

        if (gameMode == GameMode.SinglePlayer) avatarManager.SpawnPaintableAvatar();


        BeginRound();
        gameUIManager.SetSubmitButtonEnabled(true);
    }


    private void BeginRound()
    {
        roundsPlayedByPainter++;
        gameUIManager.UpdateScore(roundsPlayedByPainter, ROUNDS_PER_PLAYER);

        avatarManager.DespawnHintAvatar();
        hintStep = 0;
        showFeedbackFlashTime = 1;

        int nextMuscleIndex = GetNextMuscleIndex();

        if (nextMuscleIndex == -1)
        {
            StartCoroutine(FinishStudy());
            return;
        }

        musclesShown[nextMuscleIndex] = true;

        currentMuscle = muscleDB.Muscles[nextMuscleIndex];

        playerManager.SetAllPlayerReferenceTextures(currentMuscle.Index, currentMuscle.ReferenceTexture);

        if (gameMode == GameMode.SinglePlayer)
            avatarManager.getPaintableAvatarPM.SetReferenceTexture(currentMuscle.ReferenceTexture);

        gameUIManager.ShowMuscle(currentMuscle.LatinName, currentMuscle.function);
        gameUIManager.UpdateAttempt(0, 3);
    }


    public void ProcessSubmit()
    {
        if (!IsServer || playerManager.canvasNP == null) return;

        PaintManager canvas;

        if (gameMode == GameMode.SinglePlayer)
        {
            canvas = avatarManager.getPaintableAvatarPM;
        }
        else // gameMode == GameMode.MultiPlayer
        {
            canvas = playerManager.canvasNP.GetComponent<PaintManager>();
        }

        PaintStats ps = canvas.CompareWithReference();
        accuracy = CalculateAccuracy(ps);

        csvLogger.SaveRound(ps, accuracy, playerManager.painterNP.PlayerName, playerManager.canvasNP.PlayerName, currentMuscle.Index, currentMuscle.GermanName, hintStep);

        gameUIManager.SetSubmitButtonEnabled(false);

        if (accuracy >= ACCURACY_NEEDED_TO_CONTINUE)
        {
            StartCoroutine(HandleCorrectSubmit(accuracy, canvas));
        }
        else
        {
            StartCoroutine(HandleIncorrectSubmit(accuracy, canvas, ps));
        }

    }


    private float CalculateAccuracy(PaintStats stats)
    {
        float recall = stats.totalPaintedPixels > 0
                      ? 100 * (float)stats.correctPaintedPixels / (float)stats.totalPaintedPixels
                      : 0f;

        float accuracy = stats.referenceMaskPixelCount > 0
                     ? 100 * (float)stats.overpaintedPixels / (float)stats.referenceMaskPixelCount
                     : 0f;

        float f1denominator = stats.totalPaintedPixels + stats.referenceMaskPixelCount;
        float f1numerator = 2f * stats.correctPaintedPixels;
        float f1Score = 100f * f1numerator / f1denominator;

        Debug.Log(
            $"<b><color=#a2ff00>[GameManager]</color></b> [CalculateAccuracy]\n" +
            $"Total pixels painted: {stats.totalPaintedPixels}\n" +
            $"Reference pixels of muscle: {stats.referenceMaskPixelCount}\n" +
            $"Accuracy: {stats.correctPaintedPixels} -> {accuracy} %\n" +
            $"Recall pixels:</b> {stats.overpaintedPixels} -> {recall} %\n" +
            $"f1Score: {f1Score} %"
        );

        return f1Score;
    }


    private IEnumerator HandleCorrectSubmit(float accuracy, PaintManager canvas)
    {
        painterCorrect++;
        showFeedbackFlashTime = 5f;

        gameUIManager.ShowCorrectFeedback(accuracy, currentMuscle.LatinName, ACCURACY_NEEDED_TO_CONTINUE);
        canvas.ShowFeedBackFlashRpc(currentMuscle.Index, showFeedbackFlashTime);

        canvas.SaveFinalTextureAsPNG();

        if (IsGameFinished())
        {
            StartCoroutine(FinishStudy());
            yield break;
        }

        yield return new WaitForSeconds(5f);
        BroadcastClearToAll();
        gameUIManager.SetSubmitButtonEnabled(true);
        BeginRound();
    }


    private IEnumerator HandleIncorrectSubmit(float f1Score, PaintManager canvas, PaintStats stats)
    {
        hintStep++;

        switch (hintStep)
        {
            case 1:
                gameUIManager.UpdateAttempt(hintStep, 3);
                showFeedbackFlashTime = 3f;
                gameUIManager.ShowIncorrectFeedback(1, f1Score, currentMuscle, stats, ACCURACY_NEEDED_TO_CONTINUE);
                canvas.ShowFeedBackFlashRpc(currentMuscle.Index, showFeedbackFlashTime);
                // canvas.SaveFinalTextureAsPNG();
                yield return new WaitForSeconds(showFeedbackFlashTime);
                BroadcastClearToAll();
                break;

            case 2:
                gameUIManager.UpdateAttempt(hintStep, 3);
                showFeedbackFlashTime = 4f;
                gameUIManager.ShowIncorrectFeedback(2, f1Score, currentMuscle, stats, ACCURACY_NEEDED_TO_CONTINUE);
                avatarManager.SpawnHintAvatar(currentMuscle);
                canvas.ShowFeedBackFlashRpc(currentMuscle.Index, showFeedbackFlashTime);
                // canvas.SaveFinalTextureAsPNG();
                yield return new WaitForSeconds(showFeedbackFlashTime);
                BroadcastClearToAll();
                break;

            default:
                showFeedbackFlashTime = 5f;
                gameUIManager.ShowIncorrectFeedback(3, f1Score, currentMuscle, stats, ACCURACY_NEEDED_TO_CONTINUE);
                canvas.ShowFeedBackFlashRpc(currentMuscle.Index, showFeedbackFlashTime);

                if (IsGameFinished())
                {
                    StartCoroutine(FinishStudy());
                    yield break;
                }

                // canvas.SaveFinalTextureAsPNG();
                yield return new WaitForSeconds(showFeedbackFlashTime);
                BroadcastClearToAll();
                BeginRound();
                break;
        }

        gameUIManager.SetSubmitButtonEnabled(true);
    }


    private int GetNextMuscleIndex()
    {
        List<int> candidates = new List<int>();

        for (int i = 0; i < musclesShown.Length; i++)
        {
            if (!musclesShown[i])
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0) return -1;
        int randomIdx = candidates[Random.Range(0, candidates.Count)];

        return randomIdx;
    }


    private bool IsGameFinished()
    {
        bool painterFinished = roundsPlayedByPainter >= ROUNDS_PER_PLAYER;

        if (gameMode == GameMode.SinglePlayer)
        {
            return painterFinished;
        }
        else
        {
            if (painterFinished)
            {
                if (rolesHaveBeenSwapped)
                {
                    return true;
                }
                else
                {
                    (painterCorrect, canvasCorrect) = (canvasCorrect, painterCorrect);
                    rolesHaveBeenSwapped = true;
                    roundsPlayedByPainter = 0;
                    playerManager.AssignRoles(gameMode);
                }
            }
            return false;
        }
    }


    private IEnumerator FinishStudy()
    {
        if (studyFinished) yield break;

        studyFinished = true;
        gameUIManager.SetSubmitButtonEnabled(false);
        avatarManager.DespawnHintAvatar();

        yield return new WaitForSeconds(5f);

        if (gameMode == GameMode.SinglePlayer)
        {
            avatarManager.DespawnPaintableAvatar();
            gameUIManager.FinishStudy(painterCorrect, null, ROUNDS_PER_PLAYER);
        }
        else
        {
            gameUIManager.FinishStudy(canvasCorrect, painterCorrect, ROUNDS_PER_PLAYER);
        }
    }

    #endregion

    #region Helper Functions

    private void BroadcastClearToAll()
    {
        if (gameMode == GameMode.SinglePlayer)
        {
            avatarManager.getPaintableAvatarPM.ClearFinalRT();

            avatarManager.getPaintableAvatarPM
                .ClearPaintClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { playerManager.painterNP.OwnerClientId }
                    }
                });
        }
        else
        {
            playerManager.ClearAllPlayerPaint();
        }
    }

    #endregion

}
