using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PancakeRecipe
{
    public string foodName;
    public Texture[] ingredientTextures = new Texture[4];
    public Texture finalFoodTexture;

    [Header("Pancake Slice Amount")]
    public int pancakeSliceAmount = 3;
}

public class PancakeRecipeMenuUI : MonoBehaviour
{
    [Header("Recipe Data")]
    public PancakeRecipe[] recipes;

    [Header("UI References")]
    public TMP_Text foodNameText;
    public RawImage[] ingredientSlots = new RawImage[4];
    public RawImage finalFoodSlot;

    [Header("Pancake Amount UI")]
    public RawImage x3Image;
    public RawImage x4Image;

    [Header("Recipe Buttons")]
    public Button pancakeButton1;
    public Button pancakeButton2;
    public Button pancakeButton3;
    public Button pancakeButton4;

    [Header("Close Button")]
    public Button closeButton;
    public GameObject recipePanel;

    private Vector2[] originalIngredientSizes;
    private Vector2 originalFinalFoodSize;

    private void Awake()
    {
        originalIngredientSizes = new Vector2[ingredientSlots.Length];

        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (ingredientSlots[i] != null)
                originalIngredientSizes[i] = ingredientSlots[i].rectTransform.sizeDelta;
        }

        if (finalFoodSlot != null)
            originalFinalFoodSize = finalFoodSlot.rectTransform.sizeDelta;
    }

    private void Start()
    {
        if (pancakeButton1 != null)
            pancakeButton1.onClick.AddListener(() => ShowRecipe(0));

        if (pancakeButton2 != null)
            pancakeButton2.onClick.AddListener(() => ShowRecipe(1));

        if (pancakeButton3 != null)
            pancakeButton3.onClick.AddListener(() => ShowRecipe(2));

        if (pancakeButton4 != null)
            pancakeButton4.onClick.AddListener(() => ShowRecipe(3));

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseRecipePanel);

        if (recipePanel != null)
        recipePanel.SetActive(false);
    }

    public void ShowRecipe(int index)
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("No pancake recipes assigned.");
            return;
        }

        if (index < 0 || index >= recipes.Length)
        {
            Debug.LogWarning("Pancake recipe index is out of range: " + index);
            return;
        }

        if (recipePanel != null)
            recipePanel.SetActive(true);

        PancakeRecipe recipe = recipes[index];

        if (foodNameText != null)
            foodNameText.text = recipe.foodName;

        SetPancakeAmountUI(recipe.pancakeSliceAmount);

        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (ingredientSlots[i] == null)
                continue;

            if (i < recipe.ingredientTextures.Length && recipe.ingredientTextures[i] != null)
            {
                SetRawImageTexture(ingredientSlots[i], recipe.ingredientTextures[i], originalIngredientSizes[i]);
            }
            else
            {
                ingredientSlots[i].texture = null;
                ingredientSlots[i].enabled = false;
            }
        }

        if (finalFoodSlot != null)
        {
            if (recipe.finalFoodTexture != null)
            {
                SetRawImageTexture(finalFoodSlot, recipe.finalFoodTexture, originalFinalFoodSize);
            }
            else
            {
                finalFoodSlot.texture = null;
                finalFoodSlot.enabled = false;
            }
        }
    }

    private void SetPancakeAmountUI(int amount)
    {
        if (x3Image != null)
            x3Image.gameObject.SetActive(amount == 3);

        if (x4Image != null)
            x4Image.gameObject.SetActive(amount == 4);
    }

    private void SetRawImageTexture(RawImage rawImage, Texture texture, Vector2 originalSlotSize)
    {
        rawImage.texture = texture;
        rawImage.enabled = true;

        float slotWidth = originalSlotSize.x;
        float slotHeight = originalSlotSize.y;

        float textureAspect = (float)texture.width / texture.height;
        float slotAspect = slotWidth / slotHeight;

        RectTransform rectTransform = rawImage.rectTransform;

        if (textureAspect > slotAspect)
        {
            float height = slotWidth / textureAspect;
            rectTransform.sizeDelta = new Vector2(slotWidth, height);
        }
        else
        {
            float width = slotHeight * textureAspect;
            rectTransform.sizeDelta = new Vector2(width, slotHeight);
        }
    }

    public void CloseRecipePanel()
    {
        if (recipePanel != null)
            recipePanel.SetActive(false);
    }
}