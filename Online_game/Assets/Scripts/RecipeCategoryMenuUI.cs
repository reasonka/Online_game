using UnityEngine;
using UnityEngine.UI;

public class RecipeCategoryMenuUI : MonoBehaviour
{
    [Header("Level 1 Category Buttons")]
    public Button pizzaButton;
    public Button hotdogButton;
    public Button beerButton;

    [Header("Level 2 Category Buttons")]
    public Button burgerButton;
    public Button pancakeButton;
    public Button cocktailButton;

    [Header("Recipe Scripts")]
    public PizzaRecipeMenuUI pizzaRecipeUI;
    public HotdogRecipeMenuUI hotdogRecipeUI;
    public BurgerRecipeMenuUI burgerRecipeUI;
    public PancakeRecipeMenuUI pancakeRecipeUI;

    [Header("Main Level Panels")]
    public GameObject level1MainRecipeUI;
    public GameObject level2MainRecipeUI;

    [Header("Recipe Panels")]
    public GameObject pizzaPanel;
    public GameObject hotdogPanel;
    public GameObject beerPanel;
    public GameObject burgerPanel;
    public GameObject pancakePanel;
    public GameObject cocktailPanel;

    [Header("Recipe Close Buttons")]
    public Button pizzaCloseButton;
    public Button hotdogCloseButton;
    public Button beerCloseButton;
    public Button burgerCloseButton;
    public Button pancakeCloseButton;
    public Button cocktailCloseButton;

    private void Start()
    {
        if (pizzaButton != null)
            pizzaButton.onClick.AddListener(OpenPizzaPanel);

        if (hotdogButton != null)
            hotdogButton.onClick.AddListener(OpenHotdogPanel);

        if (beerButton != null)
            beerButton.onClick.AddListener(OpenBeerPanel);

        if (burgerButton != null)
            burgerButton.onClick.AddListener(OpenBurgerPanel);

        if (pancakeButton != null)
            pancakeButton.onClick.AddListener(OpenPancakePanel);

        if (cocktailButton != null)
            cocktailButton.onClick.AddListener(OpenCocktailPanel);

        if (pizzaCloseButton != null)
            pizzaCloseButton.onClick.AddListener(ReturnToLevel1Panel);

        if (hotdogCloseButton != null)
            hotdogCloseButton.onClick.AddListener(ReturnToLevel1Panel);

        if (beerCloseButton != null)
            beerCloseButton.onClick.AddListener(ReturnToLevel1Panel);

        if (burgerCloseButton != null)
            burgerCloseButton.onClick.AddListener(ReturnToLevel2Panel);

        if (pancakeCloseButton != null)
            pancakeCloseButton.onClick.AddListener(ReturnToLevel2Panel);

        if (cocktailCloseButton != null)
            cocktailCloseButton.onClick.AddListener(ReturnToLevel2Panel);

        CloseAllRecipePanels();
    }

    public void OpenPizzaPanel()
    {
        OpenLevel1RecipePanel();

        if (pizzaRecipeUI != null)
            pizzaRecipeUI.ShowRecipe(0);
        else if (pizzaPanel != null)
            pizzaPanel.SetActive(true);
    }

    public void OpenHotdogPanel()
    {
        OpenLevel1RecipePanel();

        if (hotdogRecipeUI != null)
            hotdogRecipeUI.ShowRecipe(0);
        else if (hotdogPanel != null)
            hotdogPanel.SetActive(true);
    }

    public void OpenBeerPanel()
    {
        OpenLevel1RecipePanel();

        if (beerPanel != null)
            beerPanel.SetActive(true);
    }

    public void OpenBurgerPanel()
    {
        OpenLevel2RecipePanel();

        if (burgerRecipeUI != null)
            burgerRecipeUI.ShowRecipe(0);
        else if (burgerPanel != null)
            burgerPanel.SetActive(true);
    }

    public void OpenPancakePanel()
    {
        OpenLevel2RecipePanel();

        if (pancakeRecipeUI != null)
            pancakeRecipeUI.ShowRecipe(0);
        else if (pancakePanel != null)
            pancakePanel.SetActive(true);
    }

    public void OpenCocktailPanel()
    {
        OpenLevel2RecipePanel();

        if (cocktailPanel != null)
            cocktailPanel.SetActive(true);
    }

    private void OpenLevel1RecipePanel()
    {
        CloseAllRecipePanels();

        if (level1MainRecipeUI != null)
            level1MainRecipeUI.SetActive(false);

        if (level2MainRecipeUI != null)
            level2MainRecipeUI.SetActive(false);
    }

    private void OpenLevel2RecipePanel()
    {
        CloseAllRecipePanels();

        if (level1MainRecipeUI != null)
            level1MainRecipeUI.SetActive(false);

        if (level2MainRecipeUI != null)
            level2MainRecipeUI.SetActive(false);
    }

    public void ReturnToLevel1Panel()
    {
        CloseAllRecipePanels();

        if (level1MainRecipeUI != null)
            level1MainRecipeUI.SetActive(true);

        if (level2MainRecipeUI != null)
            level2MainRecipeUI.SetActive(false);
    }

    public void ReturnToLevel2Panel()
    {
        CloseAllRecipePanels();

        if (level1MainRecipeUI != null)
            level1MainRecipeUI.SetActive(false);

        if (level2MainRecipeUI != null)
            level2MainRecipeUI.SetActive(true);
    }

    public void CloseAllRecipePanels()
    {
        if (pizzaPanel != null)
            pizzaPanel.SetActive(false);

        if (hotdogPanel != null)
            hotdogPanel.SetActive(false);

        if (beerPanel != null)
            beerPanel.SetActive(false);

        if (burgerPanel != null)
            burgerPanel.SetActive(false);

        if (pancakePanel != null)
            pancakePanel.SetActive(false);

        if (cocktailPanel != null)
            cocktailPanel.SetActive(false);
    }
}