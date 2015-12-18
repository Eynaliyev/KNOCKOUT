using RAIN.Navigation;
using RAIN.Navigation.Pathfinding;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FormationHarness is the base class for all formation harnesses.  A formation harness has
/// a name, a number of positions (or slots), a number of occupants of those positions, and a simple
/// visualization
/// </summary>
public abstract class FormationHarness : MonoBehaviour 
{
    /// <summary>
    /// Positions are kept in an array, which is capped at cnstMaxPossiblePositions
    /// </summary>
	public const int cnstMaxPossiblePositions = 50;
	
    /// <summary>
    /// The harness name, sometimes used to identify a particular harness (like "attack")
    /// </summary>
	public string harnessName;

    /// <summary>
    /// The maximum number of positions available in the formation.  Should be capped under cnstMaxPossiblePositions
    /// </summary>
	public int maxPositions = 6;

    /// <summary>
    /// Does the formation rotate with its parent object, or does it stay fixed in axis aligned coordinates
    /// Attack harnesses normally don't rotate, while formation harnesses normally do
    /// </summary>
	public bool rotatesWithObject = false;
	
    /// <summary>
    /// Is visualization turned on
    /// </summary>
	public bool displayVisualization = false;

    /// <summary>
    /// Empty slot visualization color
    /// </summary>
	public Color emptyColor = Color.green;

    /// <summary>
    /// Occupied slot visualization color
    /// </summary>
	public Color occupiedColor = Color.red;
	
    /// <summary>
    /// An array of occupants.  Null means not occupied.  Not null means occupied
    /// </summary>
	public GameObject[] occupants = new GameObject[cnstMaxPossiblePositions];
	
    /// <summary>
    /// Initialize and clear the occupant array on start
    /// </summary>
	public void Start () 
	{
        Initialize();
	}

    /// <summary>
    /// Clamp max positions and clear the occupant array
    /// </summary>
    public virtual void Initialize()
    {
        maxPositions = Mathf.Clamp(maxPositions, 0, cnstMaxPossiblePositions);
        Clear();
    }

	/// <summary>
	/// Loop through the occupants list and set all to null.
	/// </summary>	
	public virtual void Clear()
	{
		for (int i = 0; i < occupants.Length; i++)
			occupants[i] = null;
	}
	
    /// <summary>
    /// Clear an occupant from the occupants list.  This checks all slots, just in case an occupant occupies more than one
    /// </summary>
    /// <param name="aOccupant">The occupant to clear</param>
	public virtual void Vacate(GameObject aOccupant)
	{
		for (int i = 0; i < occupants.Length; i++)
			if (occupants[i] == aOccupant) 
				occupants[i] = null;
	}

    /// <summary>
    /// Return the occupant of a slot
    /// </summary>
    /// <param name="aSlot">The slot to check</param>
    /// <returns>null if the slot is invalid, the current occupant otherwise (could be null)</returns>
    public virtual GameObject GetOccupant(int aSlot)
    {
        if ((aSlot < 0) || (aSlot > maxPositions))
            return null;

        return occupants[aSlot];
    }

    /// <summary>
    /// Determines whether a slot is occupied (or conversely available)
    /// </summary>
    /// <param name="aSlot">The slot to check</param>
    /// <returns>True if the slot is occupied, false if the slot is invalid or available</returns>
    public virtual bool IsOccupied(int aSlot)
    {
        if ((aSlot < 0) || (aSlot > maxPositions))
            return false;

        return (occupants[aSlot] != null);
    }
	
    /// <summary>
    /// OccupyClosestSlot attempts to find the closest (in distance) that is available.  If a Navigator is
    /// specified, a path check is done to make sure the slot is reachable
    /// </summary>
    /// <param name="aOccupant">The occupant for the slot</param>
    /// <param name="aSlot">An out parameter - returns the slot if assigned, or -1 if no slot could be assigned</param>
    /// <param name="aNavigator">An optional parameter.  If a RAINNavigator is specified then a reachability test will be performed</param>
    /// <returns>true if a slot was found, false otherwise</returns>
	public virtual bool OccupyClosestSlot(GameObject aOccupant, out int aSlot, RAINNavigator aNavigator = null)
	{
        //Clamp max positions since it is a public variable
		maxPositions = Mathf.Clamp(maxPositions, 0, cnstMaxPossiblePositions);

        //If the occupant already has a slot, just return that
		for (int i = 0; i < maxPositions; i++)
			if (occupants[i] == aOccupant)
			{
				aSlot = i;
				return true;
			}
		
        //Check all unoccupied slots.  For any that are reachable, find the one closest to the occupant
		float bestDistance = float.MaxValue;
		int bestSlot = -1;
		for (int i = 0; i < maxPositions; i++)
		{
            if (occupants[i] != null)
                continue;

			Vector3 slotPosition = GetSlotPosition(i);
            if (aNavigator != null)
            {
                RAINPath path;
                if (!aNavigator.GetPathTo(slotPosition, 100, 10, false, out path))
                    continue;
            }
			
			float distance = (aOccupant.transform.position - slotPosition).magnitude;
			if (distance < bestDistance)
			{
				bestDistance = distance;
				bestSlot = i;
			}
		}
		
        //Set the return value (aSlot)
		aSlot = bestSlot;
		if (bestSlot < 0)
			return false;
		
        //Occupy the slot
		occupants[bestSlot] = aOccupant;

        return true;
	}

