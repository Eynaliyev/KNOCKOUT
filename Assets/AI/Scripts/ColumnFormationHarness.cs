using UnityEngine;

/// <summary>
/// The column formation harness is a FormationHarness that supports rectangular grids.
/// </summary>
public class ColumnFormationHarness : FormationHarness
{
    /// <summary>
    /// Number of columns is the width of the formation.  Depth is determined by max occupants
    /// </summary>
    public int numberOfColumns = 3;

    /// <summary>
    /// Position spread is the distance between units side by side
    /// </summary>
    public float positionSpread = 1f;

    /// <summary>
    /// Position distance is the distance between units front to back
    /// </summary>
	public float positionDistance = 1f;

    /// <summary>
    /// Get slot position finds the Vector3 position in world coordinates based on a rectangular
    /// grid set back one positionDistance from the parent gameobject.  Slots are numbered sequentially
    /// from upper left to lower right increasing across rows (e.g., row 1 contains positions 0,1,2,...)
    /// </summary>
    /// <param name="aSlot">The numeric position in the formation to return the position for</param>
    /// <returns>A vector3 position for the provided slot</returns>
	public override Vector3 GetSlotPosition (int aSlot)
	{
        //Number of columns can't be 0 or less
        if (numberOfColumns <= 0)
            numberOfColumns = 1;

        //If an invalid slot is requested, then a spot directly behind the formation parent will be returned
		if ((aSlot < 0) || (aSlot >= maxPositions))
		{
			return gameObject.transform.position - (gameObject.transform.forward * positionDistance);
		}

        //Determine the row/column of the requested slot
        int column = aSlot % numberOfColumns;
        int row = aSlot / numberOfColumns;

        //Center the formation around the parent
        float xLeft = 0f - (((float)(numberOfColumns - 1)) / 2f) * positionSpread;
        float x = xLeft + (column * positionSpread);
        float z = 0f - ((row + 1) * positionDistance);

        //Returnn the parent + the calculated offset, rotate it first if necessary
        Vector3 returnValue = gameObject.transform.position + new Vector3(x, 0f, z);
        if (rotatesWithObject)
            returnValue = gameObject.transform.position + gameObject.transform.rotation * new Vector3(x, 0f, z);

        return returnValue;
    }
}
