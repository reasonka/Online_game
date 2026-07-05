using UnityEngine;
using Photon.Pun;

public class LocalBookReader : MonoBehaviourPun
{
    [Header("Book UI")]
    public HistoricalBookUI bookUIPrefab;

    [Header("Input")]
    public KeyCode openBookKey = KeyCode.E;
    public KeyCode closeBookKey = KeyCode.Escape;

    private HistoricalBookUI bookUIInstance;
    private bool canReadBook = false;
    private bool isLocalPlayer = true;

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonView view = GetComponentInParent<PhotonView>();

            if (view != null && !view.IsMine)
            {
                isLocalPlayer = false;
                enabled = false;
                return;
            }
        }

        if (bookUIPrefab != null)
        {
            bookUIInstance = Instantiate(bookUIPrefab);
            bookUIInstance.Close();
        }
        else
        {
            Debug.LogWarning("LocalBookReader: Book UI prefab is not assigned.");
        }
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (canReadBook && Input.GetKeyDown(openBookKey))
        {
            if (bookUIInstance != null)
            {
                bookUIInstance.Toggle();
            }
        }

        if (Input.GetKeyDown(closeBookKey))
        {
            if (bookUIInstance != null)
            {
                bookUIInstance.Close();
            }
        }
    }

    public void SetCanReadBook(bool canRead)
    {
        if (!isLocalPlayer)
            return;

        canReadBook = canRead;

        if (!canReadBook && bookUIInstance != null)
        {
            bookUIInstance.Close();
        }
    }
}