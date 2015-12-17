using UnityEngine;

/// <summary>
/// The triangle formation harness is a FormationHarness that supports a triangle (wedge) pattern
/// </summary>
public class TriangleFormationHarness : FormationHarness
{
    /// <summary>
    /// Position distance is the distance between triangle rows (front to back)
    /// </summary>
    public float positionDistance = 1f;

    /// <summary>
    /// Angle spread is the angle within which the triangle is filled
    /// </summary>
	public float angleSpread = 30f;

    /// <summary>
    /// Get slot position finds the Vector3 position in world coordinates based on a triangle (wedge)
    /// grid set back one positionDistance from the parent gameobject, starting on a row with 2 elements.
    /// Each row contains one more element vs the prior row.
    /// </summary>
    /// <param name="aSlot">The numeric position in the formation to return the position for</param>
    /// <returns>A vector3 position for the provided slot</returns>
	public override Vector3 GetSlotPosition (int aSlot)
	{
        //If an invalid slot is requested, then a spot directly behind the formation parent will be returned
        if ((aSlot < 0) || (aSlot >= maxPositions))
		{
			return gameObject.transform.position - (gameObject.transform.forward * positionDistance);
		}

        //Compute left and right angles in world coords
		float angleLeft = 180f - (angleSpread / 2f);
		float angleRight = 180f + (angleSpread / 2f);

        //If the triangle rotates, then add those angles
		if (rotatesWithObject)
		{
			angleLeft += gameObject.transform.rotation.eulerAngles.y;
			angleRight += gameObject.transform.rotation.eulerAngles.y;
		}

        //determine which row and which item in the row corresponds to the slot
		int row = 0;
		int rowCount = 2;
		int slot = aSlot;
		while (slot >= rowCount)
		{
			slot = slot - rowCount;
			row++;
			rowCount++; //each row increases in size by 1
		}

        //determine the distance along the outside angle.  this is the vertical distance of the entire row
		float distanceAlongAngle = ((float)row + 1) * positionDistance;
		float divisor = row + 1; //number of segments in the row

		Vector3 forwardVector = new Vector3(0f, 0f, distanceAlongAngle);
		Quaternion rotationLeft = Quaternion.Euler(new Vector3(0f, angleLeft, 0f));
		Quaternion rotationRight = Quaternion.Euler(new Vector3(0f, angleRight, 0f));

        //Calculate the position of the outer edges of the triangle
		Vector3 posLeft = gameObject.transform.position + (rotationLeft * forwardVector);
		Vector3 posRight = gameObject.transform.position + (rotationRight * forwardVector);

        //Calculate the slot position along the line between the two outer edges
		Vector3 returnValue = Vector3.Lerp (posLeft, posRight, slot / divisor);

		return returnValue;
	}
}
