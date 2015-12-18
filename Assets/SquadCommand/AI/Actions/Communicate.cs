using RAIN.Action;
using RAIN.Core;
using RAIN.Representation;

/// <summary>
/// Communicate is a RAIN behavior tree action associated with the Communication System
/// To use it, create a custom action in your behavior tree and choose the "Communicate" action.
/// Set Channel to the communication channel you want to send messages to
/// Set VariableName to the name of the variable that will be assigned in the receiver's memory
/// Set Value to a valid expression containing the value of the message (e.g., a game object, string, number, etc.)
/// </summary>
[RAINAction("Communicate")]
public class Communicate : RAINAction
{
    /// <summary>
    /// The Communication System channel to broadcast the message to
    /// </summary>
    public Expression Channel = new Expression();

    /// <summary>
    /// The name of the RAIN memory variable that will be set on the receiver
    /// </summary>
    public Expression VariableName = new Expression();

    /// <summary>
    /// The value that will be assigned to the variable (VariableName)
    /// </summary>
    public Expression Value = new Expression();

    /// <summary>
    /// When executed, this action will set up a message based on the current channel, variable name, and message value
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>ActionResult.SUCCESS</returns>
    public override ActionResult Execute(AI ai)
    {
        string tChannel = null;
        string tVariable = null;
        object tValue = null;

        if (Channel.IsValid)
            tChannel = Channel.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);

        if (VariableName.IsValid)
        {
            if (VariableName.IsVariable)
                tVariable = VariableName.VariableName;
            else
                tVariable = VariableName.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);
        }

        tValue = Value.Evaluate<object>(ai.DeltaTime, ai.WorkingMemory);

        CommunicationManager.Instance.Broadcast(tChannel, tVariable, tValue);

        // And success
        return ActionResult.SUCCESS;
    }
}