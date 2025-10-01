using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text tvUI;
    [SerializeField] private TMP_Text lockerUI;

    [Header("UI Information References")]
    [SerializeField] private TMP_Text roleUI;
    [SerializeField] private TMP_Text attemptUI;
    [SerializeField] private TMP_Text correctUI;

    [Header("Button References")]
    [SerializeField] private Button ClientButton;
    [SerializeField] private Button ServerButton;

    private string cf = "<b><color=#FF3800>";
    private string cr = "<b><color=#00FF4E>";
    private string cc = "</color></b>";


    private void Awake()
    {
        ServerButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("[UIManager] Server gestartet.");
        });

        ClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            ClientButton.interactable = false;
        });
    }


    public void SetTVText(string text)
    {
        string formatText = text.Replace("{cr}", "<b><color=#00FF4E>")
                                .Replace("{cf}", "<b><color=#FF3800>")
                                .Replace("{cc}", "</color></b>");

        tvUI.text = formatText;
    }


    public void SetLockerText(string text)
    {
        lockerUI.text = text;
    }


    #region TV UI Round

    public void ShowMuscle(string latin, string function)
    {
        tvUI.text = $"Bitte zeichne folgenden Muskel an: \n\n{cr}{latin}{cc} \n\nSeine Funktion ist: \n\n{function}";
    }


    public void ShowCorrectMuscle(float percent, string muscle, float ACCURACYNEEDED)
    {
        tvUI.text = $"{cr}SUPER GEMACHT!{cc}\n\n{cr}{percent:F2} %{cc} / {cr}{ACCURACYNEEDED} %{cc} des Muskels \n\n{cr}{muscle}{cc}\n\nwurden korrekt eingezeichnet.";
    }


    public void Incorrect(float f1Score, float accuracy, float recall, string latin, string function, string german, int hintStep, float ACCURACYNEEDED)
    {
        string reason;
        string hintString;

        if (recall <= 0)
        {
            reason = "Der Muskel wurde nicht bemalt.\n\n";
        }
        else if (recall < ACCURACYNEEDED && accuracy >= ACCURACYNEEDED)
        {
            reason = "Muskel nicht ausreichend bemalt.\n\n";
        }
        else if (recall < ACCURACYNEEDED && accuracy < ACCURACYNEEDED)
        {
            reason = "Muskel nicht ausreichen bemalt\nund zu viel übermalt.\n\n";
        }
        else if (recall >= ACCURACYNEEDED && accuracy < ACCURACYNEEDED)
        {
            reason = "Muskel zu viel übermalt.\n\n";
        }
        else
        {
            reason = "Die Bemalung war unpräzise.\n\n";
        }

        if (hintStep == 1)
        {
            hintString = $"{cr}{latin}{cc}\n{function}\n\n" +
                         $"Der deutsche Name des Muskels lautet: \n{german}";
        }
        else if (hintStep == 2)
        {
            hintString = $"{cr}{latin}{cc}\n{function}\n{german}\n\n" +
                         $"Ein Avatar mit dem korrekt \nmarkierten Muskel ist erschienen.";
        }
        else
        {
            hintString = $"Leider wurde der {cf}Muskel{cc} nicht korrekt \n" +
                         $"eingezeichnet und wird {cf}übersprungen{cc}.";
        }


        tvUI.text = $"{cf}{f1Score:F2} %{cc} von {cf}{ACCURACYNEEDED} %{cc} erreicht.\n" +
                    $"{reason}" +
                    $"{hintString}";
    }

    #endregion

    #region Role UI

    public void ShowRoleMALER()
    {
        roleUI.text = "MALER";
    }

    public void ShowRoleANGEMALT()
    {
        roleUI.text = "ANGEMALT";
    }

    #endregion

    #region Attempt UI

    public void ShowAttempt(int attempt, int maxAttempts)
    {
        attemptUI.text = $"Versuch: {attempt + 1}/{maxAttempts}";
    }

    #endregion

    #region Correct UI

    public void ShowRound(int correct, int needed)
    {
        correctUI.text = $"Runde {correct}/{needed}";
    }

    #endregion

    #region Finish Study

    public void FinishStudy(int p1Score, int? p2Score, int maxMuscles)
    {
        if (p2Score.HasValue)
        {
            tvUI.text = $"{cr}Studie abgeschlossen!{cc}\n\nSpieler 1: {p1Score} von {maxMuscles} Muskeln korrekt\nSpieler 2: {p2Score} von {maxMuscles} Muskeln korrekt\n\nDie VR-Brillen können \njetzt abgesetzt werden! \n\nFür Teil 2 bitte an die \nStudienleitung wenden.";
        }
        else
        {
            tvUI.text = $"{cr}Studie abgeschlossen!{cc}\n\n{p1Score} von {maxMuscles} Muskeln korrekt\n\nDie VR-Brillen können \njetzt abgesetzt werden! \n\nFür Teil 2 bitte an die \nStudienleitung wenden.";
        }
    }

    #endregion

}

