using RAIN.Action;
using RAIN.Core;
using RAIN.Entities.Aspects;
using RAIN.Motion;
using RAIN.Perception.Sensors;
using RAIN.Representation;
using UnityEngine;

/// <summary>
/// FindCoverPoint is a RAIN behavior tree action that finds a nearby cover point.  Much like FindAttackFormationPosition,
/// this action will manage ownership of the cover point while running and vacate the spot on Stop.
/// To use it, create a custom action in your behavior tree and choose the "Find Cover Point" action.
/// Set EnemyVariable to the name of the variable containing the enemy or enemy position.
/// Set CoverVariable to the name of the variable you want to assign the cover point location to.  This variable can
/// then be used in a Move action as the Move Target.
/// </summary>
[RAINAction("Find Cover Point")]
public class FindCoverPoint : RAINAction
{
    /// <summary>
    /// The name of the variable that contains the enemy position (gameObject, Vector3, etc.)
    /// </summary>
    public Expression EnemyVariable = new Expression();

    /// <summary>
    /// The name of the variable that will contain the MoveLookTarget cover position
    /// </summary>
    public Expression CoverVariable = new Expression();

    /// <summary>
    /// We cache the cover sensor so we don't have to do a GetSensor call each time this action runs
    /// </summary>
    private TacticalSensor _coverSensor = null;

    /// <summary>
    /// Store the current cover point so it can be released on Stop
    /// </summary>
    private AIObjective _currentCoverPoint = null;

    /// <summary>
    /// Start does all the work for allocating the cover point, since it only happens once
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        //Use a sensor named Tactical Sensor.  We could change this to an input Expression
        //if we want the FindCoverPoint action to be a little more re-useable
        if (_coverSensor == null)
            _coverSensor = ai.Senses.GetSensor("Tactical Sensor") as TacticalSensor;

        //Reset and make sure we don't accidentally already have a reserved cover point
        Vacate(ai);

        //Do the work
        FindBestCoverPoint(ai);
    }

    /// <summary>
    /// Executing this action just checks to make sure we have a valid cover point that we still own.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE if we don't have a valid cover point.  Otherwise this action will continue to
    /// return RUNNING.  This means you should use it in a Parallel only.</returns>
    public override ActionResult Execute(AI ai)
    {
        if ((_currentCoverPoint == null) || (_currentCoverPoint.Occupant != ai.Body))
            return ActionResult.FAILURE;

        return ActionResult.RUNNING;
    }

    /// <summary>
    /// Release the cover point on Stop
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Stop(AI ai)
    {
        //Vacate will release the cover point
        Vacate(ai);

        base.Stop(ai);
    }

    /// <summary>
    /// To find the best cover point, we will compute a cost for every unoccupied cover point within our
    /// cover point sensor.  Then choose the cover point with the lowest cost.  We normally prefer cover points
    /// that are close to the enemy, but also close to our AI.  For example, we prefer points that are very near
    /// the enemy, but then also prefer points that are between us and the enemy (not on the other side of them).
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private void FindBestCoverPoint(AI ai)
    {
        if (!CoverVariable.IsValid || !CoverVariable.IsVariable)
            return;

        //We don't actually have to have an enemy.  We can choose cover points that are simply near ourselves.
        Vector3 tEnemyPosition = ai.Kinematic.Position;
        if (EnemyVariable.IsValid && EnemyVariable.IsVariable)
        {
            //using a MoveLookTarget lets us automatically convert between gameObject, Vector3, etc., when getting
            //a position from AI memory
            MoveLookTarget tTarget = MoveLookTarget.GetTargetFromVariable(ai.WorkingMemory, EnemyVariable.VariableName, ai.Motor.CloseEnoughDistance);
            if (tTarget.IsValid)
                tEnemyPosition = tTarget.Position;
        }

        //Cover points should be marked with the "cover" tactical aspect
        _coverSensor.Sense("cover", RAINSensor.MatchType.ALL);

        //Default cover point is our own position
        float bestCost = float.MaxValue;
        Vector3 tCoverPosition = ai.Kinematic.Position;
        for (int i = 0; i < _coverSensor.Matches.Count; i++)
        {
            TacticalAspect tCandidateAspect = _coverSensor.Matches[i] as TacticalAspect;
            if (tCandidateAspect == null)
                continue;

            //Cover points are AIObjectives, which are objects that allow AI to "occupy" them
            AIObjective tObjective = tCandidateAspect.Entity.Form.GetComponent<AIObjective>();

            //Skip occupied cover points
            if ((tObjective == null) || (tObjective.IsOccupied))
                continue;

            //Our cost function gives 50% more weight to points near the enemy
            //But then also adds in the AI distance to the point
            float tCost = 1.5f * Vector3.Distance(tEnemyPosition, tCandidateAspect.Position) + Vector3.Distance(ai.Kinematic.Position, tCandidateAspect.Position);
            if (tCost < bestCost)
            {
                _currentCoverPoint = tObjective;
                tCoverPosition = tCandidateAspect.Position;
                bestCost = tCost;
            }
        }

        //If we found a cover point, then occupy it
        if (_currentCoverPoint != null)
            _currentCoverPoint.Occupy(ai.Body);

        //Set the cover position in AI memory
        ai.WorkingMemory.SetItem<Vector3>(CoverVariable.VariableName, tCoverPosition);
    }

    /// <summary>
    /// If we have a cover point, vacate it and set our cover point to null
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private void Vacate(AI ai)
    {
        if (_currentCoverPoint != null)
            _currentCoverPoint.Vacate(ai.Body);

        _currentCoverPoint = null;
    }
}