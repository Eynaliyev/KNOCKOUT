using UnityEngine;

/// <summary>
/// The scattered radius formation harness is a FormationHarness intended to be used as an attack harness.
/// This creates semi-randomized positions at an average radius around the harness center.  The positions
/// are randomized at a scatter frequency.
/// </summary>
public class ScatteredRadiusFormationHarness : FormationHarness
{
    /// <summary>
    /// The radius of the formation, on average (not a guaranteed average)
    /// </summary>
    public float positionDistance = 1f;

    /// <summary>
    /// The weight of randomization, from 0 to 1.  0 will create a perfect circle
    /// </summary>
    public float scatterWeight = 0f;

    /// <summary>
    /// The frequency in seconds at which the harness will be randomized.  0 means no timed randomization
    /// </summary>
    public float scatterFrequency = 0f;

    /// <summary>
    /// Since the positions are only calculated on randomizing, we need to store them
    /// </summary>
    private Vector3[] positions = null;

    /// <summary>
    /// lastDistance is used to manage a manual update of positionDistance
    /// </summary>
    private float lastDistance = 0f;

    /// <summary>
    /// lastWeight is used to manage a manual update of lastWeight
    /// </summary>
    private float lastWeight = 0f;

    /// <summary>
    /// lastScatter is used to manage timing of random scattering
    /// </summary>
    private float lastScatter = 0f;

    /// <summary>
    /// In order to force a Scatter, simply null out the position array
    /// </summary>
    public void Scatter()
    {
        positions = null;
    }

    /// <summary>
    /// Get slot position finds the Vector3 position in world coordinates based on the last stored
    /// scattering.
    /// <param name="aSlot">The numeric position in the formation to return the position for</param>
    /// <returns>A vector3 position for the provided slot</returns>
    public override Vector3 GetSlotPosition(int aSlot)
    {
        //Clamp scatter weight between 0 (none) and 1 (max)
        scatterWeight = Mathf.Max(0f, Mathf.Min(1.0f, scatterWeight));

        //If we are scattering on a timer, check the timer and scatter if necessary
        if ((scatterFrequency > 0f) && ((Time.time - lastScatter) > scatterFrequency))
            Scatter();

        //If we don't have a valid position array or one of our parameters has changed, update the position array
        //with a new scatter
        if ((positions == null) || (positions.Length != maxPositions) ||
            (lastDistance != positionDistance) || (lastWeight != scatterWeight))
            UpdatePositions();

        //If the scatter failed, return a position in front of the harness
        if ((aSlot < 0) || (aSlot >= maxPositions) || (positions == null))
            return gameObject.transform.position + gameObject.transform.forward * positionDistance;

        //Grab the relative stored position.  Rotate it if necessary
        Vector3 vectorToPosition = positions[aSlot];
        if (rotatesWithObject)
            vectorToPosition = gameObject.transform.rotation * vectorToPosition;

        //Add the harness center to the relative position
        return gameObject.transform.position + vectorToPosition;
    }

    /// <summary>
    /// Update positions actually creates the scatter
    /// </summary>
    public virtual void UpdatePositions()
    {
        if (maxPositions < 1)
        {
            positions = null;
            return;
        }

        positions = new Vector3[maxPositions];

        //The scattering is done at the scatter radius +/- up to 80% depending on scatter weight
        float slice = 360f / maxPositions;
        float halfScatter = scatterWeight * positionDistance * 0.8f;
        float angleScatter = 0.5f * scatterWeight;

        //compute an angle offset from the normal angle slice (i.e., offset from even spacing)
        //compute the position based on angle and our weighted random radius
        for (int i = 0; i < maxPositions; i++)
        {
            float angle = (slice * (i + UnityEngine.Random.Range(-angleScatter, angleScatter)));
            positions[i] = Quaternion.Euler(new Vector3(0f, angle, 0f)) * (Vector3.forward * (positionDistance + UnityEngine.Random.Range(-halfScatter, halfScatter)));
        }

        //store distance, weight, and scatter time
        lastDistance = positionDistance;
        lastWeight = scatterWeight;
        lastScatter = Time.time;
    }
}
