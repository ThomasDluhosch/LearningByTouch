using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GameUIManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PresentationManager presentationManager;
    [SerializeField] private GameObject submitButton;

    [Header("Instructions")]
    [SerializeField] private InstructionList spTutorialList;
    [SerializeField] private InstructionList mpTutorialList;
    [SerializeField] private InstructionStep introduction;
    [SerializeField] private InstructionStep waitForPlayerTwo;

    [Header("Serialized Instruction List")]
    [SerializeField] private List<InstructionList> multipleInstructions;
    [SerializeField] private List<InstructionStep> singleInstructions;

    [Header("Audio Instructions")]
    [SerializeField] private AudioClip finishedStudyClip;
    [SerializeField] private AudioClip correctSubmitClip;
    [SerializeField] private AudioClip wrongSubmitClip;
    [SerializeField] private AudioClip backgroundMusic;


    #region Instructions

    public void ShowRoleAngemalt() => uiManager.ShowRoleANGEMALT();

    public void PlayIntroduction() => PlayInstruction(introduction);

    public void PlayWaitForPlayerTwo() => PlayInstruction(waitForPlayerTwo);


    private void PlayInstruction(InstructionStep instruction)
    {
        int stepIndex = singleInstructions.IndexOf(instruction);
        if (stepIndex == -1) return;

        presentationManager.PlaySingleStep(instruction);
        PlayInstructionClientRpc(stepIndex);
    }

    [ClientRpc]
    private void PlayInstructionClientRpc(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= singleInstructions.Count) return;
        InstructionStep stepToPlay = singleInstructions[stepIndex];
        presentationManager.PlaySingleStep(stepToPlay);
    }

    #endregion

    #region Background

    public void PlayBackgroundMusic()
    {
        presentationManager.PlayBackground(backgroundMusic);
        PlayBackgroundMusicClientRpc();
    }

    [ClientRpc]
    private void PlayBackgroundMusicClientRpc()
    {
        presentationManager.PlayBackground(backgroundMusic);
    }

    #endregion

    #region Tutorial

    public IEnumerator PlayTutorial(GameMode gameMode)
    {
        yield return new WaitForSeconds(2f);

        InstructionList sequenceToPlay = (gameMode == GameMode.SinglePlayer) ? spTutorialList : mpTutorialList;

        int listIndex = multipleInstructions.IndexOf(sequenceToPlay);

        if (listIndex == -1) yield break;


        float tutorialDuration = 0;
        foreach (var step in sequenceToPlay.steps) tutorialDuration += step.duration + 0.7f;


        presentationManager.PlayInstructionList(sequenceToPlay);
        PlayTutorialClientRpc(listIndex);

        yield return new WaitForSeconds(tutorialDuration);
    }


    [ClientRpc]
    private void PlayTutorialClientRpc(int listIndex)
    {
        if (IsServer || listIndex < 0 || listIndex > multipleInstructions.Count) return;

        InstructionList listToPlayOnClient = multipleInstructions[listIndex];
        presentationManager.PlayInstructionList(listToPlayOnClient);
    }

    #endregion

    #region Muscle & Feedback

    public void ShowMuscle(string latinName, string function)
    {
        uiManager.ShowMuscle(latinName, function);
        ShowMuscleClientRpc(latinName, function);
    }

    [ClientRpc]
    private void ShowMuscleClientRpc(string latin, string function)
    {
        uiManager.ShowMuscle(latin, function);
    }


    public void ShowCorrectFeedback(float accuracy, string latinName, float ACCURACYNEEDED)
    {
        uiManager.ShowCorrectMuscle(accuracy, latinName, ACCURACYNEEDED);
        ShowCorrectFeedbackClientRpc(accuracy, latinName, ACCURACYNEEDED);
    }

    [ClientRpc]
    private void ShowCorrectFeedbackClientRpc(float acc, string latin, float ACCURACYNEEDED)
    {
        presentationManager.PlayAudio(correctSubmitClip);
        uiManager.ShowCorrectMuscle(acc, latin, ACCURACYNEEDED);
    }


    public void ShowIncorrectFeedback(int hintStep, float f1Score, MuscleData muscle, PaintStats stats, float ACCURACYNEEDED)
    {
        float recall = stats.totalPaintedPixels > 0
                      ? 100 * (float)stats.correctPaintedPixels / (float)stats.totalPaintedPixels
                      : 0f;

        float accuracy = stats.referenceMaskPixelCount > 0
                     ? 100 * (float)stats.correctPaintedPixels / (float)stats.referenceMaskPixelCount
                     : 0f;

        if (hintStep == 1) uiManager.Incorrect(f1Score, accuracy, recall, muscle.LatinName, muscle.function, muscle.GermanName, 1, ACCURACYNEEDED);
        if (hintStep == 2) uiManager.Incorrect(f1Score, accuracy, recall, muscle.LatinName, muscle.function, muscle.GermanName, 2, ACCURACYNEEDED);
        if (hintStep == 3) uiManager.Incorrect(f1Score, accuracy, recall, muscle.LatinName, muscle.function, muscle.GermanName, 3, ACCURACYNEEDED);
        ShowIncorrectFeedbackClientRpc(f1Score, accuracy, recall, muscle.LatinName, muscle.function, muscle.GermanName, hintStep, ACCURACYNEEDED);
    }

    [ClientRpc]
    private void ShowIncorrectFeedbackClientRpc(float f1Score, float accuracy, float recall, string latin, string function, string german, int hintStep, float ACCURACYNEEDED)
    {
        presentationManager.PlayAudio(wrongSubmitClip);
        if (hintStep == 1) uiManager.Incorrect(f1Score, accuracy, recall, latin, function, german, 1, ACCURACYNEEDED);
        if (hintStep == 2) uiManager.Incorrect(f1Score, accuracy, recall, latin, function, german, 2, ACCURACYNEEDED);
        if (hintStep == 3) uiManager.Incorrect(f1Score, accuracy, recall, latin, function, german, 3, ACCURACYNEEDED);
    }


    // public void ShowSkipMuscle(string latinName, int incorrect, int maxIncorrect)
    // {
    //     uiManager.SkipMuscle(latinName, incorrect, maxIncorrect);
    //     ShowSkipMuscleClientRpc(latinName, incorrect, maxIncorrect);
    // }

    // [ClientRpc]
    // private void ShowSkipMuscleClientRpc(string latin, int incorrect, int maxIncorrect)
    // {
    //     uiManager.SkipMuscle(latin, incorrect, maxIncorrect);
    // }

    #endregion

    #region Score & Attempt

    public void UpdateScore(int correct, int needed)
    {
        uiManager.ShowRound(correct, needed);
        UpdateScoreClientRpc(correct, needed);
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(int c, int n)
    {
        uiManager.ShowRound(c, n);
    }


    public void UpdateAttempt(int hint, int maxAttempts)
    {
        uiManager.ShowAttempt(hint, maxAttempts);
        UpdateAttemptClientRpc(hint, maxAttempts);
    }

    [ClientRpc]
    private void UpdateAttemptClientRpc(int h, int m)
    {
        uiManager.ShowAttempt(h, m);
    }

    #endregion

    #region Submit Button

    public void SetSubmitButtonEnabled(bool isEnabled)
    {
        submitButton.GetComponent<XRSimpleInteractable>().enabled = isEnabled;
        SetSubmitButtonEnabledClientRpc(isEnabled);
    }

    [ClientRpc]
    private void SetSubmitButtonEnabledClientRpc(bool isEnabled)
    {
        submitButton.GetComponent<XRSimpleInteractable>().enabled = isEnabled;
    }

    #endregion

    #region Finished Study

    public void FinishStudy(int p1Score, int? p2Score, int maxMuscles)
    {
        if (p2Score.HasValue)
        {
            uiManager.FinishStudy(p1Score, p2Score.Value, maxMuscles);
        }
        else
        {
            uiManager.FinishStudy(p1Score, null, maxMuscles);
        }

        FinishStudyClientRpc(p1Score, p2Score ?? 0, p2Score.HasValue, maxMuscles);
    }

    [ClientRpc]
    private void FinishStudyClientRpc(int p1Score, int p2ScoreValue, bool p2HasScore, int maxMuscles)
    {
        int? finalP2Score = p2HasScore ? p2ScoreValue : (int?)null;

        uiManager.FinishStudy(p1Score, finalP2Score, maxMuscles);
        presentationManager.PlayAudio(finishedStudyClip);
    }

    #endregion

}
