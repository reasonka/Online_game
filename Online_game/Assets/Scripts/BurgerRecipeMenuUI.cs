using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class BurgerRecipe
{
    public string foodName;
    public Texture[] ingredientTextures = new Texture[6];
    public Texture finalFoodTexture;
}

public class BurgerRecipeMenuUI : MonoBehaviour
{
    [Header("Recipe Data")]
    public BurgerRecipe[] recipes;

    [Header("UI References")]
    public TMP_Text foodNameText;
    public RawImage[] ingredientSlots = new RawImage[6];
    public RawImage finalFoodSlot;

    [Header("Recipe Buttons")]
    public Button burgerButton1;
    public Button burgerButton2;
    public Button burgerButton3;
    public Button burgerButton4;

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
        if (burgerButton1 != null)
            burgerButton1.onClick.AddListener(() => ShowRecipe(0));

        if (burgerButton2 != null)
            burgerButton2.onClick.AddListener(() => ShowRecipe(1));

        if (burgerButton3 != null)
            burgerButton3.onClick.AddListener(() => ShowRecipe(2));

        if (burgerButton4 != null)
            burgerButton4.onClick.AddListener(() => ShowRecipe(3));

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseRecipePanel);

        if (recipePanel != null)
        recipePanel.SetActive(false);
    }

    public void ShowRecipe(int index)
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("No burger recipes assigned.");
            return;
        }

        if (index < 0 || index >= recipes.Length)
        {
            Debug.LogWarning("Burger recipe index is out of range: " + index);
            return;
        }

        if (recipePanel != null)
            recipePanel.SetActive(true);

        BurgerRecipe recipe = recipes[index];

        if (foodNameText != null)
            foodNameText.text = recipe.foodName;

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