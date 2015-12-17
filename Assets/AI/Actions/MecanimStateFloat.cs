using RAIN.Action;
using RAIN.Animation;
using RAIN.Core;
using RAIN.Representation;
using UnityEngine;

/// <summary>
/// MecanimStateFloat is a RAIN behavior tree action that manages the state of a float parameter in the
/// Mecanim State Machine.
/// To use it, create a custom action in your behavior tree and choose the "Maintain Mecanim Float Param" action.
/// Set MecanimParameter to the name of the mecanim parameter
/// Set StartValue to the starting value that should be assigned when the action executes
/// Set StartDampTime to the damping time for reaching the StartValue
/// Set StopValue to the value that should be assigned when the action stops.
/// NOTE: This action will always return RUNNING, and therefore should only be used in a Parallel
/// </summary>
[RAINAction("Maintain Mecanim Float Param")]
public class MecanimStateFloat : RAINAction
{
    /// <summary>
    /// The name of the Mecanim Parameter to manage
    /// </summary>
    public Expression MecanimParameter = new Expression();

    /// <summary>
    /// The starting value to set the Mecanim Parameter To
    /// </summary>
    public Expression StartValue = new Expression();

    /// <summary>
    /// The Start Value damp time
    /// </summary>
    public Expression StartDampTime = new Expression();

    /// <summary>
    /// The stopping value to set when stopping
    /// </summary>
    public Expression StopValue = new Expression();

    /// <summary>
    /// Grab the mecanimAnimator from the AI so we don't have to keep re-casting it all the time
    /// </summary>
    private MecanimAnimator _mecanimAnimator = null;

    /// <summary>
    /// Store the mecanim parameter once evaluated
    /// </summary>
    private string _mecanimParameter = null;

    /// <summary>
    /// Convert the parameter hash string just once at the beginning
    /// </summary>
    private int _mecanimHash = 0;

    /// <summary>
    /// Total damp time 
    /// </summary>
    private float _totalDampTime = 0f;

    /// <summary>
    /// Damp time remaining
    /// </summary>
    private float _dampTimeRemaining = 0f;

    /// <summary>
    /// Starting damping value
    /// </summary>
    private float _startDampValue = 0f;

    /// <summary>
    /// Ending damping value
    /// </summary>
    private float _endDampValue = 0f;

    /// <summary>
    /// Start evaluates the various initial values for mecanim and stores them
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);
        if (MecanimParameter.IsValid)
        {
            if (MecanimParameter.IsVariable)
                _mecanimParameter = MecanimParameter.VariableName;
            else
                _mecanimParameter = MecanimParameter.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);
        }

        _mecanimAnimator = ai.Animator as MecanimAnimator;
        if ((_mecanimParameter != null) && (_mecanimAnimator != null))
        {
            _mecanimHash = Animator.StringToHash(_mecanimParameter);
            _startDampValue = _mecanimAnimator.UnityAnimator.GetFloat(_mecanimHash);
            _endDampValue = StartValue.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory);
            _totalDampTime = StartDampTime.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory);
            _dampTimeRemaining = _totalDampTime;
        }
    }

    /// <summary>
    /// Execute maintains the start value of the parameter.  The value is ramped from the actual parameter value
    /// to the evaluated Start value.  After that the Start value is preserved until the action is stopped.
    /// </summary>
    /// <param name="ai"></param>
    /// <returns>ActionResult.RUNNING, this action should always be used in a parallel</returns>
    public override ActionResult Execute(AI ai)
    {
        //Lerp from start to end damp values
        if ((_mecanimParameter != null) && (_mecanimAnimator != null))
        {
            _mecanimAnimator.UnityAnimator.SetFloat(_mecanimHash, Mathf.Lerp(_startDampValue, _endDampValue, 1.0f - (_dampTimeRemaining / _totalDampTime)));
        }

        //update damp time if any remains
        if (_dampTimeRemaining > 0)
        {
            _dampTimeRemaining -= ai.DeltaTime;
            if (_dampTimeRemaining < 0f)
                _dampTimeRemaining = 0f;
        }

        return ActionResult.RUNNING;
    }

    /// <summary>
    /// Stop evaluates the Stop value and sets it
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Stop(AI ai)
    {
        if ((_mecanimParameter != null) && (_mecanimAnimator != null))
        {
            float tValue = StopValue.Evaluate<float>(ai.DeltaTime, ai.WorkingMemory);
            _mecanimAnimator.UnityAnimator.SetFloat(_mecanimHash, tValue);
        }
        base.Stop(ai);
    }
}