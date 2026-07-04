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
    public List<Transform> visualSpawnPoints =
        new List<Transform>();
}

[System.Serializable]
public class CookingRecipe
{
    [Header("Recipe Name")]
    public string recipeName;

    [Header("Order Requirement")]
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

    [Header("Photon")]
    public bool usePhotonSync = false;

    [Header("Input Method")]
    [Tooltip(
        "使用玩家按 F 放食材时保持关闭。"
    )]
    public bool acceptIngredientByTrigger = false;

    [Header("Recipes")]
    public List<CookingRecipe> recipes =
        new List<CookingRecipe>();

    [Header("Final Product")]
    public Transform finalProductSpawnPoint;

    [Header("Failed Product")]
    public GameObject failedProductPrefab;
    public Transform failedProductSpawnPoint;

    [Header("Clear Visuals")]
    public bool clearPlacedVisualsAfterSuccess = true;
    public bool clearPlacedVisualsAfterFail = true;

    [Header("Destroy Cooking Station")]
    public bool destroyCookingStationAfterSuccess = true;
    public bool destroyCookingStationAfterFail = true;

    [Header("Destroy Target")]
    public GameObject destroyTargetObject;

    [Header("Detach Spawn Points")]
    public bool detachFinalProductSpawnPointBeforeDestroy = true;
    public bool detachFailedProductSpawnPointBeforeDestroy = true;

    private readonly List<string> currentIngredientIds =
        new List<string>();

    private readonly List<GameObject> spawnedVisualObjects =
        new List<GameObject>();

    private bool recipeEnded;

    private PlacementSurface placementSurface;

    private void Start()
    {
        Log("CookingStation started.");

        Collider col = GetComponent<Collider>();

        if (col == null)
        {
            LogWarning("CookingStation has no Collider.");
        }
        else if (!col.isTrigger)
        {
            LogWarning(
                "CookingStation Collider should use Is Trigger."
            );
        }
    }

    public void SetPlacementSurface(
        PlacementSurface surface)
    {
        placementSurface = surface;

        Log(
            "PlacementSurface registered: " +
            (surface != null ? surface.name : "NULL")
        );
    }

