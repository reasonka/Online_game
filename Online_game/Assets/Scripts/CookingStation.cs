using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[System.Serializable]
public class CookingRecipe
{
    [Header("Recipe Name")]
    public string recipeName;

    [Header("Ingredients Required")]
    public List<GameObject> ingredientPrefabs = new List<GameObject>();

    [Header("Final Product")]
    public GameObject finalProductPrefab;

    [Header("Order Requirement")]
    public bool useOrder = false;
}

public class CookingStation : MonoBehaviourPun
{
    [Header("Debug")]
    public bool showDebugLog = true;

    [Header("Photon Setting")]
    [Tooltip("Turn this off for local testing without Photon sync.")]
    public bool usePhotonSync = false;

    [Header("Recipe List")]
    public List<CookingRecipe> recipes = new List<CookingRecipe>();

    [Header("Spawn Point")]
    public Transform productSpawnPoint;

    [Header("Destroy Ingredient After Adding")]
    public bool destroyIngredientAfterAdding = true;

    private List<string> currentIngredients = new List<string>();

    private void Start()
    {
        Log("CookingStation started.");
        Log("GameObject: " + gameObject.name);
        Log("Use Photon Sync: " + usePhotonSync);
        Log("Photon Connected: " + PhotonNetwork.IsConnected);
        Log("Is MasterClient: " + PhotonNetwork.IsMasterClient);
        Log("Recipe Count: " + recipes.Count);

        Collider stationCollider = GetComponent<Collider>();
        if (stationCollider == null)
        {
            LogWarning("CookingStation has NO Collider. OnTriggerEnter will not work.");
        }
        else
        {
            Log("CookingStation Collider found: " + stationCollider.GetType().Name);
            Log("CookingStation Collider Is Trigger: " + stationCollider.isTrigger);

            if (!stationCollider.isTrigger)
            {
                LogWarning("CookingStation Collider is NOT trigger. Please enable Is Trigger.");
            }
        }

        if (productSpawnPoint == null)
        {
            LogWarning("Product Spawn Point is empty. Product will spawn above CookingStation.");
        }
        else
        {
            Log("Product Spawn Point: " + productSpawnPoint.name);
        }

        CheckRecipeSetup();
    }

    private void CheckRecipeSetup()
    {
        Log("Checking recipe setup...");

        for (int i = 0; i < recipes.Count; i++)
        {
            CookingRecipe recipe = recipes[i];

            if (recipe == null)
            {
                LogWarning("Recipe index " + i + " is null.");
                continue;
            }

            Log("Recipe [" + i + "]: " + recipe.recipeName);
            Log("Use Order: " + recipe.useOrder);

            if (recipe.ingredientPrefabs == null || recipe.ingredientPrefabs.Count == 0)
            {
                LogWarning("Recipe [" + recipe.recipeName + "] has NO ingredients.");
            }
            else
            {
                Log("Ingredient Count: " + recipe.ingredientPrefabs.Count);

                for (int j = 0; j < recipe.ingredientPrefabs.Count; j++)
                {
                    GameObject prefab = recipe.ingredientPrefabs[j];

                    if (prefab == null)
                    {
                        LogWarning("Recipe [" + recipe.recipeName + "] ingredient index " + j + " is empty.");
                        continue;
                    }

                    IngredientId id = prefab.GetComponent<IngredientId>();

                    if (id == null)
                    {
                        LogWarning("Ingredient prefab [" + prefab.name + "] has NO IngredientId script.");
                    }
                    else
                    {
                        Log("Ingredient [" + j + "]: " + prefab.name + " / ID = " + id.ingredientId);

                        if (string.IsNullOrEmpty(id.ingredientId))
                        {
                            LogWarning("Ingredient prefab [" + prefab.name + "] has EMPTY ingredientId.");
                        }
                    }
                }
            }

            if (recipe.finalProductPrefab == null)
            {
                LogWarning("Recipe [" + recipe.recipeName + "] has NO final product prefab.");
            }
            else
            {
                Log("Final Product: " + recipe.finalProductPrefab.name);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Log("OnTriggerEnter detected object: " + other.gameObject.name);

        IngredientId ingredient = other.GetComponent<IngredientId>();

        if (ingredient == null)
        {
            LogWarning("Entered object [" + other.gameObject.name + "] has NO IngredientId. Ignored.");
            return;
        }

        Log("Ingredient detected: " + other.gameObject.name);
        Log("Ingredient ID: " + ingredient.ingredientId);

        if (string.IsNullOrEmpty(ingredient.ingredientId))
        {
            LogWarning("Ingredient ID is empty. Ignored.");
            return;
        }

        AddIngredient(ingredient.ingredientId);

        if (destroyIngredientAfterAdding)
        {
            DestroyIngredientObject(other.gameObject);
        }
    }

    private void DestroyIngredientObject(GameObject ingredientObject)
    {
        Log("Trying to destroy ingredient object: " + ingredientObject.name);

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            PhotonView targetPhotonView = ingredientObject.GetComponent<PhotonView>();

            if (targetPhotonView == null)
            {
                LogWarning("Ingredient has no PhotonView. Cannot PhotonNetwork.Destroy. Using normal Destroy instead.");
                Destroy(ingredientObject);
                return;
            }

            if (targetPhotonView.IsMine)
            {
                Log("Photon destroying ingredient: " + ingredientObject.name);
                PhotonNetwork.Destroy(ingredientObject);
            }
            else
            {
                LogWarning("Ingredient PhotonView is not mine. Cannot destroy from this client.");
            }
        }
        else
        {
            Log("Local destroying ingredient: " + ingredientObject.name);
            Destroy(ingredientObject);
        }
    }

    public void AddIngredient(string ingredientId)
    {
        Log("AddIngredient called with ID: " + ingredientId);

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            Log("Photon mode active. Sending RPC_AddIngredient to all clients.");
            photonView.RPC(nameof(RPC_AddIngredient), RpcTarget.AllBuffered, ingredientId);
        }
        else
        {
            Log("Local mode active. Adding ingredient locally.");
            LocalAddIngredient(ingredientId);
        }
    }