    /// <summary>
    /// OccupyIntermediateSlot attempts to find the closest (in distance) that is available, and is closer to the
    /// occupant than the harness gameObject.  This is used to ensure slots that are behind or to the side
    /// of the harness are not chosen.  If a Navigator is specified, a path check is done to make sure the slot is reachable
    /// </summary>
    /// <param name="aOccupant">The occupant for the slot</param>
    /// <param name="aSlot">An out parameter - returns the slot if assigned, or -1 if no slot could be assigned</param>
    /// <param name="aNavigator">An optional parameter.  If a RAINNavigator is specified then a reachability test will be performed</param>
    /// <returns>true if a slot was found, false otherwise</returns>
    public virtual bool OccupyIntermediateSlot(GameObject aOccupant, out int aSlot, RAINNavigator aNavigator = null)
    {
        //Clamp max positions since it is a public variable
        maxPositions = Mathf.Clamp(maxPositions, 0, cnstMaxPossiblePositions);

        //If the occupant already has a slot, just return that
        for (int i = 0; i < maxPositions; i++)
            if (occupants[i] == aOccupant)
            {
                aSlot = i;
                return true;
            }

        //Check all unoccupied slots.  For any that are closer than distanceToGameObject and reachable,
        //find the one closest to the occupant
        float distanceToGameObject = Vector3.Distance(gameObject.transform.position, aOccupant.transform.position);
        float bestDistance = float.MaxValue;
        int bestSlot = -1;
        for (int i = 0; i < maxPositions; i++)
        {
            if (occupants[i] != null)
                continue;

            Vector3 slotPosition = GetSlotPosition(i);
            float distance = (aOccupant.transform.position - slotPosition).magnitude;
            if (distance > distanceToGameObject)
                continue;

            if (aNavigator != null)
            {
                RAINPath path;
                if (!aNavigator.GetPathTo(slotPosition, 100, 10, false, out path))
                    continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = i;
            }
        }

        //Set the return value (aSlot)
        aSlot = bestSlot;
        if (bestSlot < 0)
            return false;

        //Occupy the slot
        occupants[bestSlot] = aOccupant;

        return true;
    }

    /// <summary>
    /// OccupyFirstAvailableSlot cycles through all slots in slot order (0 to max) and occupies the first unoccupied slot.
    /// If a Navigator is specified, a path check is done to make sure the slot is reachable
    /// </summary>
    /// <param name="aOccupant">The occupant for the slot</param>
    /// <param name="aSlot">An out parameter - returns the slot if assigned, or -1 if no slot could be assigned</param>
    /// <param name="aNavigator">An optional parameter.  If a RAINNavigator is specified then a reachability test will be performed</param>
    /// <returns>true if a slot was found, false otherwise</returns>
    public virtual bool OccupyFirstAvailableSlot(GameObject aOccupant, out int aSlot, RAINNavigator aNavigator = null)
    {
        //Clamp max positions since it is a public variable
        maxPositions = Mathf.Clamp(maxPositions, 0, cnstMaxPossiblePositions);

        //If the occupant already has a slot, just return that
        for (int i = 0; i < maxPositions; i++)
            if (occupants[i] == aOccupant)
            {
                aSlot = i;
                return true;
            }

        //Stop at the first unoccupied and reachable slot
        aSlot = -1;
        for (int i = 0; i < maxPositions; i++)
        {
            if (occupants[i] != null)
                continue;

            Vector3 slotPosition = GetSlotPosition(i);
            if (aNavigator != null)
            {
                RAINPath path;
                if (!aNavigator.GetPathTo(slotPosition, 100, 10, false, out path))
                    continue;
            }

            //Set the return value (aSlot)
            aSlot = i;

            //Occupy the slot
            occupants[i] = aOccupant;
            break;
        }

        return (aSlot >= 0);
    }

    /// <summary>
    /// OccupySlot can be called to occupy a particular slot.  Reachability tests are not performed
    /// </summary>
    /// <param name="aOccupant">The occupant for the slot</param>
    /// <param name="aSlot">The slot to occupy</param>
    /// <returns></returns>
	public virtual bool OccupySlot(GameObject aOccupant, int aSlot)
	{
        //Clamp max positions since it is a public variable
        maxPositions = Mathf.Clamp(maxPositions, 0, cnstMaxPossiblePositions);

		if ((aSlot < 0) || (aSlot >= maxPositions))
			return false;
		
		if (occupants[aSlot] == null)
		{
			occupants[aSlot] = aOccupant;
			return true;
		} 
		else if (occupants[aSlot] == aOccupant) 
		{
			return true;
		}
		
		return false;
	}
	
    /// <summary>
    /// The actual slot position is specified by an overriding class.
    /// </summary>
    /// <param name="aSlot">The slot to get a world space position for</param>
    /// <returns>The world space position of the slot</returns>
	public abstract Vector3 GetSlotPosition (int aSlot);

    /// <summary>
    /// A property that can be overridden to provide formation modes
    /// </summary>
    public virtual string FormationMode { get; set; }
	
    /// <summary>
    /// Visualizer for the formation harness
    /// </summary>
	public virtual void OnDrawGizmos()
	{
		maxPositions = Mathf.Clamp(maxPositions, 0, cnstMaxPossiblePositions);

		if (!displayVisualization)
			return;
		
		if (maxPositions < 1)
			return;
		
		for (int i = 0; i < maxPositions; i++)
		{
			if (occupants[i] == null)
				Gizmos.color = emptyColor;
			else
				Gizmos.color = occupiedColor;
			
	        Gizmos.matrix = Matrix4x4.identity;
			Gizmos.DrawWireCube(GetSlotPosition(i), new Vector3(0.2f, 0.2f, 0.2f));		
		}
	}
}
