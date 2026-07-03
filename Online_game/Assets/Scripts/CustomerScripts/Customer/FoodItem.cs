using UnityEngine;

public enum FoodType
{
    None,

    [InspectorName("Burger 1")]
    Burger1,

    [InspectorName("Burger 2")]
    Burger2,

    [InspectorName("Burger 3")]
    Burger3,

    [InspectorName("Burger 4")]
    Burger4,

    [InspectorName("Pizza 1")]
    Pizza1,

    [InspectorName("Pizza 2")]
    Pizza2,

    [InspectorName("Pizza 3")]
    Pizza3,

    [InspectorName("Hotdog 1")]
    Hotdog1,

    [InspectorName("Hotdog 2")]
    Hotdog2,

    [InspectorName("Hotdog 3")]
    Hotdog3,

    [InspectorName("Pancake 1")]
    Pancake1,

    [InspectorName("Pancake 2")]
    Pancake2,

    [InspectorName("Pancake 3")]
    Pancake3,

    [InspectorName("Pancake 4")]
    Pancake4
}

public class FoodItem : MonoBehaviour
{
    [Header("Food Type ID")]
    public FoodType foodType;

    [Header("Prefab ID used for customer checking")]
    public GameObject foodPrefabId;
    // Drag the original prefab from the Project folder here.
    // This is what CustomerOrderUI uses to check whether the served food is correct.

    [Header("Pickup Settings")]
    public bool canBePickedUp = true;

    [HideInInspector] public bool isHeld = false;
}