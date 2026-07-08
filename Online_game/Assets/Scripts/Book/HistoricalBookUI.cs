using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public enum BookOverlayEntryType
{
    HistoryText,
    PerformanceImage
}

[System.Serializable]
public class BookPageOverlayEntry
{
    [Header("Page")]
    public int pageIndex;
    public BookOverlayEntryType entryType;

    [Header("History Text Settings")]
    public int changedAfterLevel = 1;
    public string foodGroupName;

    [TextArea] public string originalInventedIn;
    [TextArea] public string originalBy;

    [TextArea] public string changedInventedIn;
    public bool usePlayerNamesForChangedBy = true;

    [Header("Performance Image Settings")]
    public int performanceLevelNumber = 1;
}

[System.Serializable]
public class BookOverlaySide
{
    public GameObject root;

    [Header("History Text UI")]
    public GameObject historyTextGroup;
    public TMP_Text inventedInValueText;
    public TMP_Text byValueText;

    [Header("Performance Image UI")]
    public GameObject performanceImageGroup;
    public Image performanceImage;
}

[System.Serializable]
public class LevelPerformanceSpriteSet
{
    public int levelNumber = 1;

    [Header("Sprites for this level")]
    public Sprite happyCustomersSprite;
    public Sprite angryCustomersSprite;
    public Sprite deadCustomersSprite;
}

public class HistoricalBookUI : MonoBehaviour
{
    [Header("Book")]
    public GameObject rootPanel;
    public Book book;

    [Header("Overlay Layer")]
    public RectTransform overlayLayer;
    public bool forceOverlayLayerOnTop = true;

    [Header("Flip Behaviour")]
    public bool hideOverlayWhileFlipping = true;

    [Tooltip("Small delay after the page finishes flipping before the overlay appears again.")]
    public float showOverlayAfterFlipDelay = 0.08f;

    [Header("Reveal Animation")]
    public bool animateOverlayReveal = true;

    [Tooltip("Fade/scale duration for the overlay after the page lands.")]
    public float revealDuration = 0.25f;

    [Tooltip("Start scale for the overlay reveal. 0.97 means it grows slightly into place.")]
    public float revealStartScale = 0.97f;

    [Tooltip("Animate the TMP text as if it is being written.")]
    public bool useTypewriterText = true;

    [Tooltip("How long the text writing animation takes.")]
    public float typewriterDuration = 0.35f;

    [Header("Left Page Overlay")]
    public BookOverlaySide leftOverlay;

    [Header("Right Page Overlay")]
    public BookOverlaySide rightOverlay;

    [Header("Book Page Overlay Entries")]
    public List<BookPageOverlayEntry> pageEntries = new List<BookPageOverlayEntry>();

    [Header("Performance Sprite Sets")]
    public List<LevelPerformanceSpriteSet> performanceSpriteSets = new List<LevelPerformanceSpriteSet>();

    [Header("Debug Complete Book Cheat")]
    public bool allowDebugCompleteBookKey = true;
    public KeyCode debugCompleteBookKey = KeyCode.Alpha9;
    public bool alsoAllowKeypad9 = true;

    [Tooltip("When true, pressing 9 makes Level 1 show Dead and Level 2 show Happy.")]
    public bool debugForceBookComplete = false;

    [Tooltip("Used if Photon player names are not available.")]
    public string debugFallbackPlayerNames = "Player 1 & Player 2";

    [Header("Debug")]
    public bool debugPageIndexes = false;

    private int lastKnownCurrentPage = -999;
    private bool overlaysHiddenBecauseOfFlip = false;
    private float lastFlipFinishedTime = -999f;

    private readonly List<Coroutine> runningRevealCoroutines = new List<Coroutine>();
    private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        if (book != null)
        {
            book.OnFlip.AddListener(OnBookFlipped);
        }

