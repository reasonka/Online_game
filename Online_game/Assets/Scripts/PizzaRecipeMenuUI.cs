using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PizzaRecipe
{
    public string foodName;
    public Texture[] ingredientTextures = new Texture[7];
    public Texture finalFoodTexture;
}

public class PizzaRecipeMenuUI : MonoBehaviour
{
    [Header("Recipe Data")]
    public PizzaRecipe[] recipes;

    [Header("UI References")]
    public TMP_Text foodNameText;
    public RawImage[] ingredientSlots = new RawImage[7];
    public RawImage finalFoodSlot;

    [Header("Recipe Buttons")]
    public Button pizzaButton1;
    public Button pizzaButton2;
    public Button pizzaButton3;

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
        if (pizzaButton1 != null)
            pizzaButton1.onClick.AddListener(() => ShowRecipe(0));

        if (pizzaButton2 != null)
            pizzaButton2.onClick.AddListener(() => ShowRecipe(1));

        if (pizzaButton3 != null)
            pizzaButton3.onClick.AddListener(() => ShowRecipe(2));

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseRecipePanel);

        ShowRecipe(0);
    }

    public void ShowRecipe(int index)
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("No pizza recipes assigned.");
            return;
        }

        if (index < 0 || index >= recipes.Length)
        {
            Debug.LogWarning("Pizza recipe index is out of range: " + index);
            return;
        }

        if (recipePanel != null)
            recipePanel.SetActive(true);

        PizzaRecipe recipe = recipes[index];

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