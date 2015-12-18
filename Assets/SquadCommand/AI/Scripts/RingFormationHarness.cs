using UnityEngine;

/// <summary>
/// The ring formation harness is a FormationHarness that supports a circle of positions around the formation center
/// </summary>
public class RingFormationHarness : FormationHarness
{
    /// <summary>
    /// Position distance is the radius of the ring
    /// </summary>
	public float positionDistance = 1f;

    /// <summary>
    /// Get slot position finds the Vector3 position in world coordinates based on a ring of positions
    /// centered around the parent game object.
    /// </summary>
    /// <param name="aSlot">The numeric position in the formation to return the position for</param>
    /// <returns>A vector3 position for the provided slot</returns>
	public override Vector3 GetSlotPosition (int aSlot)
	{
        //If the slot is invalid, then choose a positioni in front of the harness
		if ((aSlot < 0) || (aSlot >= maxPositions))
		{
			return gameObject.transform.position + gameObject.transform.forward * positionDistance;
		}
		
        //Compute the separation angle between each of the slots
		float angle = (360f / maxPositions) * (float)aSlot;

        //Adjust the angle by parent rotation if necessary
		if (rotatesWithObject)
			angle += gameObject.transform.rotation.eulerAngles.y;

        //Create a quaternion to represent the rotation and a vector to represent a radius vector
        Quaternion tRotation = Quaternion.AngleAxis(angle, Vector3.up);
        Vector3 tRadiusVector = gameObject.transform.forward * positionDistance;

        //Add the rotated radius vector to the harness center
        Vector3 returnValue = gameObject.transform.position + (tRotation * tRadiusVector);
		
		return returnValue;
	}
}
