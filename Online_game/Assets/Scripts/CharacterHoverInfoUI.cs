using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterHoverInfoUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Info Panel")]
    public GameObject infoPanel;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
}