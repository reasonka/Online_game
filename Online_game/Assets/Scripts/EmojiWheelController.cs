using UnityEngine;
using UnityEngine.UI;

public enum EmotionType { Angry, Happy, Crying }

/// <summary>
/// Press R to open a 3-option emoji wheel (Angry / Happy / Crying).
/// Left/Right arrows move the selection, Enter (or Space) confirms and
/// closes the panel automatically, then tells PlayerEmotionController to play it.
/// </summary>
public class EmojiWheelController : MonoBehaviour
{
    [Header("References")]
    public GameObject wheelPanel;           // the whole popup panel, toggled open/closed
    public Image[] optionIcons = new Image[3]; // exactly 3: Angry, Happy, Crying (in that order)
    public PlayerEmotionController emotionController;
    public MonoBehaviour[] scriptsToDisableWhileOpen; // e.g. TestPlayerMover, so arrow keys don't also move the player

    [Header("Visuals")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    public Color selectedColor = Color.white;
    public float selectedScale = 1.2f;

    [Header("Input")]
    public KeyCode openKey = KeyCode.R;
    public KeyCode confirmKey = KeyCode.Return;
    public KeyCode confirmKeyAlt = KeyCode.Space;

    private bool _isOpen;
    private int _selectedIndex;
    private Color[] _baseColors;

    void Awake()
    {
        Debug.Log("EmojiWheelController.Awake() ran on " + gameObject.name);

        if (wheelPanel != null) wheelPanel.SetActive(false);

        _baseColors = new Color[optionIcons.Length];
        for (int i = 0; i < optionIcons.Length; i++)
            _baseColors[i] = optionIcons[i].color;
    }

    void Update()
    {
        // TEMPORARY DIAGNOSTIC - remove once the panel is confirmed working.
        if (Input.GetKeyDown(KeyCode.R))
            Debug.Log("R was pressed! isOpen=" + _isOpen + " wheelPanel=" + (wheelPanel != null) + " enabled=" + enabled + " activeInHierarchy=" + gameObject.activeInHierarchy);

        if (!_isOpen)
        {
            if (Input.GetKeyDown(openKey))
                Open();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _selectedIndex = (_selectedIndex + 1) % optionIcons.Length;
            RefreshVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _selectedIndex = (_selectedIndex - 1 + optionIcons.Length) % optionIcons.Length;
            RefreshVisuals();
        }
        else if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            Confirm();
        }
    }

    void Open()
    {
        Debug.Log("Open() called - activating wheelPanel now");
        _isOpen = true;
        _selectedIndex = 0;
        if (wheelPanel != null) wheelPanel.SetActive(true);
        RefreshVisuals();

        foreach (var s in scriptsToDisableWhileOpen)
            if (s != null) s.enabled = false;
    }

    void Close()
    {
        _isOpen = false;
        if (wheelPanel != null) wheelPanel.SetActive(false);

        foreach (var s in scriptsToDisableWhileOpen)
            if (s != null) s.enabled = true;
    }

    void RefreshVisuals()
    {
        for (int i = 0; i < optionIcons.Length; i++)
        {
            bool isSelected = i == _selectedIndex;
            Color c = _baseColors[i];

            if (!isSelected)
            {
                float gray = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                c = new Color(gray, gray, gray, 0.6f);
            }

            optionIcons[i].color = c;
            optionIcons[i].transform.localScale = Vector3.one * (isSelected ? selectedScale : 0.85f);
        }
    }

    void Confirm()
    {
        EmotionType chosen = (EmotionType)_selectedIndex; // 0=Angry, 1=Happy, 2=Crying
        Close();
        if (emotionController != null)
            emotionController.PlayEmotion(chosen);
    }
}