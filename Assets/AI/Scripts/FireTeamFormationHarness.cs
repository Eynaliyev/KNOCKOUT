using UnityEngine;

/// <summary>
/// Fire Team formations approximated from information found here: http://www.tpub.com/seabee/3.htm
/// The fire team formation is a four man harness based on the Formation Harness and includes
/// Wedge, Column, Skirmish, and Echelon formations
/// </summary>
public class FireTeamFormationHarness : FormationHarness
{
    /// <summary>
    /// Formation types.  See the URL in the header for more info
    /// </summary>
    public enum FireTeamFormationType
    {
        Wedge,
        Column,
        SkirmishLeft,
        SkirmishRight,
        EchelonLeft,
        EchelonRight
    }

    /// <summary>
    /// Roles are only currently used as a way to request a slot based on the AI type
    /// </summary>
    public enum FireTeamRole
    {
        Leader,
        Automatic,
        Rifleman
    }

    /// <summary>
    /// The fire team formation must have a single leader.  This can be assigned statically in the Unity editor
    /// </summary>
    public GameObject leader;

    /// <summary>
    /// Position distance is a distance factor that helps determine how spread out the formation is, both
    /// vertically and horizontally
    /// </summary>
    public float positionDistance = 1f;

    /// <summary>
    /// Default formation type is Wedge
    /// </summary>
    public FireTeamFormationType formationType = FireTeamFormationType.Wedge;

    /// <summary>
    /// Wedge constant is used to shape the wedge
    /// </summary>
    public const float cnstWedgeForwardConstant = 1.732f; //sqrt(3) to form an equilateral wedge

    /// <summary>
    /// Column constant is used to space the distance of the forward position
    /// </summary>
    public const float cnstColumnForwardConstant = 1.5f;

    /// <summary>
    /// Echelon constant is used to determine the vertical spread in the echelon formmation
    /// </summary>
    public const float cnstEchelonForwardConstant = 0.75f;

    /// <summary>
    /// lastForward is used to save the orientation of the formation when not rotating the formation
    /// </summary>
    private Vector3 lastForward;

    /// <summary>
    /// lastRight is used to save the orientation of the formation when not rotating the formation
    /// </summary>
    private Vector3 lastRight;

    /// <summary>
    /// On awake, max positions is reset to 4.  If a leader is preset, they are assigned to the leader slot
    /// automatically.
    /// </summary>
    public void Awake()
    {
        maxPositions = 4;
        if (leader != null)
        {
            OccupySlotForRole(leader, FireTeamRole.Leader);
        }
        lastForward = gameObject.transform.forward;
        lastRight = gameObject.transform.right;
    }

    /// <summary>
    /// Update stores lastForward and lastRight
    /// </summary>
    public void Update()
    {
        if (rotatesWithObject)
        {
            lastForward = gameObject.transform.forward;
            lastRight = gameObject.transform.right;
        }
    }

    /// <summary>
    /// Clear causes the formation to be cleared, but then reassigns the lader
    /// </summary>
    public override void Clear()
    {
        base.Clear();
        if (leader != null)
        {
            OccupySlotForRole(leader, FireTeamRole.Leader);
        }
    }

    /// <summary>
    /// Get slot for role converts from a FireTeamRole to a slot number, which is returned
    /// </summary>
    /// <param name="aRole">The FireTeamRole (rifleman, leader, automatic) to assume</param>
    /// <returns>The position of the specified fireteam role.  For rifleman, this is the first open rifleman position</returns>
    public int GetSlotForRole(FireTeamRole aRole)
    {
        //In the fire team, the following always hold regardless of formation type:
        // Rifleman 2 = 0
        // Leader = 1
        // Automatic = 2
        // Rifleman 1 = 3
        if (aRole == FireTeamRole.Leader)
            return 1;
        if (aRole == FireTeamRole.Automatic)
            return 2;
        if (aRole == FireTeamRole.Rifleman)
        {
            if (!IsOccupied(3))
                return 3;
            if (!IsOccupied(0))
                return 0;
        }

        return -1;
    }

    /// <summary>
    /// Occupy slot for role first gets a slot number based on a role, then occupies the slot
    /// </summary>
    /// <param name="aOccupant">The new occupant</param>
    /// <param name="aRole">The role to occupy</param>
    /// <returns>true if a slot was occupied successfully, false otherwise</returns>
    public bool OccupySlotForRole(GameObject aOccupant, FireTeamRole aRole)
    {
        return OccupySlot(aOccupant, GetSlotForRole(aRole));
    }

    /// <summary>
    /// Formation mode accessor.  Returns all lowercase string versions of the formation types.  When set, this
    /// causes the formation to change automatically.
    /// </summary>
    public override string FormationMode
    {
        get
        {
            switch (formationType)
            {
                case FireTeamFormationType.Wedge:
                    return "wedge";
                case FireTeamFormationType.Column:
                    return "column";
                case FireTeamFormationType.SkirmishLeft:
                    return "skirmish left";
                case FireTeamFormationType.SkirmishRight:
                    return "skirmish right";
                case FireTeamFormationType.EchelonLeft:
                    return "echelon left";
                case FireTeamFormationType.EchelonRight:
                    return "echelon right";
            }
            return null;
        }
        set
        {
            if (value == "wedge")
                formationType = FireTeamFormationType.Wedge;
            else if (value == "column")
                formationType = FireTeamFormationType.Column;
            else if (value == "skirmish left")
                formationType = FireTeamFormationType.SkirmishLeft;
            else if (value == "skirmish right")
                formationType = FireTeamFormationType.SkirmishRight;
            else if (value == "echelon left")
                formationType = FireTeamFormationType.EchelonLeft;
            else if (value == "echelon right")
                formationType = FireTeamFormationType.EchelonRight;
        }
    }

