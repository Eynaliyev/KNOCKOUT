using UnityEngine;

/// <summary>
/// AIObjective is a Unity component used to mark objects that can be owned or "occupied" by AI
/// For example, cover points are objectives that can't be occupied by more than one occupant at a time.
/// The AIObjective class manages occupying and vacating
/// </summary>
public class AIObjective : MonoBehaviour 
{
    /// <summary>
    /// Occupant is the current "owner" of the objective.  It is serialized so that it shows up and can be
    /// set through the Unity Editor
    /// </summary>
    [SerializeField]
    private GameObject occupant;

    /// <summary>
    /// Returns true if the objective is currently occupied, false otherwise
    /// </summary>
    public virtual bool IsOccupied
    {
        get
        {
            if (occupant == null)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Returns the game object associated with the current occupant
    /// </summary>
    public virtual GameObject Occupant
    {
        get { return occupant; }
    }

    /// <summary>
    /// Occupy attempts to assign the current occupant of the objective. 
    /// </summary>
    /// <param name="aOccupant">The occupant to assign</param>
    /// <param name="aForce">If true, the new occupant is assigned even the objective is already occupied</param>
    /// <returns>true if the occupant was assigned and now occupies the objective, false otherwise</returns>
    public virtual bool Occupy(GameObject aOccupant, bool aForce = false)
    {
        if ((occupant == null) || aForce)
            occupant = aOccupant;

        return (occupant == aOccupant);
    }

    /// <summary>
    /// If the passed in occupant currently occupies the objective, it is cleared and will no longer occupy the objective
    /// </summary>
    /// <param name="aOccupant">The occupant to clear</param>
    public virtual void Vacate(GameObject aOccupant)
    {
        if (occupant == aOccupant)
            occupant = null;
    }
}
