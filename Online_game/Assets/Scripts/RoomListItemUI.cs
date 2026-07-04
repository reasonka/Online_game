using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItemUI : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private string roomName;
    private Action<string> onJoinClicked;

    public void SetRoom(string newRoomName, int currentPlayers, int maxPlayers, Action<string> joinAction)
    {
        roomName = newRoomName;
        onJoinClicked = joinAction;

        roomNameText.text = roomName;
        playerCountText.text = currentPlayers + "/" + maxPlayers;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(JoinRoom);
    }

    private void JoinRoom()
    {
        onJoinClicked?.Invoke(roomName);
    }
}