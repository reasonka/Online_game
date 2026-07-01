using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class FoodRecipe
{
    public string foodName;
    public Sprite[] ingredientSprites = new Sprite[6];
    public Sprite finalFoodSprite;
}

public class RecipeMenuUI : MonoBehaviour
{
    [Header("Recipe Data")]
    public FoodRecipe[] recipes;

    [Header("UI References")]
    public TMP_Text foodNameText;
    public TMP_Text yummyText;

    public Image[] ingredientSlots = new Image[6];
    public Image finalFoodSlot;

    public Button nextButton;
    public Button previousButton;

    private int currentRecipeIndex = 0;

    private void Start()
    {
        yummyText.text = "YUMMY!";

        nextButton.onClick.AddListener(ShowNextRecipe);
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

        foodNameText.text = recipe.foodName;

        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (i < recipe.ingredientSprites.Length && recipe.ingredientSprites[i] != null)
            {
                ingredientSlots[i].sprite = recipe.ingredientSprites[i];
                ingredientSlots[i].enabled = true;
            }
            else
            {
                ingredientSlots[i].sprite = null;
                ingredientSlots[i].enabled = false;
            }
        }

        finalFoodSlot.sprite = recipe.finalFoodSprite;
        finalFoodSlot.enabled = recipe.finalFoodSprite != null;
    }

    private void ShowNextRecipe()
    {
        currentRecipeIndex++;

        if (currentRecipeIndex >= recipes.Length)
        {
            currentRecipeIndex = 0;
        }

        ShowRecipe(currentRecipeIndex);
    }

    private void ShowPreviousRecipe()
    {
        currentRecipeIndex--;

        if (currentRecipeIndex < 0)
        {
            currentRecipeIndex = recipes.Length - 1;
        }

        ShowRecipe(currentRecipeIndex);
    }
}