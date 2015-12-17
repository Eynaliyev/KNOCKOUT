using RAIN.Action;
using RAIN.Core;
using RAIN.Representation;
using UnityEngine;

/// <summary>
/// SetFormationMoveTarget is a RAIN behavior tree action that sets a move location by finding and then
/// following a position in a formation harness attached to a commander.
/// To use it, create a custom action in your behavior tree and choose the "Set Formation Move Target" action.
/// Set Commander to a variable containing the commander game object.
/// Set MoveTargetVariable to the name of the variable you want to assign the move location to.  This variable can
/// then be used in a Move action.
/// Set FaceTargetVariable to the name of the variable you want to assign the face location to.  This variable can
/// then be used in a Move action to keep the AI facing the right way while moving.
/// Set FirstOrClosest to one of "first", "closest", or "intermediate" to indicate how position selection should work.
/// Note that this action will never return SUCCESS, and so should only be used inside a Parallel.  This is because
/// the action is responsible for retrieving, maintaining, and eventually releasing its harness slot.
/// </summary>
[RAINAction("Set Formation Move Target")]
public class SetFormationMoveTarget : RAINAction
{
    /// <summary>
    /// A variable that contains the commander game object
    /// </summary>
    public Expression Commander = new Expression();

    /// <summary>
    /// The name of the variable that will contain the Vector3 move location
    /// </summary>
    public Expression MoveTargetVariable = new Expression();

    /// <summary>
    /// The name of the variable that will contain the Vector3 face toward location
    /// </summary>
    public Expression FaceTargetVariable = new Expression();

    /// <summary>
    /// A string value indicating how position selection should work
    /// "first" (default) will attempt to take the first slot available, as determined by the attack harness
    /// "closest" will attempt to take the closest unoccupied slot in the attack harness
    /// "intermediate" will only take harness positions that are nearer to the AI than the enemy is.
    /// </summary>
    public Expression FirstOrClosest = new Expression();

    /// <summary>
    /// The commander game object
    /// </summary>
    private GameObject _commander = null;

    /// <summary>
    /// The _moveTargetVariableName is set on Start.  It is not re-evaluated each timestep.
    /// </summary>
    private string _moveTargetVariableName = null;

    /// <summary>
    /// The _faceTargetVariableName is set on Start.  It is not re-evaluated each timestep.
    /// </summary>
    private string _faceTargetVariableName = null;

    /// <summary>
    /// The current active formation harness on the commander
    /// </summary>
    private FormationHarness _harness = null;

    /// <summary>
    /// The occupied slot on the harness, which must be released on Stop
    /// </summary>
    private int _slot = -1;