    [PunRPC]
    private void RPC_AddIngredient(string ingredientId)
    {
        Log("RPC_AddIngredient received: " + ingredientId);
        LocalAddIngredient(ingredientId);
    }

    private void LocalAddIngredient(string ingredientId)
    {
        currentIngredients.Add(ingredientId);

        Log("Ingredient added to current list: " + ingredientId);
        LogCurrentIngredients();

        CheckRecipes();
    }

    private void CheckRecipes()
    {
        Log("Checking recipes...");

        if (recipes == null || recipes.Count == 0)
        {
            LogWarning("No recipes in CookingStation.");
            return;
        }

        foreach (CookingRecipe recipe in recipes)
        {
            if (recipe == null)
            {
                LogWarning("Found null recipe. Skipping.");
                continue;
            }

            Log("Checking recipe: " + recipe.recipeName);

            if (recipe.ingredientPrefabs == null || recipe.ingredientPrefabs.Count == 0)
            {
                LogWarning("Recipe [" + recipe.recipeName + "] has no ingredients. Skipping.");
                continue;
            }

            if (recipe.finalProductPrefab == null)
            {
                LogWarning("Recipe [" + recipe.recipeName + "] has no final product prefab. Skipping.");
                continue;
            }

            List<string> requiredIds = GetRecipeIngredientIds(recipe);

            Log("Required ingredients for [" + recipe.recipeName + "]: " + string.Join(", ", requiredIds));
            Log("Current ingredients: " + string.Join(", ", currentIngredients));
            Log("Use Order: " + recipe.useOrder);

            bool matched;

            if (recipe.useOrder)
            {
                matched = CheckOrderedRecipe(requiredIds);
            }
            else
            {
                matched = CheckUnorderedRecipe(requiredIds);
            }

            Log("Recipe [" + recipe.recipeName + "] matched: " + matched);

            if (matched)
            {
                Log("RECIPE COMPLETED: " + recipe.recipeName);

                CreateFinalProduct(recipe);
                ClearIngredients();

                return;
            }
        }

        Log("No recipe matched yet.");
    }

