using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItemUI : MonoBehaviour
{
    public TMP_Text roomInfoText;
    public Button roomButton;

    private string roomName;
    private Action<string> onRoomClicked;

    public void SetRoom(string newRoomName, int currentPlayers, int maxPlayers, Action<string> clickAction)
    {
        roomName = newRoomName;
        onRoomClicked = clickAction;

        if (roomInfoText != null)
            roomInfoText.text = roomName + "    " + currentPlayers + "/" + maxPlayers;

        if (roomButton != null)
        {
            roomButton.onClick.RemoveAllListeners();
            roomButton.onClick.AddListener(ClickRoom);
        }
    }

    private void ClickRoom()
    {
        onRoomClicked?.Invoke(roomName);
    }
}