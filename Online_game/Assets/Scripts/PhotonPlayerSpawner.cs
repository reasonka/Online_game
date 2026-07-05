using Photon.Pun;
using UnityEngine;

public class PhotonPlayerSpawner : MonoBehaviour
{
    [Header("Photon Prefab Names")]
    public string orderTakerPrefabName = "OrderTakerPlayer";
    public string doodleBuddyPrefabName = "DoodleBuddyPlayer";
    public string chefPrefabName = "ChefPlayer";

    [Header("Spawn Points")]
    public Transform orderTakerSpawnPoint;
    public Transform doodleBuddySpawnPoint;
    public Transform chefSpawnPoint;

    private const string CharacterPropertyKey = "CharacterIndex";

    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CharacterPropertyKey, out object characterValue))
        {
            Debug.LogWarning("No character selected. Spawning Order Taker by default.");
            SpawnCharacter(0);
            return;
        }

        int characterIndex = (int)characterValue;
        SpawnCharacter(characterIndex);
    }

    private void SpawnCharacter(int characterIndex)
    {
        string prefabName = orderTakerPrefabName;
        Transform spawnPoint = orderTakerSpawnPoint;

        if (characterIndex == 1)
        {
            prefabName = doodleBuddyPrefabName;
            spawnPoint = doodleBuddySpawnPoint;
        }
        else if (characterIndex == 2)
        {
            prefabName = chefPrefabName;
            spawnPoint = chefSpawnPoint;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        PhotonNetwork.Instantiate(prefabName, spawnPosition, spawnRotation);
    }
}