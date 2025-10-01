using System.Collections;
using UnityEngine;

public class PresentationManager : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;


    public void PlayInstructionList(InstructionList list)
    {
        StartCoroutine(ListCoroutine(list));
    }


    private IEnumerator ListCoroutine(InstructionList list)
    {
        foreach (var step in list.steps)
        {
            uiManager.SetTVText(step.text);
            audioManager.PlayInstruction(step.audioClip);

            yield return new WaitForSeconds(step.duration + 0.7f);
        }
    }


    public void PlaySingleStep(InstructionStep step)
    {
        switch (step.targetDisplay)
        {
            case UITarget.TV:
                uiManager.SetTVText(step.text);
                break;
            case UITarget.Locker:
                uiManager.SetLockerText(step.text);
                break;
        }

        audioManager.PlayInstruction(step.audioClip);
    }


    public void PlayAudio(AudioClip clip)
    {
        audioManager.PlayInstruction(clip);
    }


    public void PlayBackground(AudioClip backgroundMusic)
    {
        audioManager.PlayBackgroundMusic(backgroundMusic);
    }
} 
