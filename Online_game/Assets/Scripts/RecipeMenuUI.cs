using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class FoodRecipe
{
    public string foodName;
    public Texture[] ingredientTextures = new Texture[6];
    public Texture finalFoodTexture;
}

public class RecipeMenuUI : MonoBehaviour
{
    [Header("Recipe Data")]
    public FoodRecipe[] recipes;

    [Header("UI References")]
    public TMP_Text foodNameText;
    public RawImage[] ingredientSlots = new RawImage[6];
    public RawImage finalFoodSlot;

    public Button nextButton;
    public Button previousButton;

    private int currentRecipeIndex = 0;

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextRecipe);

        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousRecipe);

        ShowRecipe(currentRecipeIndex);
    }

    private void ShowRecipe(int index)
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("No recipes assigned.");
            return;
        }

        FoodRecipe recipe = recipes[index];

        if (foodNameText != null)
            foodNameText.text = recipe.foodName;

        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (ingredientSlots[i] == null)
                continue;

            if (i < recipe.ingredientTextures.Length && recipe.ingredientTextures[i] != null)
            {
                SetRawImageTexture(ingredientSlots[i], recipe.ingredientTextures[i]);
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
                SetRawImageTexture(finalFoodSlot, recipe.finalFoodTexture);
            }
            else
            {
                finalFoodSlot.texture = null;
                finalFoodSlot.enabled = false;
            }
        }
    }

    private void SetRawImageTexture(RawImage rawImage, Texture texture)
    {
        rawImage.texture = texture;
        rawImage.enabled = true;

        RectTransform rectTransform = rawImage.rectTransform;

        float slotWidth = rectTransform.rect.width;
        float slotHeight = rectTransform.rect.height;

        float textureWidth = texture.width;
        float textureHeight = texture.height;

        float slotAspect = slotWidth / slotHeight;
        float textureAspect = textureWidth / textureHeight;

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

    private void ShowNextRecipe()
    {
        currentRecipeIndex++;

        if (currentRecipeIndex >= recipes.Length)
            currentRecipeIndex = 0;

        ShowRecipe(currentRecipeIndex);
    }

    private void ShowPreviousRecipe()
    {
        currentRecipeIndex--;

        if (currentRecipeIndex < 0)
            currentRecipeIndex = recipes.Length - 1;

        ShowRecipe(currentRecipeIndex);
    }
}