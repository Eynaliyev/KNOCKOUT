using UnityEngine;

/// <summary>
/// Damage is a struct that is passed as part of the Unity message when sending damage Hits
/// </summary>
public struct Damage
{
    /// <summary>
    /// The GameObject associated with the damage giver
    /// </summary>
    public GameObject damageFrom;

    /// <summary>
    /// The raycast hit on the object being damaged.  This helps determine hit location for adding effects
    /// </summary>
    public RaycastHit hit;

    /// <summary>
    /// The amount of damage given
    /// </summary>
    public float damage;
}
