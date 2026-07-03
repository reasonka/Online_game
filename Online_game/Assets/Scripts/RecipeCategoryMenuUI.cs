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

    [Header("Level 1 Recipe Panels")]
    public GameObject pizzaPanel;
    public GameObject hotdogPanel;
    public GameObject beerPanel;

    [Header("Level 2 Recipe Panels")]
    public GameObject burgerPanel;
    public GameObject pancakePanel;
    public GameObject cocktailPanel;

    [Header("Optional Main UI Panels")]
    public GameObject level1MainRecipeUI;
    public GameObject level2MainRecipeUI;

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

        CloseAllRecipePanels();
    }

    public void OpenPizzaPanel()
    {
        CloseAllRecipePanels();

        if (pizzaPanel != null)
            pizzaPanel.SetActive(true);
    }

    public void OpenHotdogPanel()
    {
        CloseAllRecipePanels();

        if (hotdogPanel != null)
            hotdogPanel.SetActive(true);
    }

    public void OpenBeerPanel()
    {
        CloseAllRecipePanels();

        if (beerPanel != null)
            beerPanel.SetActive(true);
    }

    public void OpenBurgerPanel()
    {
        CloseAllRecipePanels();

        if (burgerPanel != null)
            burgerPanel.SetActive(true);
    }

    public void OpenPancakePanel()
    {
        CloseAllRecipePanels();

        if (pancakePanel != null)
            pancakePanel.SetActive(true);
    }

    public void OpenCocktailPanel()
    {
        CloseAllRecipePanels();

        if (cocktailPanel != null)
            cocktailPanel.SetActive(true);
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

    public void OpenLevel1MainRecipeUI()
    {
        CloseAllRecipePanels();

        if (level1MainRecipeUI != null)
            level1MainRecipeUI.SetActive(true);

        if (level2MainRecipeUI != null)
            level2MainRecipeUI.SetActive(false);
    }

    public void OpenLevel2MainRecipeUI()
    {
        CloseAllRecipePanels();

        if (level1MainRecipeUI != null)
            level1MainRecipeUI.SetActive(false);

        if (level2MainRecipeUI != null)
            level2MainRecipeUI.SetActive(true);
    }
}