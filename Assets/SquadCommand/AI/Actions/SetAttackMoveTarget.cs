using RAIN.Action;
using RAIN.Core;
using RAIN.Representation;
using UnityEngine;

/// <summary>
/// SetAttackMoveTarget is a RAIN behavior tree action that tries to find an attack location associated
/// with an enemy, based on using an enemy attack harness.
/// To use it, create a custom action in your behavior tree and choose the "Set Attack Move Target" action.
/// Set MoveTargetVariable to the name of the variable you want to assign the move location to.  This variable can
/// then be used in a Move action.
/// Set FirstOrClosest to one of "first", "closest", or "intermediate" to indicate how position selection should work.
/// Note that this action will never return SUCCESS, and so should only be used inside a Parallel.  This is because
/// the action is responsible for retrieving, maintaining, and eventually releasing its harness slot.
/// </summary>
[RAINAction("Set Attack Move Target")]
public class SetAttackMoveTarget : RAINAction
{
    /// <summary>
    /// The name of the variable that will contain the Vector3 move location
    /// </summary>
    public Expression MoveTargetVariable = new Expression();

    /// <summary>
    /// A string value indicating how position selection should work
    /// "first" (default) will attempt to take the first slot available, as determined by the attack harness
    /// "closest" will attempt to take the closest unoccupied slot in the attack harness
    /// "intermediate" will only take harness positions that are nearer to the AI than the enemy is.
    /// </summary>
    public Expression FirstOrClosest = new Expression();

    /// <summary>
    /// The enemy is set on Start and retained during subsequent execute calls
    /// </summary>
    private GameObject _enemy = null;

    /// <summary>
    /// The _moveTargetVariableName is set on Start.  It is not re-evaluated each timestep.
    /// </summary>
    private string _moveTargetVariableName = null;

    /// <summary>
    /// The harness associated with the enemy.  A harness is added automatically if none exists.
    /// </summary>
    private FormationHarness _harness = null;

    /// <summary>
    /// The occupied slot on the harness, which must be released on Stop
    /// </summary>
    private int _slot = -1;

    /// <summary>
    /// Set up the enemy and the harness.  Set up the move target variable
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        SetEnemy(ai);

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
    }

    /// <summary>
    /// When executing, this action checks to make sure the enemy and attack harness are still valid.
    /// If a valid harness does not exist, then the AI either remains in place, or moves to the last known
    /// enemy position.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE if a slot could not be assigned, RUNNING otherwise</returns>
    public override ActionResult Execute(AI ai)
    {
        if (string.IsNullOrEmpty(_moveTargetVariableName))
            return ActionResult.FAILURE;

        //If the harness is inactive, try to reset it from the enemy
        if ((_harness == null) || (!_harness.gameObject.activeInHierarchy))
            SetEnemy(ai);

        //Default movement is our own current position
        Vector3 targetPosition = ai.Kinematic.Position;

        //If the harness is still missing, set the position based on last known enemy position
        if (_harness != null)
        {
            //If somehow the harness doesn't agree that we own the slot we think we do, then release it
            //and try again
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

        }

        //Assign our position if we have a slot
        if (_slot >= 0)
            targetPosition = _harness.GetSlotPosition(_slot);
        else
        {
            if (ai.WorkingMemory.ItemExists("enemyPosition"))
                targetPosition = ai.WorkingMemory.GetItem<Vector3>("enemyPosition") - ai.Body.transform.forward;
        }

        //set a position in memory no matter what
        ai.WorkingMemory.SetItem<Vector3>(_moveTargetVariableName, targetPosition);
        
        return ActionResult.RUNNING;
    }

    /// <summary>
    /// We must release our harness slot before exiting
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Stop(AI ai)
    {
        Vacate(ai);
        base.Stop(ai);
    }

    /// <summary>
    /// Get the enemy from AI memory.  If we have a new enemy, or if we have a bad harness
    /// then attempt to find a new harness
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>true if the enemy changed, false otherwise</returns>
    private bool SetEnemy(AI ai)
    {
        bool tEnemyChanged = false;
        GameObject tEnemy = ai.WorkingMemory.GetItem<GameObject>("enemy");

        if (_enemy != tEnemy)
        {
            _enemy = tEnemy;
            SwapHarness(ai);
            tEnemyChanged = true;
        }
        if ((_harness == null) || (!_harness.gameObject.activeInHierarchy))
            SwapHarness(ai);

        return tEnemyChanged;
    }

    /// <summary>
    /// Vacate our current harness, releasing our slot.  Attempt to find a new harness with the name "attack"
    /// If no harness is found, add a new ScatteredRadiusTarget and use that
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private void SwapHarness(AI ai)
    {
        Vacate(ai);
        _harness = null;
        if (_enemy == null)
            return;

        FormationHarness[] tHarnesses = _enemy.GetComponentsInChildren<FormationHarness>();
        for (int i = 0; i < tHarnesses.Length; i++)
        {
            if (tHarnesses[i].harnessName == "attack")
            {
                _harness = tHarnesses[i];
                return;
            }
        }

        //If there isn't one already on the enemy, add one
        ScatteredRadiusFormationHarness tNewHarness = _enemy.AddComponent<ScatteredRadiusFormationHarness>();
        tNewHarness.harnessName = "attack";
        tNewHarness.maxPositions = 10;
        tNewHarness.positionDistance = 12f;
        tNewHarness.scatterWeight = 0.5f;
        tNewHarness.scatterFrequency = 10f;
        _harness = tNewHarness;
    }

    /// <summary>
    /// If we have an attack harness, vacate any reserved slots
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    private void Vacate(AI ai)
    {
        _slot = -1;
        if (_harness != null)
            _harness.Vacate(ai.Body);
    }
}