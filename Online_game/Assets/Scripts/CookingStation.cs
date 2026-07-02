using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[System.Serializable]
public class IngredientRequirement
{
    [Header("Input Ingredient Prefab")]
    public GameObject ingredientPrefab;

    [Header("Visual Prefab On Dish")]
    public GameObject visualPrefabOnDish;

    [Header("Spawn Points For This Ingredient")]
    public List<Transform> visualSpawnPoints = new List<Transform>();
}

[System.Serializable]
public class CookingRecipe
{
    [Header("Recipe Name")]
    public string recipeName;

    [Header("Order Requirement")]
    [Tooltip("If enabled, ingredients must be added in the exact Inspector order.")]
    public bool useOrder = false;

    [Header("Required Ingredients")]
    public List<IngredientRequirement> requiredIngredients =
        new List<IngredientRequirement>();

    [Header("Final Product Prefab")]
    public GameObject finalProductPrefab;
}

public enum RecipeMatchState
{
    Invalid,
    Possible,
    Complete
}

public class CookingStation : MonoBehaviourPun
{
    [Header("Debug")]
    public bool showDebugLog = true;

    [Header("Photon Setting")]
    [Tooltip("Turn this off for local testing.")]
    public bool usePhotonSync = false;

    [Header("Input Method")]
    [Tooltip(
        "Enable this only if ingredient objects should automatically enter " +
        "the recipe when their Collider touches this trigger. " +
        "Disable it when using PlayerInteraction + F key."
    )]
    public bool acceptIngredientByTrigger = false;

    [Header("Recipe List")]
    public List<CookingRecipe> recipes = new List<CookingRecipe>();

    [Header("Final Product")]
    public Transform finalProductSpawnPoint;

    [Header("Failed Product")]
    public GameObject failedProductPrefab;
    public Transform failedProductSpawnPoint;

    [Header("Trigger Ingredient Setting")]
    [Tooltip(
        "Only applies when Accept Ingredient By Trigger is enabled. " +
        "Destroys the physical ingredient that entered the trigger."
    )]
    public bool destroyTriggerIngredientAfterAdding = true;

    [Header("Clear Visuals When Recipe Ends")]
    public bool clearPlacedVisualsAfterSuccess = true;
    public bool clearPlacedVisualsAfterFail = true;

    [Header("Destroy Cooking Station When Recipe Ends")]
    public bool destroyCookingStationAfterSuccess = false;
    public bool destroyCookingStationAfterFail = false;

    [Header("Destroy Target Object")]
    [Tooltip(
        "Object deleted after success or failure. " +
        "If empty, the GameObject containing this script is deleted."
    )]
    public GameObject destroyTargetObject;

    [Header("Detach Spawn Points Before Destroy")]
    public bool detachFinalProductSpawnPointBeforeDestroy = true;
    public bool detachFailedProductSpawnPointBeforeDestroy = true;

    [Tooltip(
        "Usually keep this disabled. Topping spawn points normally disappear " +
        "together with the CookingStation."
    )]
    public bool detachIngredientVisualSpawnPointsBeforeDestroy = false;

    private readonly List<string> currentIngredientIds =
        new List<string>();

    private readonly List<GameObject> spawnedVisualObjects =
        new List<GameObject>();

    private bool recipeEnded;

    private void Start()
    {
        Log("CookingStation started.");
        Log("GameObject: " + gameObject.name);
        Log("Use Photon Sync: " + usePhotonSync);
        Log("Accept Ingredient By Trigger: " + acceptIngredientByTrigger);
        Log("Photon Connected: " + PhotonNetwork.IsConnected);
        Log("Is MasterClient: " + PhotonNetwork.IsMasterClient);
        Log("Recipe Count: " + recipes.Count);

        ValidateCollider();
        CheckRecipeSetup();
    }

