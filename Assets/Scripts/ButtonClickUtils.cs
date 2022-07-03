using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonClickUtils : MonoBehaviour, IPointerClickHandler
{
    public MainMenu mainMenu;
    private string worldName;

    public void OnPointerClick(PointerEventData eventData)
    {
        int clicks = eventData.clickCount;

        if (clicks == 1)
            OnSingleClick();
        else if (clicks >= 2)
            OnDoubleClick();
    }

    void OnSingleClick()
    {
        this.worldName = this.gameObject.GetComponentInParent<Button>().GetComponentInChildren<TextMeshPro>().text;
    }

    void OnDoubleClick()
    {
        mainMenu.StartGameSingleplayer(worldName);
    }
}