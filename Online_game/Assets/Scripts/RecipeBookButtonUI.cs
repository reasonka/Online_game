using UnityEngine;
using UnityEngine.UI;

public class RecipeBookButtonUI : MonoBehaviour
{
    [Header("Recipe Button")]
    public Button recipeButton;

    [Header("Current Level")]
    public int currentLevel = 1;

    [Header("Main Recipe Panels")]
    public GameObject level1RecipeUI;
    public GameObject level2RecipeUI;

    [Header("Recipe Category Controller")]
    public RecipeCategoryMenuUI recipeCategoryMenuUI;

    private void Start()
    {
        if (recipeButton != null)
            recipeButton.onClick.AddListener(OpenRecipeForCurrentLevel);

        CloseAllRecipeUI();
    }

    public void SetCurrentLevel(int level)
    {
        currentLevel = level;
    }

    public void OpenRecipeForCurrentLevel()
    {
        CloseAllRecipeUI();

        if (recipeCategoryMenuUI != null)
            recipeCategoryMenuUI.CloseAllRecipePanels();

        if (currentLevel == 1)
        {
            if (level1RecipeUI != null)
                level1RecipeUI.SetActive(true);

            if (level2RecipeUI != null)
                level2RecipeUI.SetActive(false);
        }
        else if (currentLevel == 2)
        {
            if (level1RecipeUI != null)
                level1RecipeUI.SetActive(false);

            if (level2RecipeUI != null)
                level2RecipeUI.SetActive(true);
        }
    }

    public void CloseAllRecipeUI()
    {
        if (level1RecipeUI != null)
            level1RecipeUI.SetActive(false);

        if (level2RecipeUI != null)
            level2RecipeUI.SetActive(false);
    }
}