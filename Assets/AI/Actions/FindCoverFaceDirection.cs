using RAIN.Action;
using RAIN.Core;
using RAIN.Motion;
using RAIN.Navigation.Pathfinding;
using RAIN.Representation;
using RAIN.Perception.Sensors;
using UnityEngine;

/// <summary>
/// FindCoverFaceDirection is a RAIN behavior tree action that sets the face direction of a soldier in cover based on
/// the most likely path the soldier would take to reach the enemy.
/// To use it, create a custom action in your behavior tree and choose the "Find Cover Face Direction" action.
/// Set EnemyVariable to the name of the variable containing the enemy or enemy position.
/// Set FaceVariable to the name of the variable you want to assign the face target to.  This variable can
/// then be used in a Move action as the Face Target.
/// </summary>
[RAINAction("Find Cover Face Direction")]
public class FindCoverFaceDirection : RAINAction
{
    /// <summary>
    /// The name of the variable that contains the enemy position (gameObject, Vector3, etc.)
    /// </summary>
    public Expression EnemyVariable = new Expression();

    /// <summary>
    /// The name of the variable that will contain the Vector3 face target
    /// </summary>
    public Expression FaceVariable = new Expression();

    /// <summary>
    /// We cache the cover sensor so we don't have to do a GetSensor call each time this action runs
    /// </summary>
    private TacticalSensor _tacticalSensor = null;

    /// <summary>
    /// Start finds and stores the tactical sensor
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        //Use a sensor named Tactical Sensor.
        if (_tacticalSensor == null)
            _tacticalSensor = ai.Senses.GetSensor("Tactical Sensor") as TacticalSensor;
    }

    /// <summary>
    /// When executing, this action tries to pathfind toward the enemy position.  If successful, the
    /// face target is set to the first step of the path.  Otherwise it is set toward the enemy directly.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE if either variable is missing, SUCCESS otherwise</returns>
    public override ActionResult Execute(AI ai)
    {
        //If the enemy variable is missing, fail
        if (!EnemyVariable.IsValid || !EnemyVariable.IsVariable)
            return ActionResult.FAILURE;

        //If the face variable is missing, fail
        if (!FaceVariable.IsValid || !FaceVariable.IsVariable)
            return ActionResult.FAILURE;

        Vector3 facePosition;

        //Grab the enemy position as a move look target.  This will convert from Vector, GameObject, and other types
        MoveLookTarget enemyPosition = MoveLookTarget.GetTargetFromVariable(ai.WorkingMemory, EnemyVariable.VariableName, ai.Motor.CloseEnoughDistance);
        if ((enemyPosition == null) || (!enemyPosition.IsValid))
        {
            //No enemy, so face the nearest threat area
            TacticalAspect tAspect = FindThreatArea(ai);
            if (tAspect != null)
                facePosition = tAspect.Position;
            else
            {
                ai.WorkingMemory.RemoveItem(FaceVariable.VariableName);
                return ActionResult.SUCCESS;
            }
        }
        else
        {
            facePosition = enemyPosition.Position;
        }

        //Calculate a path to the enemy.  If found, look toward our next movement waypoint
        RAINPath path;
        ai.Navigator.GetPathTo(facePosition, 100, 100, ai.Motor.AllowOffGraphMovement, out path);
        if ((path != null) && (path.IsValid))
            facePosition = path.GetWaypointPosition(1);

        ai.WorkingMemory.SetItem<Vector3>(FaceVariable.VariableName, facePosition);

        return ActionResult.SUCCESS;
    }

    /// <summary>
    /// Find the nearest tactical threatarea.  Use the tactical sensor to find nearby areas, then choose the closest.
    /// To find the best cover point, we will compute a cost for every unoccupied cover point within our
    /// cover point sensor.  Then choose the cover point with the lowest cost.  We normally prefer cover points
    /// that are close to the enemy, but also close to our AI.  For example, we prefer points that are very near
    /// the enemy, but then also prefer points that are between us and the enemy (not on the other side of them).
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private TacticalAspect FindThreatArea(AI ai)
    {
        TacticalAspect tAspect = null;

        //Threat areas should be marked with the "threatarea" tactical aspect
        _tacticalSensor.Sense("threatarea", RAINSensor.MatchType.ALL);

        float bestCost = float.MaxValue;
        for (int i = 0; i < _tacticalSensor.Matches.Count; i++)
        {
            TacticalAspect tCandidateAspect = _tacticalSensor.Matches[i] as TacticalAspect;
            if (tCandidateAspect == null)
                continue;

            float tCost = Vector3.Distance(ai.Kinematic.Position, tCandidateAspect.Position);
            if (tCost < bestCost)
            {
                tAspect = tCandidateAspect;
                bestCost = tCost;
            }
        }

        return tAspect;
    }
}