    private void ValidateCollider()
    {
        Collider stationCollider = GetComponent<Collider>();

        if (stationCollider == null)
        {
            LogWarning(
                "CookingStation has no Collider. " +
                "Nearby-player detection or trigger input may not work."
            );
            return;
        }

        Log(
            "CookingStation Collider: " +
            stationCollider.GetType().Name
        );

        Log(
            "CookingStation Collider Is Trigger: " +
            stationCollider.isTrigger
        );

        if (!stationCollider.isTrigger)
        {
            LogWarning(
                "CookingStation Collider should normally have Is Trigger enabled."
            );
        }
    }

    private void CheckRecipeSetup()
    {
        Log("Checking recipe setup...");

        if (recipes == null || recipes.Count == 0)
        {
            LogWarning("No recipes assigned.");
            return;
        }

        for (int recipeIndex = 0;
             recipeIndex < recipes.Count;
             recipeIndex++)
        {
            CookingRecipe recipe = recipes[recipeIndex];

            if (recipe == null)
            {
                LogWarning(
                    "Recipe index " + recipeIndex + " is null."
                );
                continue;
            }

            Log(
                "Recipe [" + recipeIndex + "]: " +
                recipe.recipeName
            );

            Log("Use Order: " + recipe.useOrder);

            if (recipe.requiredIngredients == null ||
                recipe.requiredIngredients.Count == 0)
            {
                LogWarning(
                    "Recipe [" + recipe.recipeName +
                    "] has no required ingredients."
                );
            }
            else
            {
                for (int requirementIndex = 0;
                     requirementIndex <
                     recipe.requiredIngredients.Count;
                     requirementIndex++)
                {
                    IngredientRequirement requirement =
                        recipe.requiredIngredients[requirementIndex];

                    if (requirement == null)
                    {
                        LogWarning(
                            "Recipe [" + recipe.recipeName +
                            "] requirement [" +
                            requirementIndex + "] is null."
                        );
                        continue;
                    }

                    string id =
                        GetIngredientIdFromRequirement(requirement);

                    Log(
                        "Recipe [" + recipe.recipeName +
                        "] Requirement [" +
                        requirementIndex + "] Prefab: " +
                        GetObjectName(requirement.ingredientPrefab) +
                        " / ID: " + id
                    );

                    if (string.IsNullOrEmpty(id))
                    {
                        LogWarning(
                            "Requirement [" +
                            requirementIndex +
                            "] has no valid IngredientId."
                        );
                    }

                    if (requirement.visualPrefabOnDish == null)
                    {
                        LogWarning(
                            "Requirement [" +
                            requirementIndex +
                            "] has no Visual Prefab On Dish."
                        );
                    }

                    if (requirement.visualSpawnPoints == null ||
                        requirement.visualSpawnPoints.Count == 0)
                    {
                        LogWarning(
                            "Requirement [" +
                            requirementIndex +
                            "] has no visual spawn points."
                        );
                    }
                }
            }

            if (recipe.finalProductPrefab == null)
            {
                LogWarning(
                    "Recipe [" + recipe.recipeName +
                    "] has no final product prefab."
                );
            }
            else
            {
                Log(
                    "Recipe [" + recipe.recipeName +
                    "] Final Product: " +
                    recipe.finalProductPrefab.name
                );
            }
        }

        if (finalProductSpawnPoint == null)
        {
            LogWarning(
                "Final Product Spawn Point is empty. " +
                "The station position plus Vector3.up will be used."
            );
        }

        if (failedProductPrefab == null)
        {
            LogWarning("Failed Product Prefab is empty.");
        }

        if (failedProductSpawnPoint == null)
        {
            LogWarning(
                "Failed Product Spawn Point is empty. " +
                "The station position plus Vector3.up will be used."
            );
        }

        if (destroyTargetObject == null)
        {
            Log(
                "Destroy Target Object is empty. " +
                "If deletion is enabled, this GameObject will be destroyed."
            );
        }
        else
        {
            Log(
                "Destroy Target Object: " +
                destroyTargetObject.name
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!acceptIngredientByTrigger)
        {
            return;
        }

        if (recipeEnded)
        {
            return;
        }

        Log(
            "Trigger ingredient detected object: " +
            other.gameObject.name
        );

        IngredientId ingredient =
            FindIngredientComponent(other.gameObject);

        if (ingredient == null)
        {
            LogWarning(
                "Entered object has no IngredientId: " +
                other.gameObject.name
            );
            return;
        }

        if (string.IsNullOrEmpty(ingredient.ingredientId))
        {
            LogWarning(
                "Entered ingredient has an empty IngredientId."
            );
            return;
        }

        bool accepted =
            TryAddIngredient(ingredient.ingredientId);

        if (accepted &&
            destroyTriggerIngredientAfterAdding)
        {
            DestroyInputIngredient(
                ingredient.gameObject
            );
        }
    }

    /// <summary>
    /// Called by PlayerInteraction when the player presses F.
    ///
    /// Returning true means this CookingStation has taken responsibility
    /// for the ingredient. The player's inventory can then consume the
    /// held ingredient object.
    ///
    /// A wrong ingredient also returns true because the station processes
    /// it as a failed recipe.
    /// </summary>
    public bool TryAddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            Log(
                "TryAddIngredient rejected because recipe already ended."
            );
            return false;
        }