        HideSide(leftOverlay);
        HideSide(rightOverlay);
    }

    private void Update()
    {
        if (!allowDebugCompleteBookKey)
            return;

        bool pressedDebugKey = Input.GetKeyDown(debugCompleteBookKey);

        if (alsoAllowKeypad9)
        {
            pressedDebugKey = pressedDebugKey || Input.GetKeyDown(KeyCode.Keypad9);
        }

        if (pressedDebugKey)
        {
            DebugCompleteBook();
        }
    }

    private void OnEnable()
    {
        ForceOverlayOnTop();
        RefreshVisiblePageOverlays(true);
    }

    private void OnDestroy()
    {
        if (book != null)
        {
            book.OnFlip.RemoveListener(OnBookFlipped);
        }
    }

    private void LateUpdate()
    {
        ForceOverlayOnTop();

        if (book == null)
            return;

        bool bookIsFlipping = IsBookCurrentlyFlipping();

        if (hideOverlayWhileFlipping && bookIsFlipping)
        {
            if (!overlaysHiddenBecauseOfFlip)
            {
                overlaysHiddenBecauseOfFlip = true;
                StopRevealAnimations();
                HideSide(leftOverlay);
                HideSide(rightOverlay);
            }

            return;
        }

        if (overlaysHiddenBecauseOfFlip && !bookIsFlipping)
        {
            if (Time.time - lastFlipFinishedTime >= showOverlayAfterFlipDelay)
            {
                overlaysHiddenBecauseOfFlip = false;
                RefreshVisiblePageOverlays(true);
            }

            return;
        }

        if (book.currentPage != lastKnownCurrentPage)
        {
            RefreshVisiblePageOverlays(true);
        }
    }

    public void DebugCompleteBook()
    {
        debugForceBookComplete = true;

        Debug.Log("DEBUG BOOK COMPLETE: Level 1 = Dead, Level 2 = Happy.");

        RefreshVisiblePageOverlays(true);
    }

    public void DebugResetBookToRealData()
    {
        debugForceBookComplete = false;

        Debug.Log("DEBUG BOOK RESET: Book is using real OurGameManager data again.");

        RefreshVisiblePageOverlays(true);
    }

    public void Open()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }

        overlaysHiddenBecauseOfFlip = false;
        ForceOverlayOnTop();
        RefreshVisiblePageOverlays(true);
    }

    public void Close()
    {
        StopRevealAnimations();
        HideSide(leftOverlay);
        HideSide(rightOverlay);
    }

    public void Toggle()
    {
        if (rootPanel == null)
            return;

        if (rootPanel.activeSelf)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void OnBookFlipped()
    {
        lastFlipFinishedTime = Time.time;

        if (!hideOverlayWhileFlipping)
        {
            RefreshVisiblePageOverlays(true);
        }
    }

    private bool IsBookCurrentlyFlipping()
    {
        if (book == null)
            return false;

        bool leftTempPageActive = book.Left != null && book.Left.gameObject.activeInHierarchy;
        bool rightTempPageActive = book.Right != null && book.Right.gameObject.activeInHierarchy;

        return leftTempPageActive || rightTempPageActive;
    }

    public void RefreshVisiblePageOverlays()
    {
        RefreshVisiblePageOverlays(false);
    }

    public void RefreshVisiblePageOverlays(bool animateReveal)
    {
        if (book == null)
        {
            HideSide(leftOverlay);
            HideSide(rightOverlay);
            return;
        }

        if (hideOverlayWhileFlipping && IsBookCurrentlyFlipping())
        {
            HideSide(leftOverlay);
            HideSide(rightOverlay);
            return;
        }

        lastKnownCurrentPage = book.currentPage;

        int leftPageIndex = book.currentPage - 1;
        int rightPageIndex = book.currentPage;

        if (debugPageIndexes)
        {
            Debug.Log("Book currentPage = " + book.currentPage +
                      " | Left page index = " + leftPageIndex +
                      " | Right page index = " + rightPageIndex);
        }

        StopRevealAnimations();

        RefreshSide(leftOverlay, leftPageIndex);
        RefreshSide(rightOverlay, rightPageIndex);

        ForceOverlayOnTop();

        if (animateReveal && animateOverlayReveal)
        {
            AnimateVisibleSide(leftOverlay);
            AnimateVisibleSide(rightOverlay);
        }
    }

    private void RefreshSide(BookOverlaySide side, int pageIndex)
    {
        if (side == null)
            return;

        BookPageOverlayEntry entry = GetEntryForPage(pageIndex);

        if (entry == null)
        {
            HideSide(side);
            return;
        }

        if (side.root != null)
        {
            side.root.SetActive(true);
        }

        if (entry.entryType == BookOverlayEntryType.HistoryText)
        {
            ApplyHistoryEntry(side, entry);
        }
        else if (entry.entryType == BookOverlayEntryType.PerformanceImage)
        {
            ApplyPerformanceEntry(side, entry);
        }
    }

    private void ApplyHistoryEntry(BookOverlaySide side, BookPageOverlayEntry entry)
    {
        if (side.historyTextGroup != null)
        {
            side.historyTextGroup.SetActive(true);
        }

        if (side.performanceImageGroup != null)
        {
            side.performanceImageGroup.SetActive(false);
        }

        if (side.performanceImage != null)
        {
            side.performanceImage.enabled = false;
        }

        LevelHistoryData historyData = GetHistoryDataForBook(entry.changedAfterLevel);

        bool levelCompleted = historyData != null && historyData.completed;

        if (levelCompleted)
        {
            if (side.inventedInValueText != null)
            {
                side.inventedInValueText.text = entry.changedInventedIn;
            }

            if (side.byValueText != null)
            {
                side.byValueText.text = entry.usePlayerNamesForChangedBy
                    ? historyData.playerNames
                    : entry.originalBy;
            }
        }
        else
        {
            if (side.inventedInValueText != null)
            {
                side.inventedInValueText.text = entry.originalInventedIn;
            }

            if (side.byValueText != null)
            {
                side.byValueText.text = entry.originalBy;
            }
        }

        ResetTMPVisibility(side.inventedInValueText);
        ResetTMPVisibility(side.byValueText);
    }

    private void ApplyPerformanceEntry(BookOverlaySide side, BookPageOverlayEntry entry)
    {
        if (side.historyTextGroup != null)
        {
            side.historyTextGroup.SetActive(false);
        }

        if (side.performanceImageGroup != null)
        {
            side.performanceImageGroup.SetActive(true);
        }

        LevelHistoryData historyData = GetHistoryDataForBook(entry.performanceLevelNumber);

        if (historyData == null || !historyData.completed)
        {
            HideSide(side);
            return;
        }

        if (side.performanceImage == null)
            return;

        LevelPerformanceSpriteSet spriteSet = GetSpriteSetForLevel(entry.performanceLevelNumber);

        if (spriteSet == null)
        {
            Debug.LogWarning("HistoricalBookUI: No performance sprite set found for level " + entry.performanceLevelNumber);
            side.performanceImage.sprite = null;
            side.performanceImage.enabled = false;
            return;
        }

        switch (historyData.outcome)
        {
            case HistoryPerformanceOutcome.Dead:
                side.performanceImage.sprite = spriteSet.deadCustomersSprite;
                break;

            case HistoryPerformanceOutcome.Happy:
                side.performanceImage.sprite = spriteSet.happyCustomersSprite;
                break;

            case HistoryPerformanceOutcome.Angry:
                side.performanceImage.sprite = spriteSet.angryCustomersSprite;
                break;

            default:
                side.performanceImage.sprite = null;
                break;
        }

        side.performanceImage.enabled = side.performanceImage.sprite != null;
    }

    private LevelHistoryData GetHistoryDataForBook(int levelNumber)
    {
        if (debugForceBookComplete)
        {
            return CreateDebugCompletedHistoryData(levelNumber);
        }

        if (OurGameManager.Instance != null)
        {
            return OurGameManager.Instance.GetLevelHistoryData(levelNumber);
        }

        return null;
    }

    private LevelHistoryData CreateDebugCompletedHistoryData(int levelNumber)
    {
        string playerNames = GetDebugPlayerNames();

        if (levelNumber == 1)
        {
            return new LevelHistoryData
            {
                completed = true,
                totalServed = 5,
                correctServed = 0,
                deathServed = 5,
                playerNames = playerNames,
                outcome = HistoryPerformanceOutcome.Dead
            };
        }

        if (levelNumber == 2)
        {
            return new LevelHistoryData
            {
                completed = true,
                totalServed = 10,
                correctServed = 10,
                deathServed = 0,
                playerNames = playerNames,
                outcome = HistoryPerformanceOutcome.Happy
            };
        }

        return new LevelHistoryData
        {
            completed = false,
            totalServed = 0,
            correctServed = 0,
            deathServed = 0,
            playerNames = playerNames,
            outcome = HistoryPerformanceOutcome.None
        };
    }

    private string GetDebugPlayerNames()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.PlayerList != null && PhotonNetwork.PlayerList.Length > 0)
        {
            List<string> names = new List<string>();

            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player == null)
                    continue;

                if (!string.IsNullOrEmpty(player.NickName))
                {
                    names.Add(player.NickName);
                }
                else
                {
                    names.Add("Player " + player.ActorNumber);
                }
            }

            if (names.Count > 0)
            {
                return string.Join(" & ", names);
            }
        }

        return debugFallbackPlayerNames;
    }

    private void AnimateVisibleSide(BookOverlaySide side)
    {
        if (side == null || side.root == null)
            return;

        if (!side.root.activeInHierarchy)
            return;

        Coroutine routine = StartCoroutine(AnimateSideRoutine(side));
        runningRevealCoroutines.Add(routine);
    }

    private IEnumerator AnimateSideRoutine(BookOverlaySide side)
    {
        if (side == null || side.root == null)
            yield break;

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(side.root);
        Transform rootTransform = side.root.transform;

        Vector3 originalScale = GetOriginalScale(rootTransform);

        canvasGroup.alpha = 0f;
        rootTransform.localScale = originalScale * revealStartScale;

        if (useTypewriterText && side.historyTextGroup != null && side.historyTextGroup.activeInHierarchy)
        {
            PrepareTypewriterText(side.inventedInValueText);
            PrepareTypewriterText(side.byValueText);
        }

        float timer = 0f;
        float duration = Mathf.Max(0.01f, revealDuration);
        float typeDuration = Mathf.Max(0.01f, typewriterDuration);

        int inventedTotalChars = GetTMPCharacterCount(side.inventedInValueText);
        int byTotalChars = GetTMPCharacterCount(side.byValueText);

        while (timer < Mathf.Max(duration, typeDuration))
        {
            timer += Time.deltaTime;

            float revealT = Mathf.Clamp01(timer / duration);
            revealT = SmoothStep(revealT);

            canvasGroup.alpha = revealT;
            rootTransform.localScale = Vector3.Lerp(originalScale * revealStartScale, originalScale, revealT);

            if (useTypewriterText && side.historyTextGroup != null && side.historyTextGroup.activeInHierarchy)
            {
                float typeT = Mathf.Clamp01(timer / typeDuration);

                if (side.inventedInValueText != null)
                {
                    side.inventedInValueText.maxVisibleCharacters = Mathf.RoundToInt(inventedTotalChars * typeT);
                }

                if (side.byValueText != null)
                {
                    side.byValueText.maxVisibleCharacters = Mathf.RoundToInt(byTotalChars * typeT);
                }
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        rootTransform.localScale = originalScale;

        ShowAllTMPText(side.inventedInValueText);
        ShowAllTMPText(side.byValueText);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        return canvasGroup;
    }

    private Vector3 GetOriginalScale(Transform target)
    {
        if (target == null)
            return Vector3.one;

        if (!originalScales.ContainsKey(target))
        {
            originalScales.Add(target, target.localScale);
        }

        return originalScales[target];
    }

    private void PrepareTypewriterText(TMP_Text text)
    {
        if (text == null)
            return;

        text.ForceMeshUpdate();
        text.maxVisibleCharacters = 0;
    }

    private void ResetTMPVisibility(TMP_Text text)
    {
        if (text == null)
            return;

        text.maxVisibleCharacters = 99999;
    }

    private void ShowAllTMPText(TMP_Text text)
    {
        if (text == null)
            return;

        text.maxVisibleCharacters = 99999;
    }

    private int GetTMPCharacterCount(TMP_Text text)
    {
        if (text == null)
            return 0;

        text.ForceMeshUpdate();
        return text.textInfo.characterCount;
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private BookPageOverlayEntry GetEntryForPage(int pageIndex)
    {
        foreach (BookPageOverlayEntry entry in pageEntries)
        {
            if (entry != null && entry.pageIndex == pageIndex)
            {
                return entry;
            }
        }

        return null;
    }

    private LevelPerformanceSpriteSet GetSpriteSetForLevel(int levelNumber)
    {
        foreach (LevelPerformanceSpriteSet set in performanceSpriteSets)
        {
            if (set != null && set.levelNumber == levelNumber)
            {
                return set;
            }
        }

        return null;
    }

    private void HideSide(BookOverlaySide side)
    {
        if (side == null)
            return;

        if (side.root != null)
        {
            side.root.SetActive(false);
        }

        if (side.historyTextGroup != null)
        {
            side.historyTextGroup.SetActive(false);
        }

        if (side.performanceImageGroup != null)
        {
            side.performanceImageGroup.SetActive(false);
        }

        if (side.performanceImage != null)
        {
            side.performanceImage.sprite = null;
            side.performanceImage.enabled = false;
        }

        ResetTMPVisibility(side.inventedInValueText);
        ResetTMPVisibility(side.byValueText);
    }

    private void ForceOverlayOnTop()
    {
        if (!forceOverlayLayerOnTop)
            return;

        if (overlayLayer != null)
        {
            overlayLayer.SetAsLastSibling();
        }
    }

    private void StopRevealAnimations()
    {
        foreach (Coroutine routine in runningRevealCoroutines)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }

        runningRevealCoroutines.Clear();

        ResetTMPVisibility(leftOverlay.inventedInValueText);
        ResetTMPVisibility(leftOverlay.byValueText);
        ResetTMPVisibility(rightOverlay.inventedInValueText);
        ResetTMPVisibility(rightOverlay.byValueText);
    }
}