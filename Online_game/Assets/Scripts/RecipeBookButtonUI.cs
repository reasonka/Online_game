using UnityEngine;
using UnityEngine.UI;

public class RecipeBookButtonUI : MonoBehaviour
{
    [Header("Current Level")]
    public int currentLevel = 1;

    [Header("Recipe UI")]
    public GameObject level1RecipeUI;
    public GameObject level2RecipeUI;

    [Header("Button")]
    public Button recipeButton;

    private void Start()
    {
        if (recipeButton == null)
            recipeButton = GetComponent<Button>();

        if (recipeButton != null)
            recipeButton.onClick.AddListener(OpenRecipeUI);

        CloseRecipeUI();
    }

    public void OpenRecipeUI()
    {
        CloseRecipeUI();

        if (currentLevel == 1)
        {
            if (level1RecipeUI != null)
                level1RecipeUI.SetActive(true);
        }
        else if (currentLevel == 2)
        {
            if (level2RecipeUI != null)
                level2RecipeUI.SetActive(true);
        }
    }

    public void CloseRecipeUI()
    {
        if (level1RecipeUI != null)
            level1RecipeUI.SetActive(false);

        if (level2RecipeUI != null)
            level2RecipeUI.SetActive(false);
    }
}