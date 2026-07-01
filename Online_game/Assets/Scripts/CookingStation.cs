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

    [Header("Use Order")]
    public bool useOrder = false;

    [Header("Required Ingredients")]
    public List<IngredientRequirement> requiredIngredients = new List<IngredientRequirement>();

    [Header("Final Product Prefab")]
    public GameObject finalProductPrefab;
}

public class CookingStation : MonoBehaviourPun
{
    [Header("Debug")]
    public bool showDebugLog = true;

    [Header("Photon Setting")]
    [Tooltip("Turn this off for local testing.")]
    public bool usePhotonSync = false;

    [Header("Recipe List")]
    public List<CookingRecipe> recipes = new List<CookingRecipe>();

    [Header("Current Recipe Index")]
    public int currentRecipeIndex = 0;

    [Header("Final Product Spawn Point")]
    public Transform finalProductSpawnPoint;

    [Header("Failed Product Setting")]
    public GameObject failedProductPrefab;
    public Transform failedProductSpawnPoint;

    [Header("Input Ingredient Setting")]
    [Tooltip("Destroy the input ingredient object that enters the trigger zone, such as cheese_pack or sauce bottle.")]
    public bool destroyInputIngredientAfterAdding = true;

    [Header("Clear Visuals When Recipe Ends")]
    [Tooltip("Clear placed toppings after successful cooking.")]
    public bool clearPlacedVisualsAfterSuccess = true;

    [Tooltip("Clear placed toppings after failed cooking.")]
    public bool clearPlacedVisualsAfterFail = true;

    [Header("Destroy Cooking Station When Recipe Ends")]
    [Tooltip("Destroy this CookingStation object or Destroy Target Object after success.")]
    public bool destroyCookingStationAfterSuccess = false;

    [Tooltip("Destroy this CookingStation object or Destroy Target Object after fail.")]
    public bool destroyCookingStationAfterFail = false;

    [Header("Destroy Target Object")]
    [Tooltip("The object to destroy after success/fail. If empty, this GameObject will be destroyed. Usually this is the root parent, such as Pizza_dough root.")]
    public GameObject destroyTargetObject;

    [Header("Detach Spawn Points Before Destroy")]
    [Tooltip("Detach final product spawn point before destroying the cooking station target object.")]
    public bool detachFinalProductSpawnPointBeforeDestroy = true;

    [Tooltip("Detach failed product spawn point before destroying the cooking station target object.")]
    public bool detachFailedProductSpawnPointBeforeDestroy = true;

    [Tooltip("Detach ingredient visual spawn points before destroying the cooking station target object.")]
    public bool detachIngredientVisualSpawnPointsBeforeDestroy = false;

    private List<string> currentIngredientIds = new List<string>();
    private List<GameObject> spawnedVisualObjects = new List<GameObject>();

    private bool recipeEnded = false;

    private CookingRecipe CurrentRecipe
    {
        get
        {
            if (recipes == null || recipes.Count == 0)
                return null;

            if (currentRecipeIndex < 0 || currentRecipeIndex >= recipes.Count)
                return null;

            return recipes[currentRecipeIndex];
        }
    }

    private void Start()
    {
        Log("CookingStation started.");
        Log("GameObject: " + gameObject.name);
        Log("Use Photon Sync: " + usePhotonSync);
        Log("Photon Connected: " + PhotonNetwork.IsConnected);
        Log("Is MasterClient: " + PhotonNetwork.IsMasterClient);
        Log("Recipe Count: " + (recipes == null ? 0 : recipes.Count));

        Collider stationCollider = GetComponent<Collider>();

        if (stationCollider == null)
        {
            LogWarning("CookingStation has NO Collider. Trigger will not work.");
        }
        else
        {
            Log("CookingStation Collider: " + stationCollider.GetType().Name);
            Log("CookingStation Is Trigger: " + stationCollider.isTrigger);

            if (!stationCollider.isTrigger)
            {
                LogWarning("CookingStation Collider is NOT trigger. Please enable Is Trigger.");
            }
        }

        CheckRecipeSetup();
    }