        if (string.IsNullOrEmpty(ingredientId))
        {
            LogWarning(
                "TryAddIngredient rejected because ID is empty."
            );
            return false;
        }

        if (recipes == null || recipes.Count == 0)
        {
            LogWarning(
                "TryAddIngredient rejected because no recipes are assigned."
            );
            return false;
        }

        Log(
            "TryAddIngredient accepted interaction request: " +
            ingredientId
        );

        AddIngredient(ingredientId);
        return true;
    }

    public void AddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            Log(
                "Cannot add ingredient because recipe already ended: " +
                ingredientId
            );
            return;
        }

        if (string.IsNullOrEmpty(ingredientId))
        {
            LogWarning("Cannot add an empty ingredient ID.");
            return;
        }

        Log("AddIngredient called: " + ingredientId);

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            if (photonView == null)
            {
                LogWarning(
                    "Photon mode is enabled, but this CookingStation " +
                    "has no PhotonView."
                );
                return;
            }

            /*
             * The sender also receives RpcTarget.All.
             * Every client therefore updates the same recipe state.
             */
            photonView.RPC(
                nameof(RPC_AddIngredient),
                RpcTarget.All,
                ingredientId
            );
        }
        else
        {
            LocalAddIngredient(ingredientId);
        }
    }

    [PunRPC]
    private void RPC_AddIngredient(string ingredientId)
    {
        Log(
            "RPC_AddIngredient received: " +
            ingredientId
        );

        LocalAddIngredient(ingredientId);
    }

    private void LocalAddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            return;
        }

        Log(
            "Current ingredients before add: " +
            GetCurrentIngredientText()
        );

        currentIngredientIds.Add(ingredientId);

        Log(
            "Current ingredients after add: " +
            GetCurrentIngredientText()
        );

        List<CookingRecipe> possibleRecipes =
            new List<CookingRecipe>();

        CookingRecipe completedRecipe = null;

        IngredientRequirement requirementForVisual = null;

        foreach (CookingRecipe recipe in recipes)
        {
            if (recipe == null)
            {
                continue;
            }

            RecipeMatchState state =
                CheckRecipeState(
                    recipe,
                    currentIngredientIds
                );

            Log(
                "Recipe [" + recipe.recipeName +
                "] state: " + state
            );

            if (state == RecipeMatchState.Invalid)
            {
                continue;
            }

            possibleRecipes.Add(recipe);

            /*
             * This selects the correct occurrence of a repeated ingredient.
             *
             * Example:
             * Element 0 = chicken -> ChickenPoint1
             * Element 2 = chicken -> ChickenPoint2
             *
             * First chicken uses Element 0.
             * Second chicken uses Element 2.
             */
            if (requirementForVisual == null)
            {
                requirementForVisual =
                    FindRequirementForCurrentAdd(
                        recipe,
                        ingredientId,
                        currentIngredientIds
                    );
            }

            if (state == RecipeMatchState.Complete &&
                completedRecipe == null)
            {
                completedRecipe = recipe;
            }
        }

        if (possibleRecipes.Count == 0)
        {
            LogWarning(
                "No possible recipe remains after adding: " +
                ingredientId
            );

            HandleFailedRecipe(
                "Wrong ingredient or incorrect order: " +
                ingredientId
            );

            return;
        }

        if (requirementForVisual != null)
        {
            SpawnIngredientVisuals(
                requirementForVisual
            );
        }
        else
        {
            LogWarning(
                "Ingredient is still valid, but no matching " +
                "visual requirement was found: " +
                ingredientId
            );
        }

        if (completedRecipe != null)
        {
            HandleCompletedRecipe(completedRecipe);
        }
        else
        {
            Log(
                "Recipe not completed yet. Possible recipes: " +
                possibleRecipes.Count
            );
        }
    }

    private RecipeMatchState CheckRecipeState(
        CookingRecipe recipe,
        List<string> currentIds)
    {
        if (recipe == null ||
            recipe.requiredIngredients == null ||
            recipe.requiredIngredients.Count == 0)
        {
            return RecipeMatchState.Invalid;
        }

        if (currentIds == null ||
            currentIds.Count == 0)
        {
            return RecipeMatchState.Possible;
        }

        if (currentIds.Count >
            recipe.requiredIngredients.Count)
        {
            return RecipeMatchState.Invalid;
        }

        if (recipe.useOrder)
        {
            return CheckOrderedRecipeState(
                recipe,
                currentIds
            );
        }

        return CheckUnorderedRecipeState(
            recipe,
            currentIds
        );
    }

    private RecipeMatchState CheckOrderedRecipeState(
        CookingRecipe recipe,
        List<string> currentIds)
    {
        for (int i = 0; i < currentIds.Count; i++)
        {
            string expectedId =
                GetIngredientIdFromRequirement(
                    recipe.requiredIngredients[i]
                );

            string actualId = currentIds[i];

            Log(
                "Ordered check [" + recipe.recipeName +
                "] index " + i +
                ", expected: " + expectedId +
                ", actual: " + actualId
            );

            if (expectedId != actualId)
            {
                return RecipeMatchState.Invalid;
            }
        }

        if (currentIds.Count ==
            recipe.requiredIngredients.Count)
        {
            return RecipeMatchState.Complete;
        }

        return RecipeMatchState.Possible;
    }

    private RecipeMatchState CheckUnorderedRecipeState(
        CookingRecipe recipe,
        List<string> currentIds)
    {
        Dictionary<string, int> requiredCounts =
            GetRequiredCounts(recipe);

        Dictionary<string, int> currentCounts =
            GetCounts(currentIds);

        foreach (KeyValuePair<string, int> pair
                 in currentCounts)
        {
            if (!requiredCounts.ContainsKey(pair.Key))
            {
                Log(
                    "Recipe [" + recipe.recipeName +
                    "] does not require ingredient: " +
                    pair.Key
                );

                return RecipeMatchState.Invalid;
            }

            if (pair.Value >
                requiredCounts[pair.Key])
            {
                Log(
                    "Recipe [" + recipe.recipeName +
                    "] received too many of ingredient: " +
                    pair.Key
                );

                return RecipeMatchState.Invalid;
            }
        }

        if (currentIds.Count ==
            recipe.requiredIngredients.Count)
        {
            foreach (KeyValuePair<string, int> pair
                     in requiredCounts)
            {
                int currentCount =
                    currentCounts.ContainsKey(pair.Key)
                        ? currentCounts[pair.Key]
                        : 0;

                if (currentCount != pair.Value)
                {
                    return RecipeMatchState.Invalid;
                }
            }

            return RecipeMatchState.Complete;
        }

        return RecipeMatchState.Possible;
    }

    private IngredientRequirement
        FindRequirementForCurrentAdd(
            CookingRecipe recipe,
            string ingredientId,
            List<string> currentIds)
    {
        if (recipe == null ||
            recipe.requiredIngredients == null ||
            currentIds == null)
        {
            return null;
        }

        /*
         * Ordered recipes are easy:
         * the newest ingredient corresponds directly to the newest index.
         */
        if (recipe.useOrder)
        {
            int currentIndex =
                currentIds.Count - 1;

            if (currentIndex < 0 ||
                currentIndex >=
                recipe.requiredIngredients.Count)
            {
                return null;
            }

            IngredientRequirement requirement =
                recipe.requiredIngredients[currentIndex];

            string requiredId =
                GetIngredientIdFromRequirement(
                    requirement
                );

            if (requiredId == ingredientId)
            {
                return requirement;
            }

            return null;
        }

        /*
         * Unordered recipe:
         * count which occurrence of this ingredient was just added.
         *
         * If chicken now appears twice in currentIds,
         * use the second chicken requirement in the recipe.
         */
        int currentOccurrence = 0;

        foreach (string currentId in currentIds)
        {
            if (currentId == ingredientId)
            {
                currentOccurrence++;
            }
        }

        int requirementOccurrence = 0;

        foreach (IngredientRequirement requirement
                 in recipe.requiredIngredients)
        {
            string requiredId =
                GetIngredientIdFromRequirement(
                    requirement
                );

            if (requiredId != ingredientId)
            {
                continue;
            }

            requirementOccurrence++;

            if (requirementOccurrence ==
                currentOccurrence)
            {
                return requirement;
            }
        }

        return null;
    }

    private void SpawnIngredientVisuals(
        IngredientRequirement requirement)
    {
        if (requirement == null)
        {
            return;
        }

        if (requirement.visualPrefabOnDish == null)
        {
            LogWarning(
                "Cannot spawn ingredient visual because prefab is empty."
            );
            return;
        }

        if (requirement.visualSpawnPoints == null ||
            requirement.visualSpawnPoints.Count == 0)
        {
            LogWarning(
                "Cannot spawn ingredient visual because no spawn points exist."
            );
            return;
        }

        Log(
            "Spawning visual prefab: " +
            requirement.visualPrefabOnDish.name
        );

        foreach (Transform spawnPoint
                 in requirement.visualSpawnPoints)
        {
            if (spawnPoint == null)
            {
                LogWarning(
                    "One ingredient visual spawn point is null."
                );
                continue;
            }

            SpawnObject(
                requirement.visualPrefabOnDish,
                spawnPoint.position,
                spawnPoint.rotation,
                true
            );
        }
    }

    private void HandleCompletedRecipe(
        CookingRecipe completedRecipe)
    {
        if (recipeEnded)
        {
            return;
        }

        recipeEnded = true;

        Log(
            "RECIPE COMPLETED: " +
            completedRecipe.recipeName
        );

        SpawnFinalProduct(completedRecipe);

        if (clearPlacedVisualsAfterSuccess)
        {
            ClearSpawnedVisuals();
        }

        ClearProgress();

        if (destroyCookingStationAfterSuccess)
        {
            DestroyCookingStationObject();
        }
    }

    private void SpawnFinalProduct(
        CookingRecipe recipe)
    {
        if (recipe == null ||
            recipe.finalProductPrefab == null)
        {
            LogWarning(
                "Cannot spawn final product because prefab is empty."
            );
            return;
        }

        Vector3 position =
            finalProductSpawnPoint != null
                ? finalProductSpawnPoint.position
                : transform.position + Vector3.up;

        Quaternion rotation =
            finalProductSpawnPoint != null
                ? finalProductSpawnPoint.rotation
                : Quaternion.identity;

        Log(
            "Spawning final product: " +
            recipe.finalProductPrefab.name
        );

        SpawnObject(
            recipe.finalProductPrefab,
            position,
            rotation,
            false
        );
    }

    private void HandleFailedRecipe(string reason)
    {
        if (recipeEnded)
        {
            return;
        }

        recipeEnded = true;

        LogWarning(
            "RECIPE FAILED. Reason: " +
            reason
        );

        Vector3 position =
            failedProductSpawnPoint != null
                ? failedProductSpawnPoint.position
                : transform.position + Vector3.up;

        Quaternion rotation =
            failedProductSpawnPoint != null
                ? failedProductSpawnPoint.rotation
                : Quaternion.identity;

        if (failedProductPrefab != null)
        {
            SpawnObject(
                failedProductPrefab,
                position,
                rotation,
                false
            );
        }
        else
        {
            LogWarning(
                "Failed Product Prefab is empty."
            );
        }

        if (clearPlacedVisualsAfterFail)
        {
            ClearSpawnedVisuals();
        }

        ClearProgress();

        if (destroyCookingStationAfterFail)
        {
            DestroyCookingStationObject();
        }
    }

    private GameObject SpawnObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        bool trackAsVisual)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject spawnedObject;

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            /*
             * Only MasterClient creates recipe visuals and products.
             * PhotonNetwork.Instantiate automatically shows the object
             * to every player.
             */
            if (!PhotonNetwork.IsMasterClient)
            {
                Log(
                    "This client is not MasterClient, " +
                    "so it will not instantiate: " +
                    prefab.name
                );

                return null;
            }

            Log(
                "PhotonNetwork.Instantiate: " +
                prefab.name
            );

            spawnedObject =
                PhotonNetwork.Instantiate(
                    prefab.name,
                    position,
                    rotation
                );
        }
        else
        {
            Log(
                "Local Instantiate: " +
                prefab.name
            );

            spawnedObject =
                Instantiate(
                    prefab,
                    position,
                    rotation
                );
        }

        if (trackAsVisual &&
            spawnedObject != null)
        {
            spawnedVisualObjects.Add(
                spawnedObject
            );
        }

        return spawnedObject;
    }

    private void DestroyInputIngredient(
        GameObject ingredientObject)
    {
        if (ingredientObject == null)
        {
            return;
        }

        Log(
            "Destroying input ingredient: " +
            ingredientObject.name
        );

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            PhotonView itemView =
                ingredientObject.GetComponent<PhotonView>();

            if (itemView == null)
            {
                itemView =
                    ingredientObject
                        .GetComponentInParent<PhotonView>();
            }

            if (itemView != null &&
                itemView.IsMine)
            {
                PhotonNetwork.Destroy(
                    itemView.gameObject
                );
            }
            else
            {
                LogWarning(
                    "Input ingredient has no locally owned PhotonView. " +
                    "It was not network-destroyed."
                );
            }
        }
        else
        {
            Destroy(ingredientObject);
        }
    }

    private void ClearSpawnedVisuals()
    {
        Log(
            "Clearing spawned visual objects. Count: " +
            spawnedVisualObjects.Count
        );

        for (int i =
                 spawnedVisualObjects.Count - 1;
             i >= 0;
             i--)
        {
            GameObject visualObject =
                spawnedVisualObjects[i];

            if (visualObject == null)
            {
                continue;
            }

            if (usePhotonSync &&
                PhotonNetwork.IsConnected)
            {
                PhotonView visualView =
                    visualObject.GetComponent<PhotonView>();

                if (visualView != null &&
                    PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(
                        visualView.gameObject
                    );
                }
                else if (visualView == null)
                {
                    LogWarning(
                        "A spawned visual has no PhotonView: " +
                        visualObject.name
                    );
                }
            }
            else
            {
                Destroy(visualObject);
            }
        }

        spawnedVisualObjects.Clear();
    }

    private void DestroyCookingStationObject()
    {
        Log("DestroyCookingStationObject called.");

        DetachSpawnPointsBeforeDestroy();

        GameObject target =
            destroyTargetObject != null
                ? destroyTargetObject
                : gameObject;

        Log(
            "Destroying CookingStation target: " +
            target.name
        );

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            PhotonView targetView =
                target.GetComponent<PhotonView>();

            if (targetView == null)
            {
                targetView =
                    target.GetComponentInChildren<PhotonView>();
            }

            if (targetView == null)
            {
                LogWarning(
                    "Destroy target has no PhotonView. " +
                    "It cannot be safely network-destroyed."
                );
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Log(
                    "Only MasterClient destroys the CookingStation."
                );
                return;
            }

            PhotonNetwork.Destroy(
                targetView.gameObject
            );
        }
        else
        {
            Destroy(target);
        }
    }

    private void DetachSpawnPointsBeforeDestroy()
    {
        if (detachFinalProductSpawnPointBeforeDestroy &&
            finalProductSpawnPoint != null)
        {
            finalProductSpawnPoint.SetParent(
                null,
                true
            );

            Log(
                "Detached final spawn point: " +
                finalProductSpawnPoint.name
            );
        }

        if (detachFailedProductSpawnPointBeforeDestroy &&
            failedProductSpawnPoint != null &&
            failedProductSpawnPoint !=
            finalProductSpawnPoint)
        {
            failedProductSpawnPoint.SetParent(
                null,
                true
            );

            Log(
                "Detached failed spawn point: " +
                failedProductSpawnPoint.name
            );
        }

        if (detachIngredientVisualSpawnPointsBeforeDestroy)
        {
            DetachIngredientVisualSpawnPoints();
        }
    }

    private void DetachIngredientVisualSpawnPoints()
    {
        HashSet<Transform> detachedPoints =
            new HashSet<Transform>();

        foreach (CookingRecipe recipe in recipes)
        {
            if (recipe == null ||
                recipe.requiredIngredients == null)
            {
                continue;
            }

            foreach (IngredientRequirement requirement
                     in recipe.requiredIngredients)
            {
                if (requirement == null ||
                    requirement.visualSpawnPoints == null)
                {
                    continue;
                }

                foreach (Transform point
                         in requirement.visualSpawnPoints)
                {
                    if (point == null ||
                        detachedPoints.Contains(point))
                    {
                        continue;
                    }

                    point.SetParent(null, true);
                    detachedPoints.Add(point);
                }
            }
        }
    }

    private IngredientId FindIngredientComponent(
        GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        IngredientId ingredient =
            target.GetComponent<IngredientId>();

        if (ingredient == null)
        {
            ingredient =
                target.GetComponentInParent<IngredientId>();
        }

        if (ingredient == null)
        {
            ingredient =
                target.GetComponentInChildren<IngredientId>();
        }

        return ingredient;
    }

    private string GetIngredientIdFromRequirement(
        IngredientRequirement requirement)
    {
        if (requirement == null)
        {
            return "";
        }

        return GetIngredientIdFromPrefab(
            requirement.ingredientPrefab
        );
    }

    private string GetIngredientIdFromPrefab(
        GameObject prefab)
    {
        if (prefab == null)
        {
            return "";
        }

        IngredientId ingredient =
            FindIngredientComponent(prefab);

        return ingredient != null
            ? ingredient.ingredientId
            : "";
    }

    private Dictionary<string, int>
        GetRequiredCounts(CookingRecipe recipe)
    {
        Dictionary<string, int> counts =
            new Dictionary<string, int>();

        if (recipe == null ||
            recipe.requiredIngredients == null)
        {
            return counts;
        }

        foreach (IngredientRequirement requirement
                 in recipe.requiredIngredients)
        {
            string id =
                GetIngredientIdFromRequirement(
                    requirement
                );

            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (!counts.ContainsKey(id))
            {
                counts[id] = 0;
            }

            counts[id]++;
        }

        return counts;
    }

    private Dictionary<string, int> GetCounts(
        List<string> ids)
    {
        Dictionary<string, int> counts =
            new Dictionary<string, int>();

        if (ids == null)
        {
            return counts;
        }

        foreach (string id in ids)
        {
            if (!counts.ContainsKey(id))
            {
                counts[id] = 0;
            }

            counts[id]++;
        }

        return counts;
    }

    private void ClearProgress()
    {
        Log("Clearing recipe progress.");
        currentIngredientIds.Clear();
    }

    public void ResetStation()
    {
        Log("ResetStation called.");

        recipeEnded = false;
        ClearProgress();
        ClearSpawnedVisuals();
    }

    public bool HasRecipeEnded()
    {
        return recipeEnded;
    }

    private string GetCurrentIngredientText()
    {
        if (currentIngredientIds.Count == 0)
        {
            return "Empty";
        }

        return string.Join(
            ", ",
            currentIngredientIds
        );
    }

    private string GetObjectName(
        GameObject target)
    {
        return target != null
            ? target.name
            : "NULL";
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[CookingStation] " + message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[CookingStation] " + message,
                this
            );
        }
    }
}