    /// <summary>
    /// Setup the commander and the attack harness.  Evaluate and store the move and face target variable names
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        SetCommander(ai);

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
        _faceTargetVariableName = null;
        if (FaceTargetVariable.IsValid)
        {
            if (FaceTargetVariable.IsVariable)
            {
                _faceTargetVariableName = FaceTargetVariable.VariableName;
            }
            else if (FaceTargetVariable.IsConstant)
            {
                _faceTargetVariableName = FaceTargetVariable.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);
            }
        }
    }

    /// <summary>
    /// When executing, this action checks to make sure the commander and attack harness are still valid.
    /// If a slot is not found, the action fails.
    /// The action will set the move and face position relative to the slot position on the formation
    /// harness, and also relative to the facing direction of the AI body, the facing direction of the harness,
    /// and the whether the location is in front or behind the AI.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE if a slot could not be assigned, RUNNING otherwise</returns>
    public override ActionResult Execute(AI ai)
    {
        //If the move target variable name is invalid, there's no purpose in running so just fail
        if (string.IsNullOrEmpty(_moveTargetVariableName))
            return ActionResult.FAILURE;

        //Validate the current commander and set the formation harness from it
        SetCommander(ai);
        if (_harness == null)
            return ActionResult.FAILURE;

        //Double check that the harness agrees that the AI still occupies its slot
        //Vacate if they don't match
        if (_harness.GetOccupant(_slot) != ai.Body)
        {
            Vacate(ai);
        }


        //Grab a slot if we don't have one
        if (_slot < 0)
        {
            string slotType = null;
            if (FirstOrClosest.IsValid)
                slotType = FirstOrClosest.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory).ToLower();

            if (slotType == "closest")
                _harness.OccupyClosestSlot(ai.Body, out _slot, ai.Navigator);
            else if (slotType == "intermediate")
                _harness.OccupyIntermediateSlot(ai.Body, out _slot, ai.Navigator);
            else
                _harness.OccupyFirstAvailableSlot(ai.Body, out _slot, ai.Navigator);
        }

        if (_slot >= 0)
        {
            //Get the slot position
            Vector3 movePosition = _harness.GetSlotPosition(_slot);

            //Calculate a dot product between the harness forward and the body forward to determine if they
            //are heading in the same direction
            float dotHarness = Vector3.Dot(_harness.gameObject.transform.forward, ai.Body.transform.forward);

            //Calculate a dot product between the direction to the slot location and the body forward to determine
            //if it is in front or behind the body
            float dotPosition = Vector3.Dot(movePosition - ai.Body.transform.position, ai.Body.transform.forward);

            //if the harness is going in the same direction as the AI
            //and the position is behind the AI
            //then don't turn when moving but instead face in the same direction as the harness
            if (!string.IsNullOrEmpty(_faceTargetVariableName))
            {
                if (dotHarness >= 0 && (dotPosition < 0))
                {
                    ai.WorkingMemory.SetItem<Vector3>(_faceTargetVariableName, ai.Body.transform.position + _harness.gameObject.transform.forward);
                }
                else
                {
                    ai.WorkingMemory.SetItem<Vector3>(_faceTargetVariableName, ai.Body.transform.position + _harness.gameObject.transform.forward);
//                    ai.WorkingMemory.RemoveItem(_faceTargetVariableName);
                }
            }

            //Set the move position to the slot position
            ai.WorkingMemory.SetItem<Vector3>(_moveTargetVariableName, movePosition);
        }
        else
            return ActionResult.FAILURE; //Fail if a slot was not assigned

        return ActionResult.RUNNING;
    }

    /// <summary>
    /// Vacate our harness slot when Stopping
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Stop(AI ai)
    {
        Vacate(ai);
        base.Stop(ai);
    }

    /// <summary>
    /// Get the commander from AI memory.  If we have a new commander, or if we have a bad harness
    /// then attempt to find a new harness
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>true if the commander changed, false otherwise</returns>
    private bool SetCommander(AI ai)
    {
        bool tCommanderChanged = false;
        GameObject tCommander = null;

        if (Commander.IsValid)
            tCommander = Commander.Evaluate<GameObject>(ai.DeltaTime, ai.WorkingMemory);
        if (_commander != tCommander)
        {
            _commander = tCommander;
            SwapHarness(ai);
            tCommanderChanged = true;
        }
        if ((_harness == null) || (!_harness.gameObject.activeInHierarchy))
            SwapHarness(ai);

        return tCommanderChanged;
    }

    /// <summary>
    /// Vacate our current harness, releasing our slot.  Attempt to find a new harness from the commander's formation harness
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private void SwapHarness(AI ai)
    {
        if (_commander == null)
        {
            Vacate(ai);
            _harness = null;
            return;
        }

        FormationHarness tNewHarness = _commander.GetComponentInChildren<FormationHarness>();
        if (_harness != tNewHarness)
        {
            Vacate(ai);
            _harness = tNewHarness;
        }
    }

    /// <summary>
    /// If we have a formation harness, vacate any reserved slots
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private void Vacate(AI ai)
    {
        _slot = -1;
        if (_harness != null)
            _harness.Vacate(ai.Body);
    }
}