    private List<string> GetRecipeIngredientIds(CookingRecipe recipe)
    {
        List<string> ids = new List<string>();

        foreach (GameObject prefab in recipe.ingredientPrefabs)
        {
            if (prefab == null)
            {
                LogWarning("A required ingredient prefab is empty in recipe: " + recipe.recipeName);
                continue;
            }

            IngredientId ingredientId = prefab.GetComponent<IngredientId>();

            if (ingredientId == null)
            {
                LogWarning("Prefab missing IngredientId: " + prefab.name);
                continue;
            }

            ids.Add(ingredientId.ingredientId);
        }

        return ids;
    }

    private bool CheckOrderedRecipe(List<string> requiredIds)
    {
        Log("Checking ordered recipe...");

        if (currentIngredients.Count < requiredIds.Count)
        {
            Log("Not enough ingredients. Current: " + currentIngredients.Count + ", Required: " + requiredIds.Count);
            return false;
        }

        int startIndex = currentIngredients.Count - requiredIds.Count;

        for (int i = 0; i < requiredIds.Count; i++)
        {
            string current = currentIngredients[startIndex + i];
            string required = requiredIds[i];

            Log("Order check index " + i + ": current = " + current + ", required = " + required);

            if (current != required)
            {
                Log("Ordered recipe failed at index " + i);
                return false;
            }
        }

        Log("Ordered recipe success.");
        return true;
    }

    private bool CheckUnorderedRecipe(List<string> requiredIds)
    {
        Log("Checking unordered recipe...");

        if (currentIngredients.Count < requiredIds.Count)
        {
            Log("Not enough ingredients. Current: " + currentIngredients.Count + ", Required: " + requiredIds.Count);
            return false;
        }

        List<string> tempCurrent = new List<string>(currentIngredients);

        foreach (string requiredId in requiredIds)
        {
            Log("Looking for required ingredient: " + requiredId);

            if (!tempCurrent.Contains(requiredId))
            {
                Log("Missing required ingredient: " + requiredId);
                return false;
            }

            tempCurrent.Remove(requiredId);
            Log("Found and removed required ingredient: " + requiredId);
        }

        Log("Unordered recipe success.");
        return true;
    }

    private void CreateFinalProduct(CookingRecipe recipe)
    {
        Log("CreateFinalProduct called for recipe: " + recipe.recipeName);

        Vector3 spawnPosition = productSpawnPoint != null
            ? productSpawnPoint.position
            : transform.position + Vector3.up;

        Quaternion spawnRotation = productSpawnPoint != null
            ? productSpawnPoint.rotation
            : Quaternion.identity;

        Log("Spawn Position: " + spawnPosition);
        Log("Spawn Rotation: " + spawnRotation.eulerAngles);

        if (recipe.finalProductPrefab == null)
        {
            LogWarning("Cannot create final product because finalProductPrefab is null.");
            return;
        }

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            Log("Photon spawn mode.");

            if (PhotonNetwork.IsMasterClient)
            {
                Log("This client is MasterClient. PhotonNetwork.Instantiate: " + recipe.finalProductPrefab.name);
                PhotonNetwork.Instantiate(recipe.finalProductPrefab.name, spawnPosition, spawnRotation);
            }
            else
            {
                Log("This client is NOT MasterClient. Product will not be spawned by this client.");
            }
        }
        else
        {
            Log("Local spawn mode. Instantiate: " + recipe.finalProductPrefab.name);
            Instantiate(recipe.finalProductPrefab, spawnPosition, spawnRotation);
        }
    }

    private void ClearIngredients()
    {
        Log("ClearIngredients called.");

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            Log("Clearing ingredients by RPC.");
            photonView.RPC(nameof(RPC_ClearIngredients), RpcTarget.AllBuffered);
        }
        else
        {
            Log("Clearing ingredients locally.");
            currentIngredients.Clear();
            LogCurrentIngredients();
        }
    }

    [PunRPC]
    private void RPC_ClearIngredients()
    {
        Log("RPC_ClearIngredients received.");
        currentIngredients.Clear();
        LogCurrentIngredients();
    }

    public void ResetCookingStation()
    {
        Log("ResetCookingStation called.");
        ClearIngredients();
    }

    private void LogCurrentIngredients()
    {
        if (currentIngredients.Count == 0)
        {
            Log("Current ingredients list is empty.");
        }
        else
        {
            Log("Current ingredients list: " + string.Join(", ", currentIngredients));
        }
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log("[CookingStation] " + message, this);
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning("[CookingStation] " + message, this);
        }
    }
}