    private void CheckRecipeSetup()
    {
        Log("Checking recipe setup...");

        if (recipes == null || recipes.Count == 0)
        {
            LogWarning("No recipes assigned.");
            return;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            CookingRecipe recipe = recipes[i];

            if (recipe == null)
            {
                LogWarning("Recipe [" + i + "] is null.");
                continue;
            }

            Log("Recipe [" + i + "]: " + recipe.recipeName);
            Log("Use Order: " + recipe.useOrder);

            if (recipe.requiredIngredients == null || recipe.requiredIngredients.Count == 0)
            {
                LogWarning("Recipe [" + recipe.recipeName + "] has no required ingredients.");
            }
            else
            {
                Log("Required Ingredient Count: " + recipe.requiredIngredients.Count);

                for (int j = 0; j < recipe.requiredIngredients.Count; j++)
                {
                    IngredientRequirement req = recipe.requiredIngredients[j];

                    if (req == null)
                    {
                        LogWarning("Requirement [" + j + "] is null.");
                        continue;
                    }

                    string id = GetIngredientIdFromRequirement(req);

                    Log("Required [" + j + "]: " + GetObjectName(req.ingredientPrefab) + " / ID = " + id);

                    if (string.IsNullOrEmpty(id))
                    {
                        LogWarning("Required ingredient [" + j + "] has empty ID or missing IngredientId.");
                    }

                    if (req.visualPrefabOnDish == null)
                    {
                        LogWarning("Requirement [" + j + "] has no visualPrefabOnDish.");
                    }
                    else
                    {
                        Log("Visual Prefab [" + j + "]: " + req.visualPrefabOnDish.name);
                    }

                    if (req.visualSpawnPoints == null || req.visualSpawnPoints.Count == 0)
                    {
                        LogWarning("Requirement [" + j + "] has no visual spawn points.");
                    }
                    else
                    {
                        Log("Visual Spawn Point Count [" + j + "]: " + req.visualSpawnPoints.Count);
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

        if (finalProductSpawnPoint == null)
        {
            LogWarning("Final Product Spawn Point is empty. Final product will spawn above station.");
        }

        if (failedProductPrefab == null)
        {
            LogWarning("Failed Product Prefab is empty.");
        }

        if (failedProductSpawnPoint == null)
        {
            LogWarning("Failed Product Spawn Point is empty. Failed product will spawn above station.");
        }

        if (destroyTargetObject == null)
        {
            Log("Destroy Target Object is empty. If destroy is enabled, this GameObject will be destroyed: " + gameObject.name);
        }
        else
        {
            Log("Destroy Target Object: " + destroyTargetObject.name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (recipeEnded)
        {
            Log("Recipe already ended. Ignoring trigger: " + other.gameObject.name);
            return;
        }

        Log("OnTriggerEnter detected object: " + other.gameObject.name);

        IngredientId ingredient = other.GetComponent<IngredientId>();

        if (ingredient == null)
            ingredient = other.GetComponentInParent<IngredientId>();

        if (ingredient == null)
            ingredient = other.GetComponentInChildren<IngredientId>();

        if (ingredient == null)
        {
            LogWarning("Entered object has no IngredientId. Ignored: " + other.gameObject.name);
            return;
        }

        string ingredientId = ingredient.ingredientId;

        Log("Ingredient detected object: " + ingredient.gameObject.name);
        Log("Ingredient ID: " + ingredientId);

        if (string.IsNullOrEmpty(ingredientId))
        {
            LogWarning("Ingredient ID is empty. Ignored.");
            return;
        }

        AddIngredient(ingredientId);

        if (destroyInputIngredientAfterAdding)
        {
            DestroyInputIngredient(ingredient.gameObject);
        }
    }

    public void AddIngredient(string ingredientId)
    {
        if (recipeEnded)
        {
            Log("Recipe already ended. Cannot add ingredient: " + ingredientId);
            return;
        }

        Log("AddIngredient called: " + ingredientId);

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            Log("Photon mode. Sending RPC_AddIngredient.");
            photonView.RPC(nameof(RPC_AddIngredient), RpcTarget.AllBuffered, ingredientId);
        }
        else
        {
            Log("Local mode. Adding ingredient locally.");
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
        if (recipeEnded)
        {
            Log("Recipe already ended inside LocalAddIngredient. Ignored.");
            return;
        }

        CookingRecipe recipe = CurrentRecipe;

        if (recipe == null)
        {
            LogWarning("No current recipe. Cannot add ingredient.");
            return;
        }

        Log("Current Recipe: " + recipe.recipeName);
        Log("Current ingredients before add: " + GetCurrentIngredientText());

        IngredientRequirement matchedRequirement = FindRequirementById(recipe, ingredientId);

        if (matchedRequirement == null)
        {
            LogWarning("Wrong ingredient. This ingredient is not required: " + ingredientId);
            HandleFailedRecipe("Wrong ingredient: " + ingredientId);
            return;
        }

        if (recipe.useOrder)
        {
            int nextIndex = currentIngredientIds.Count;

            if (nextIndex >= recipe.requiredIngredients.Count)
            {
                LogWarning("Too many ingredients added.");
                HandleFailedRecipe("Too many ingredients.");
                return;
            }

            string expectedId = GetIngredientIdFromRequirement(recipe.requiredIngredients[nextIndex]);

            Log("Order check. Expected: " + expectedId + " / Actual: " + ingredientId);

            if (ingredientId != expectedId)
            {
                LogWarning("Wrong order. Expected " + expectedId + " but got " + ingredientId);
                HandleFailedRecipe("Wrong order.");
                return;
            }
        }
        else
        {
            int alreadyAddedCount = CountCurrentIngredient(ingredientId);
            int requiredCount = CountRequiredIngredient(recipe, ingredientId);

            Log("Unordered count check. Already Added: " + alreadyAddedCount + " / Required: " + requiredCount);

            if (alreadyAddedCount >= requiredCount)
            {
                LogWarning("Too many of same ingredient: " + ingredientId);
                HandleFailedRecipe("Too many same ingredient: " + ingredientId);
                return;
            }
        }

        currentIngredientIds.Add(ingredientId);

        Log("Ingredient accepted: " + ingredientId);
        Log("Current ingredients after add: " + GetCurrentIngredientText());

        SpawnIngredientVisuals(matchedRequirement);

        if (IsRecipeComplete(recipe))
        {
            Log("RECIPE COMPLETED: " + recipe.recipeName);

            recipeEnded = true;

            SpawnFinalProduct(recipe);

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
        else
        {
            Log("Recipe not complete yet.");
        }
    }

    private void SpawnIngredientVisuals(IngredientRequirement requirement)
    {
        if (requirement == null)
        {
            LogWarning("Cannot spawn visuals because requirement is null.");
            return;
        }

        if (requirement.visualPrefabOnDish == null)
        {
            LogWarning("Cannot spawn visuals because visualPrefabOnDish is null.");
            return;
        }

        if (requirement.visualSpawnPoints == null || requirement.visualSpawnPoints.Count == 0)
        {
            LogWarning("Cannot spawn visuals because spawn point list is empty.");
            return;
        }

        Log("Spawning visual topping prefab: " + requirement.visualPrefabOnDish.name);
        Log("Spawn point count: " + requirement.visualSpawnPoints.Count);

        for (int i = 0; i < requirement.visualSpawnPoints.Count; i++)
        {
            Transform point = requirement.visualSpawnPoints[i];

            if (point == null)
            {
                LogWarning("Visual spawn point [" + i + "] is null.");
                continue;
            }

            SpawnObject(requirement.visualPrefabOnDish, point.position, point.rotation, true);
        }
    }

    private void SpawnFinalProduct(CookingRecipe recipe)
    {
        if (recipe == null || recipe.finalProductPrefab == null)
        {
            LogWarning("Cannot spawn final product. Final product prefab is null.");
            return;
        }

        Vector3 pos = finalProductSpawnPoint != null
            ? finalProductSpawnPoint.position
            : transform.position + Vector3.up;

        Quaternion rot = finalProductSpawnPoint != null
            ? finalProductSpawnPoint.rotation
            : Quaternion.identity;

        Log("Spawning final product: " + recipe.finalProductPrefab.name);
        SpawnObject(recipe.finalProductPrefab, pos, rot, false);
    }

    private void HandleFailedRecipe(string reason)
    {
        if (recipeEnded)
        {
            Log("Recipe already ended. Ignore failure: " + reason);
            return;
        }

        recipeEnded = true;

        LogWarning("RECIPE FAILED. Reason: " + reason);

        Vector3 pos = failedProductSpawnPoint != null
            ? failedProductSpawnPoint.position
            : transform.position + Vector3.up;

        Quaternion rot = failedProductSpawnPoint != null
            ? failedProductSpawnPoint.rotation
            : Quaternion.identity;

        if (failedProductPrefab != null)
        {
            Log("Spawning failed product: " + failedProductPrefab.name);
            SpawnObject(failedProductPrefab, pos, rot, false);
        }
        else
        {
            LogWarning("Failed Product Prefab is null. Nothing spawned.");
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

    private void SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, bool trackAsVisual)
    {
        if (prefab == null)
        {
            LogWarning("SpawnObject failed because prefab is null.");
            return;
        }

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Log("PhotonNetwork.Instantiate: " + prefab.name);

                GameObject obj = PhotonNetwork.Instantiate(prefab.name, position, rotation);

                if (trackAsVisual && obj != null)
                {
                    spawnedVisualObjects.Add(obj);
                }
            }
            else
            {
                Log("Not MasterClient. This client will not spawn object.");
            }
        }
        else
        {
            Log("Local Instantiate: " + prefab.name);

            GameObject obj = Instantiate(prefab, position, rotation);

            if (trackAsVisual && obj != null)
            {
                spawnedVisualObjects.Add(obj);
            }
        }
    }

    private void DestroyInputIngredient(GameObject obj)
    {
        if (obj == null)
            return;

        Log("Destroying input ingredient: " + obj.name);

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            PhotonView view = obj.GetComponent<PhotonView>();

            if (view != null)
            {
                if (view.IsMine)
                {
                    PhotonNetwork.Destroy(obj);
                }
                else
                {
                    LogWarning("Input ingredient PhotonView is not mine. Cannot destroy.");
                }
            }
            else
            {
                LogWarning("Input ingredient has no PhotonView. Using local Destroy.");
                Destroy(obj);
            }
        }
        else
        {
            Destroy(obj);
        }
    }

    private void DestroyCookingStationObject()
    {
        Log("DestroyCookingStationObject called.");

        DetachSpawnPointsBeforeDestroy();

        GameObject target = destroyTargetObject != null ? destroyTargetObject : gameObject;

        Log("Destroying CookingStation target object: " + target.name);

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            PhotonView view = target.GetComponent<PhotonView>();

            if (view != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(target);
                }
                else
                {
                    Log("Not MasterClient. Target object will not be destroyed by this client.");
                }
            }
            else
            {
                LogWarning("Destroy target has no PhotonView. Using local Destroy.");
                Destroy(target);
            }
        }
        else
        {
            Destroy(target);
        }
    }

    private void DetachSpawnPointsBeforeDestroy()
    {
        if (detachFinalProductSpawnPointBeforeDestroy && finalProductSpawnPoint != null)
        {
            Log("Detaching final product spawn point: " + finalProductSpawnPoint.name);
            finalProductSpawnPoint.SetParent(null);
        }

        if (detachFailedProductSpawnPointBeforeDestroy && failedProductSpawnPoint != null)
        {
            if (failedProductSpawnPoint != finalProductSpawnPoint)
            {
                Log("Detaching failed product spawn point: " + failedProductSpawnPoint.name);
                failedProductSpawnPoint.SetParent(null);
            }
        }

        if (detachIngredientVisualSpawnPointsBeforeDestroy)
        {
            DetachIngredientVisualSpawnPoints();
        }
    }

    private void DetachIngredientVisualSpawnPoints()
    {
        if (recipes == null)
            return;

        Log("Detaching ingredient visual spawn points.");

        HashSet<Transform> detachedPoints = new HashSet<Transform>();

        foreach (CookingRecipe recipe in recipes)
        {
            if (recipe == null || recipe.requiredIngredients == null)
                continue;

            foreach (IngredientRequirement req in recipe.requiredIngredients)
            {
                if (req == null || req.visualSpawnPoints == null)
                    continue;

                foreach (Transform point in req.visualSpawnPoints)
                {
                    if (point == null)
                        continue;

                    if (detachedPoints.Contains(point))
                        continue;

                    Log("Detaching ingredient visual spawn point: " + point.name);
                    point.SetParent(null);
                    detachedPoints.Add(point);
                }
            }
        }
    }

    private bool IsRecipeComplete(CookingRecipe recipe)
    {
        if (recipe == null)
            return false;

        if (recipe.requiredIngredients == null)
            return false;

        if (currentIngredientIds.Count != recipe.requiredIngredients.Count)
        {
            Log("Recipe incomplete. Current count: " + currentIngredientIds.Count + " / Required count: " + recipe.requiredIngredients.Count);
            return false;
        }

        if (recipe.useOrder)
        {
            for (int i = 0; i < recipe.requiredIngredients.Count; i++)
            {
                string requiredId = GetIngredientIdFromRequirement(recipe.requiredIngredients[i]);

                if (currentIngredientIds[i] != requiredId)
                {
                    Log("Recipe not complete because order mismatch at index " + i);
                    return false;
                }
            }

            return true;
        }
        else
        {
            Dictionary<string, int> requiredCounts = GetRequiredCounts(recipe);
            Dictionary<string, int> currentCounts = GetCurrentCounts();

            foreach (KeyValuePair<string, int> pair in requiredCounts)
            {
                int currentCount = currentCounts.ContainsKey(pair.Key) ? currentCounts[pair.Key] : 0;

                if (currentCount != pair.Value)
                {
                    Log("Recipe not complete. Ingredient " + pair.Key + " current " + currentCount + " / required " + pair.Value);
                    return false;
                }
            }

            return true;
        }
    }

    private IngredientRequirement FindRequirementById(CookingRecipe recipe, string ingredientId)
    {
        if (recipe == null || recipe.requiredIngredients == null)
            return null;

        foreach (IngredientRequirement req in recipe.requiredIngredients)
        {
            string reqId = GetIngredientIdFromRequirement(req);

            if (reqId == ingredientId)
            {
                return req;
            }
        }

        return null;
    }

    private string GetIngredientIdFromRequirement(IngredientRequirement req)
    {
        if (req == null)
            return "";

        return GetIngredientIdFromPrefab(req.ingredientPrefab);
    }

    private string GetIngredientIdFromPrefab(GameObject prefab)
    {
        if (prefab == null)
            return "";

        IngredientId id = prefab.GetComponent<IngredientId>();

        if (id == null)
            id = prefab.GetComponentInChildren<IngredientId>();

        if (id == null)
            return "";

        return id.ingredientId;
    }

    private int CountCurrentIngredient(string ingredientId)
    {
        int count = 0;

        foreach (string id in currentIngredientIds)
        {
            if (id == ingredientId)
                count++;
        }

        return count;
    }

    private int CountRequiredIngredient(CookingRecipe recipe, string ingredientId)
    {
        int count = 0;

        if (recipe == null || recipe.requiredIngredients == null)
            return count;

        foreach (IngredientRequirement req in recipe.requiredIngredients)
        {
            string reqId = GetIngredientIdFromRequirement(req);

            if (reqId == ingredientId)
                count++;
        }

        return count;
    }

    private Dictionary<string, int> GetRequiredCounts(CookingRecipe recipe)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        if (recipe == null || recipe.requiredIngredients == null)
            return counts;

        foreach (IngredientRequirement req in recipe.requiredIngredients)
        {
            string id = GetIngredientIdFromRequirement(req);

            if (string.IsNullOrEmpty(id))
                continue;

            if (!counts.ContainsKey(id))
                counts[id] = 0;

            counts[id]++;
        }

        return counts;
    }

    private Dictionary<string, int> GetCurrentCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (string id in currentIngredientIds)
        {
            if (!counts.ContainsKey(id))
                counts[id] = 0;

            counts[id]++;
        }

        return counts;
    }

    private void ClearProgress()
    {
        Log("Clearing recipe progress.");
        currentIngredientIds.Clear();
    }

    private void ClearSpawnedVisuals()
    {
        Log("Clearing spawned visual objects. Count: " + spawnedVisualObjects.Count);

        for (int i = spawnedVisualObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawnedVisualObjects[i];

            if (obj == null)
                continue;

            if (usePhotonSync && PhotonNetwork.IsConnected)
            {
                PhotonView view = obj.GetComponent<PhotonView>();

                if (view != null && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(obj);
                }
                else
                {
                    Destroy(obj);
                }
            }
            else
            {
                Destroy(obj);
            }
        }

        spawnedVisualObjects.Clear();
    }

    public void ResetStation()
    {
        Log("ResetStation called.");
        recipeEnded = false;
        ClearProgress();
        ClearSpawnedVisuals();
    }

    private string GetCurrentIngredientText()
    {
        if (currentIngredientIds == null || currentIngredientIds.Count == 0)
            return "Empty";

        return string.Join(", ", currentIngredientIds);
    }

    private string GetObjectName(GameObject obj)
    {
        if (obj == null)
            return "NULL";

        return obj.name;
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