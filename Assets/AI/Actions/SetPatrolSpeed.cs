using RAIN.Action;
using RAIN.Core;
using RAIN.Motion;
using RAIN.Representation;
using UnityEngine;

/// <summary>
/// SetPatrolSpeed is a RAIN behavior tree action that sets a move speed variable assuming the AI is patrolling
/// and wants to maintain its position in a formation.
/// To use it, create a custom action in your behavior tree and choose the "Set Patrol Speed" action.
/// Set CloseEnoughDistance to the distance from the target location the AI can be without moving
/// Set MaxDistance to the maximum distance from the target location the AI can be before it starts running
/// Set WalkSpeed to the walking speed of the AI
/// Set RunSpeed to the running speed of the AI
/// Set MoveTargetVariable to the name of the variable containing the target location being moved to
/// Set MoveSpeedVariable to the name of the variable the calculated speed will be assigned to.  This can be used
/// as the speed variable in a Move Action.
/// NOTE: this action will never return SUCCESS, and so should only be used inside a Parallel, usually in conjunction with
/// a Move node.
/// </summary>
[RAINAction("Set Patrol Speed")]
public class SetPatrolSpeed : RAINAction
{
    /// <summary>
    /// The close enough distance for movement, when running should turn to walking
    /// </summary>
    public Expression CloseEnoughDistance = new Expression();

    /// <summary>
    /// The max distance from the target, beyond which running should start
    /// </summary>
    public Expression MaxDistance = new Expression();

    /// <summary>
    /// Walk speed
    /// </summary>
    public Expression WalkSpeed = new Expression();

    /// <summary>
    /// Run speed
    /// </summary>
    public Expression RunSpeed = new Expression();

    /// <summary>
    /// A variable containing the target movement location.  This can be a gameObject, Vector3, etc.
    /// </summary>
    public Expression MoveTargetVariable = new Expression();

    /// <summary>
    /// The name of a variable that the calculated movement speed will be assigned to
    /// </summary>
    public Expression MoveSpeedVariable = new Expression();

    /// <summary>
    /// A MoveLookTarget that maintains a connection to the move target variable
    /// </summary>
    private MoveLookTarget _target = new MoveLookTarget();

    /// <summary>
    /// The _moveTargetVariableName is set on Start.  It is not re-evaluated each timestep.
    /// </summary>
    private string _moveTargetVariableName = null;

    /// <summary>
    /// The _moveSpeedVariableName is set on Start.  It is not re-evaluated each timestep.
    /// </summary>
    private string _moveSpeedVariableName = null;

    /// <summary>
    /// Track whether we are at our target (walking) or not (running to catch up)
    /// </summary>
    private bool _atTarget = false;

    /// <summary>
    /// Evaluate the move target and move speed variable names.  Create a MoveLookTarget to track the move target variable.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        _moveTargetVariableName = null; 
        if (MoveTargetVariable.IsValid)
        {
            if (MoveTargetVariable.IsVariable)
            {
                _moveTargetVariableName = MoveTargetVariable.VariableName;
            }
            else if (MoveTargetVariable.IsConstant)
            {
                _moveTargetVariableName = MoveTargetVariable.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);
            }
        }
        _target.SetVariableTarget(_moveTargetVariableName, ai.WorkingMemory);

        _moveSpeedVariableName = null;
        if (MoveSpeedVariable.IsValid)
        {
            if (MoveSpeedVariable.IsVariable)
            {
                _moveSpeedVariableName = MoveSpeedVariable.VariableName;
            }
            else if (MoveSpeedVariable.IsConstant)
            {
                _moveSpeedVariableName = MoveSpeedVariable.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);
            }
        }
    }

    /// <summary>
    /// Evaluate the distance, close enough distance, walk, and run speed each time.
    /// Calculate the distance to the target and choose between walking and running
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE the move target variable or move speed variable are invalid, RUNNING otherwise</returns>
    public override ActionResult Execute(AI ai)
    {
        //Fail if our variables are missing
        if (string.IsNullOrEmpty(_moveTargetVariableName))
            return ActionResult.FAILURE;
        if (string.IsNullOrEmpty(_moveSpeedVariableName))
            return ActionResult.FAILURE;

        //Max distance is either twice the regular close enough distance or the specified max distance
        float tMaxDistance = Mathf.Max(ai.Motor.DefaultCloseEnoughDistance * 2f, MaxDistance.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory));

        //Close enough distance is either the regular close enough distance or the specified close enough
        float tCloseEnoughDistance = Mathf.Max(ai.Motor.DefaultCloseEnoughDistance, CloseEnoughDistance.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory));

        //Walk speed must be evaluated, and could be 0
        float tWalkSpeed = WalkSpeed.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory);

        //Run speed must be evaluated, and could be 0
        float tRunSpeed = RunSpeed.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory);

        //Calculate the distance to the target position
        Vector3 tDistanceVector = ai.Kinematic.Position - _target.Position;
        tDistanceVector.y = 0f;
        float tDistance = tDistanceVector.magnitude;

        if (tDistance > tMaxDistance)
        {
            //We are further away than max distance, so start running
            _atTarget = false;
            ai.WorkingMemory.SetItem<float>(_moveSpeedVariableName, tRunSpeed);
        }
        else if (!_atTarget && (tDistance > tCloseEnoughDistance))
        {
            //We may be within max distance, but we don't stop running until we reach the target
            ai.WorkingMemory.SetItem<float>(_moveSpeedVariableName, tRunSpeed);
        }
        else
        {
            //We are at the target, so stop running and walk
            _atTarget = true;
            ai.WorkingMemory.SetItem<float>(_moveSpeedVariableName, tWalkSpeed);
        }

        return ActionResult.RUNNING;
    }
}