    /// <summary>
    /// GetSlotPosition determines the global position of a slot based on the formation mode, the slot number, and
    /// the position of the formation
    /// </summary>
    /// <param name="aSlot">The slot number to get the position for</param>
    /// <returns>A Vector3 position.  If the slot is invalid, this returns a position directly behind the formation parent</returns>
    public override Vector3 GetSlotPosition(int aSlot)
    {
        //default (failure) case is directly behind the parent object
        Vector3 returnValue = gameObject.transform.position - (lastForward * positionDistance);

        maxPositions = 4;
        if ((aSlot < 0) || (aSlot >= maxPositions))
        {
            return returnValue;
        }

        //Leader always returns the mount point of the formation (which is usually the leader itself)
        if (aSlot == 1)
            return gameObject.transform.position;

        //Determine a position based on the current formation type
        switch (formationType)
        {
            case FireTeamFormationType.Column:
                {
                    if (aSlot == 0) //Rifleman 2 is forward and to the right
                    {
                        returnValue = gameObject.transform.position + 
                                      (lastRight * positionDistance * cnstColumnForwardConstant * 0.5f) +
                                      (lastForward * positionDistance * cnstColumnForwardConstant);
                    }
                    else if (aSlot == 2) //Automatic is to the right and back
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance * cnstColumnForwardConstant) -
                                      (lastForward * positionDistance * 0.5f);
                    }
                    else if (aSlot == 3) //Rifleman 1 is back and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance * cnstColumnForwardConstant * 0.5f) -
                                      (lastForward * positionDistance * (cnstColumnForwardConstant + 0.5f));

                    }
                    break;
                }
            case FireTeamFormationType.Wedge:
                {
                    if (aSlot == 0) //Rifleman 2 is forward and to the right
                    {
                        returnValue = gameObject.transform.position + 
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance * cnstWedgeForwardConstant);
                    }
                    else if (aSlot == 2) //Automatic is to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance * 2f);
                    }
                    else if (aSlot == 3) //Rifleman 1 is back and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance) -
                                      (lastForward * positionDistance);

                    }
                    break;
                }
            case FireTeamFormationType.SkirmishLeft:
                {
                    if (aSlot == 0) //Rifleman 2 is forward and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance);
                    }
                    else if (aSlot == 2) //Automatic is forward and to the left
                    {
                        returnValue = gameObject.transform.position -
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance * 0.5f);
                    }
                    else if (aSlot == 3) //Rifleman 1 is back and to the left
                    {
                        returnValue = gameObject.transform.position -
                                      (lastRight * positionDistance * 2f) -
                                      (lastForward * positionDistance);

                    }
                    break;
                }
            case FireTeamFormationType.SkirmishRight:
                {
                    if (aSlot == 0) //Rifleman 2 is forward and to the left
                    {
                        returnValue = gameObject.transform.position -
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance);
                    }
                    else if (aSlot == 2) //Automatic is forward and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance * 0.5f);
                    }
                    else if (aSlot == 3) //Rifleman 1 is back and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance * 2f) -
                                      (lastForward * positionDistance);

                    }
                    break;
                }
            case FireTeamFormationType.EchelonLeft:
                {
                    if (aSlot == 0) //Rifleman 2 is forward and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance * cnstEchelonForwardConstant);
                    }
                    else if (aSlot == 2) //Automatic is back and to the left
                    {
                        returnValue = gameObject.transform.position -
                                      (lastRight * positionDistance) -
                                      (lastForward * positionDistance * cnstEchelonForwardConstant);
                    }
                    else if (aSlot == 3) //Rifleman 1 is back and to the left
                    {
                        returnValue = gameObject.transform.position -
                                      (lastRight * positionDistance * 2f) -
                                      (lastForward * positionDistance * cnstEchelonForwardConstant * 2f);

                    }
                    break;
                }
            case FireTeamFormationType.EchelonRight:
                {
                    if (aSlot == 0) //Rifleman 2 is forward and to the left
                    {
                        returnValue = gameObject.transform.position -
                                      (lastRight * positionDistance) +
                                      (lastForward * positionDistance * cnstEchelonForwardConstant);
                    }
                    else if (aSlot == 2) //Automatic is back and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance) -
                                      (lastForward * positionDistance * cnstEchelonForwardConstant);
                    }
                    else if (aSlot == 3) //Rifleman 1 is back and to the right
                    {
                        returnValue = gameObject.transform.position +
                                      (lastRight * positionDistance * 2f) -
                                      (lastForward * positionDistance * cnstEchelonForwardConstant * 2f);

                    }
                    break;
                }
        }

        return returnValue;
    }
}
