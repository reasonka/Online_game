using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndSceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";
    public string level1EndSceneName = "L1End";
    public string level2EndSceneName = "L2End";

    [Header("Photon")]
    public bool usePhoton = true;
    public bool onlyMasterClientCanLoad = true;

    public void LoadEndSceneForCurrentLevel()
    {
        string currentSceneName =
            SceneManager.GetActiveScene().name;

        if (currentSceneName == level1SceneName)
        {
            LoadScene(level1EndSceneName);
        }
        else if (currentSceneName == level2SceneName)
        {
            LoadScene(level2EndSceneName);
        }
        else
        {
            Debug.LogWarning(
                "No end scene set for current scene: " +
                currentSceneName
            );
        }
    }

    public void LoadLevel1EndScene()
    {
        LoadScene(level1EndSceneName);
    }

    public void LoadLevel2EndScene()
    {
        LoadScene(level2EndSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (usePhoton && PhotonNetwork.IsConnected)
        {
            if (onlyMasterClientCanLoad &&
                !PhotonNetwork.IsMasterClient)
            {
                Debug.Log(
                    "Only Master Client can load the end scene."
                );
                return;
            }

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}