    public bool TryAddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            return false;
        }

        if (string.IsNullOrEmpty(ingredientId))
        {
            return false;
        }

        if (recipes == null || recipes.Count == 0)
        {
            return false;
        }

        AddIngredient(ingredientId);
        return true;
    }

    public void AddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            return;
        }

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
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
        LocalAddIngredient(ingredientId);
    }

    private void LocalAddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            return;
        }

        currentIngredientIds.Add(ingredientId);

        List<CookingRecipe> possibleRecipes =
            new List<CookingRecipe>();

        CookingRecipe completedRecipe = null;

        IngredientRequirement visualRequirement = null;

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

            if (visualRequirement == null)
            {
                visualRequirement =
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
            HandleFailedRecipe(
                "Wrong ingredient or incorrect order."
            );

            return;
        }

        if (visualRequirement != null)
        {
            SpawnIngredientVisuals(
                visualRequirement
            );
        }

        if (completedRecipe != null)
        {
            HandleCompletedRecipe(
                completedRecipe
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

        if (currentIds.Count >
            recipe.requiredIngredients.Count)
        {
            return RecipeMatchState.Invalid;
        }

        if (recipe.useOrder)
        {
            for (int i = 0;
                 i < currentIds.Count;
                 i++)
            {
                string expected =
                    GetIngredientId(
                        recipe.requiredIngredients[i]
                    );

                if (currentIds[i] != expected)
                {
                    return RecipeMatchState.Invalid;
                }
            }

            return currentIds.Count ==
                   recipe.requiredIngredients.Count
                ? RecipeMatchState.Complete
                : RecipeMatchState.Possible;
        }

        Dictionary<string, int> required =
            GetRequiredCounts(recipe);

        Dictionary<string, int> current =
            GetCounts(currentIds);

        foreach (KeyValuePair<string, int> pair
                 in current)
        {
            if (!required.ContainsKey(pair.Key))
            {
                return RecipeMatchState.Invalid;
            }

            if (pair.Value > required[pair.Key])
            {
                return RecipeMatchState.Invalid;
            }
        }

        if (currentIds.Count ==
            recipe.requiredIngredients.Count)
        {
            foreach (KeyValuePair<string, int> pair
                     in required)
            {
                int count =
                    current.ContainsKey(pair.Key)
                        ? current[pair.Key]
                        : 0;

                if (count != pair.Value)
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
        if (recipe.useOrder)
        {
            int index = currentIds.Count - 1;

            if (index < 0 ||
                index >= recipe.requiredIngredients.Count)
            {
                return null;
            }

            IngredientRequirement requirement =
                recipe.requiredIngredients[index];

            return GetIngredientId(requirement) ==
                   ingredientId
                ? requirement
                : null;
        }

        int currentOccurrence = 0;

        foreach (string id in currentIds)
        {
            if (id == ingredientId)
            {
                currentOccurrence++;
            }
        }

        int requirementOccurrence = 0;

        foreach (IngredientRequirement requirement
                 in recipe.requiredIngredients)
        {
            if (GetIngredientId(requirement) !=
                ingredientId)
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
        if (requirement == null ||
            requirement.visualPrefabOnDish == null ||
            requirement.visualSpawnPoints == null)
        {
            return;
        }

        foreach (Transform point
                 in requirement.visualSpawnPoints)
        {
            if (point == null)
            {
                continue;
            }

            GameObject visual = SpawnObject(
                requirement.visualPrefabOnDish,
                point.position,
                point.rotation
            );

            if (visual != null)
            {
                spawnedVisualObjects.Add(visual);
            }
        }
    }

    private void HandleCompletedRecipe(
        CookingRecipe recipe)
    {
        if (recipeEnded)
        {
            return;
        }

        recipeEnded = true;

        GameObject finalObject =
            SpawnFinalProduct(recipe);

        /*
         * 先让桌子清掉 CookingBase 的占用记录，
         * 再登记新生成的 FinishedFood。
         */
        if (placementSurface != null)
        {
            placementSurface.ClearOccupiedReference();

            if (finalObject != null)
            {
                placementSurface.RegisterPlacedItem(
                    finalObject
                );
            }
        }

        if (clearPlacedVisualsAfterSuccess)
        {
            ClearSpawnedVisuals();
        }

        currentIngredientIds.Clear();

        if (destroyCookingStationAfterSuccess)
        {
            DestroyCookingStationObject();
        }
    }

    private GameObject SpawnFinalProduct(
        CookingRecipe recipe)
    {
        if (recipe == null ||
            recipe.finalProductPrefab == null)
        {
            LogWarning(
                "Final Product Prefab is missing."
            );

            return null;
        }

        Vector3 position =
            finalProductSpawnPoint != null
                ? finalProductSpawnPoint.position
                : transform.position;

        Quaternion rotation =
            finalProductSpawnPoint != null
                ? finalProductSpawnPoint.rotation
                : transform.rotation;

        return SpawnObject(
            recipe.finalProductPrefab,
            position,
            rotation
        );
    }

    private void HandleFailedRecipe(string reason)
    {
        if (recipeEnded)
        {
            return;
        }

        recipeEnded = true;

        Vector3 position =
            failedProductSpawnPoint != null
                ? failedProductSpawnPoint.position
                : transform.position;

        Quaternion rotation =
            failedProductSpawnPoint != null
                ? failedProductSpawnPoint.rotation
                : transform.rotation;

        GameObject failedObject = null;

        if (failedProductPrefab != null)
        {
            failedObject = SpawnObject(
                failedProductPrefab,
                position,
                rotation
            );
        }

        if (placementSurface != null)
        {
            placementSurface.ClearOccupiedReference();

            /*
             * 失败物如果也想允许拿取，
             * 它必须挂 HoldableItem 且类型为 FinishedFood。
             */
            if (failedObject != null)
            {
                HoldableItem item =
                    failedObject.GetComponent<HoldableItem>();

                if (item != null &&
                    item.itemType ==
                    HoldableItemType.FinishedFood)
                {
                    placementSurface.RegisterPlacedItem(
                        failedObject
                    );
                }
            }
        }

        if (clearPlacedVisualsAfterFail)
        {
            ClearSpawnedVisuals();
        }

        currentIngredientIds.Clear();

        if (destroyCookingStationAfterFail)
        {
            DestroyCookingStationObject();
        }

        LogWarning(
            "Recipe failed: " + reason
        );
    }

    private GameObject SpawnObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return null;
            }

            return PhotonNetwork.Instantiate(
                prefab.name,
                position,
                rotation
            );
        }

        return Instantiate(
            prefab,
            position,
            rotation
        );
    }

    private void ClearSpawnedVisuals()
    {
        for (int i =
                 spawnedVisualObjects.Count - 1;
             i >= 0;
             i--)
        {
            GameObject obj =
                spawnedVisualObjects[i];

            if (obj == null)
            {
                continue;
            }

            if (usePhotonSync &&
                PhotonNetwork.IsConnected)
            {
                PhotonView view =
                    obj.GetComponent<PhotonView>();

                if (view != null &&
                    PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(obj);
                }
            }
            else
            {
                Destroy(obj);
            }
        }

        spawnedVisualObjects.Clear();
    }

    private void DestroyCookingStationObject()
    {
        if (detachFinalProductSpawnPointBeforeDestroy &&
            finalProductSpawnPoint != null)
        {
            finalProductSpawnPoint.SetParent(
                null,
                true
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
        }

        GameObject target =
            destroyTargetObject != null
                ? destroyTargetObject
                : gameObject;

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            PhotonView view =
                target.GetComponent<PhotonView>();

            if (view == null)
            {
                view =
                    target.GetComponentInChildren<PhotonView>();
            }

            if (view != null &&
                PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(
                    view.gameObject
                );
            }
        }
        else
        {
            Destroy(target);
        }
    }

    private string GetIngredientId(
        IngredientRequirement requirement)
    {
        if (requirement == null ||
            requirement.ingredientPrefab == null)
        {
            return "";
        }

        IngredientId id =
            requirement.ingredientPrefab
                .GetComponent<IngredientId>();

        if (id == null)
        {
            id =
                requirement.ingredientPrefab
                    .GetComponentInChildren<IngredientId>();
        }

        return id != null
            ? id.ingredientId
            : "";
    }

    private Dictionary<string, int>
        GetRequiredCounts(CookingRecipe recipe)
    {
        Dictionary<string, int> result =
            new Dictionary<string, int>();

        foreach (IngredientRequirement requirement
                 in recipe.requiredIngredients)
        {
            string id =
                GetIngredientId(requirement);

            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (!result.ContainsKey(id))
            {
                result[id] = 0;
            }

            result[id]++;
        }

        return result;
    }

    private Dictionary<string, int>
        GetCounts(List<string> ids)
    {
        Dictionary<string, int> result =
            new Dictionary<string, int>();

        foreach (string id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = 0;
            }

            result[id]++;
        }

        return result;
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