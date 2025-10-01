using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    PaintManager paintMgr;

    private Toggle eraserToggle, leftHandToggle;
    private Slider brushSizeSlider;
    private Button blueButton, redButton, yellowButton;
    private Button menuButton;

    private string contentPath = "XR Origin (XR Rig)/ButtonHandMenu/Hand Menu ScrollView/Follow GameObject/Hand Scroll View/Panel/Scroll View/Viewport/Content";

    private string eraserTogglePath, leftHandTogglePath;
    private string brushSizeSliderPath;
    private string buttonTextPath, buttonWrapperPath, blueButtonPath, redButtonPath, yellowButtonPath;
    private string menuButtonPath = "XR Origin (XR Rig)/ButtonHandMenu/Hand Menu Wrist Button/Follow GameObject/Wrist Button UI/Text Button";

    GameObject eraserGO, leftHandGO;
    GameObject brushSizeSliderGO;
    GameObject buttonTextGO, buttonWrapperGO, blueButtonGO, redButtonGO, yellowButtonGO;
    GameObject menuButtonGO;
    ColorBlock brushSizeSliderCB, menuButtonCB;

    bool eraserToggled = false;

    private string eraserBackgroundPath, leftHandBackgroundPath;
    GameObject eraserBackgroundGO, leftHandBackgroundGO;
    private Image eraserBackground, leftHandBackground;

    private Color blue = new Color(32f / 255f, 150f / 255f, 243f / 255f);
    private Color red = new Color(243f / 255f, 36f / 255f, 32f / 255f);
    private Color yellow = new Color(243f / 255f, 217f / 255f, 32f / 255f);


    public MenuManager() { }


    public void InitializeButtons()
    {
        paintMgr = GetComponent<PaintManager>();

        SetPaths();
        GetGameObjects();
        GetComponents();
        AddListeners();
    }


    private void SetPaths()
    {
        eraserTogglePath = contentPath + "/Toggles/Offset Anchor Radiergummi/RadiergummiToggle";
        eraserBackgroundPath = eraserTogglePath + "/Background/Background On";

        leftHandTogglePath = contentPath + "/Toggles/Offset Anchor Linkshaender/LinkshaenderToggle";
        leftHandBackgroundPath = leftHandTogglePath + "/Background/Background On";

        brushSizeSliderPath = contentPath + "/List Item Slider/MinMax Slider";

        buttonTextPath = contentPath + "/Color Button";
        buttonWrapperPath = contentPath + "/ButtonText";
        blueButtonPath = contentPath + "/Color Button/Blau";
        redButtonPath = contentPath + "/Color Button/Rot";
        yellowButtonPath = contentPath + "/Color Button/Gelb";
    }


    private void GetGameObjects()
    {
        eraserGO = GameObject.Find(eraserTogglePath);
        eraserBackgroundGO = GameObject.Find(eraserBackgroundPath);

        leftHandGO = GameObject.Find(leftHandTogglePath);
        leftHandBackgroundGO = GameObject.Find(leftHandBackgroundPath);

        brushSizeSliderGO = GameObject.Find(brushSizeSliderPath);

        buttonTextGO = GameObject.Find(buttonTextPath);
        buttonWrapperGO = GameObject.Find(buttonWrapperPath);
        blueButtonGO = GameObject.Find(blueButtonPath);
        redButtonGO = GameObject.Find(redButtonPath);
        yellowButtonGO = GameObject.Find(yellowButtonPath);

        menuButtonGO = GameObject.Find(menuButtonPath);

        // Debug.Log($"eraserGO {eraserGO} \n leftHandGO leftHandGO \n brushSizeSliderGO {brushSizeSliderGO} \n buttonTextGO {buttonTextGO} \n buttonWrapperGO {buttonWrapperGO} \n blueButtonGO {blueButtonGO} \n redButtonGO {redButtonGO} \n yellowButtonGO {yellowButtonGO}");
    }


    private void GetComponents()
    {
        eraserToggle = eraserGO.GetComponent<Toggle>();
        eraserBackground = eraserBackgroundGO.GetComponent<Image>();

        leftHandToggle = leftHandGO.GetComponent<Toggle>();
        leftHandBackground = leftHandBackgroundGO.GetComponent<Image>();

        brushSizeSlider = brushSizeSliderGO.GetComponent<Slider>();
        brushSizeSliderCB = brushSizeSlider.colors;

        blueButton = blueButtonGO.GetComponent<Button>();
        redButton = redButtonGO.GetComponent<Button>();
        yellowButton = yellowButtonGO.GetComponent<Button>();

        menuButton = menuButtonGO.GetComponent<Button>();
        menuButtonCB = menuButton.colors;
    }


    private void AddListeners()
    {
        eraserToggle.onValueChanged.AddListener(_ => paintMgr.toggleEraser());
        eraserToggle.onValueChanged.AddListener(_ => ToggleBrushColor());

        leftHandToggle.onValueChanged.AddListener(_ => paintMgr.toggleLeftHand());

        brushSizeSlider.onValueChanged.AddListener(paintMgr.setBrushSize);
        brushSizeSlider.SetValueWithoutNotify(0.01f);

        blueButton.onClick.AddListener(paintMgr.setBrushBlue);
        blueButton.onClick.AddListener(() => setMenuColor(blue));

        redButton.onClick.AddListener(paintMgr.setBrushRed);
        redButton.onClick.AddListener(() => setMenuColor(red));

        yellowButton.onClick.AddListener(paintMgr.setBrushYellow);
        yellowButton.onClick.AddListener(() => setMenuColor(yellow));
    }


    private void ToggleBrushColor()
    {
        buttonTextGO.SetActive(eraserToggled);
        buttonWrapperGO.SetActive(eraserToggled);
        eraserToggled = !eraserToggled;
    }

    private void setMenuColor(Color color)
    {
        eraserBackground.color = color;
        leftHandBackground.color = color;

        brushSizeSliderCB.pressedColor = color;
        brushSizeSliderCB.highlightedColor = color;
        brushSizeSliderCB.selectedColor = color;
        brushSizeSlider.colors = brushSizeSliderCB;

        menuButtonCB.normalColor = color;
        menuButtonCB.pressedColor = color;
        menuButtonCB.highlightedColor = color;
        menuButton.colors = menuButtonCB;
    }

}
