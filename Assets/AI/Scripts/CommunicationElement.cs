using RAIN.Core;
using RAIN.Serialization;
using System.Collections.Generic;

/// <summary>
/// CommunicationElement is a CustomAIElement that appears in the Custom tab of a RAIN AI Rig
/// This Element is responsible for defining communication channels the AI is listening to, 
/// receiving communications sent to the listened channels, and assigning incoming values into AI memory
/// </summary>
[RAINSerializableClass, RAINElement("Communication Manager")]
public class CommunicationElement : CustomAIElement
{
    /// <summary>
    /// The list of channels to listen to
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip="Channels to receive messages from")]
    private List<string> listenToChannels = new List<string>();

    /// <summary>
    /// Channel list accessor
    /// </summary>
    public List<string> Channels
    {
        get { return listenToChannels; }
    }

    /// <summary>
    /// On AIInit the channel subscriptions are set up
    /// </summary>
    public override void AIInit()
    {
        base.AIInit();
        for (int i = 0; i < listenToChannels.Count; i++)
            SubscribeTo(listenToChannels[i]);
    }

    /// <summary>
    /// Remove the AI as a listener from a channel
    /// </summary>
    /// <param name="aChannel">The channel to stop listening to</param>
    public virtual void UnsubscribeFrom(string aChannel)
    {
        listenToChannels.Remove(aChannel);
        CommunicationManager.Instance.Unsubscribe(aChannel, this);
    }

    /// <summary>
    /// Add the AI as a listener to a channel
    /// </summary>
    /// <param name="aChannel">The channel to listen to</param>
    public virtual void SubscribeTo(string aChannel)
    {
        listenToChannels.Add(aChannel);
        CommunicationManager.Instance.Subscribe(aChannel, this);
    }

    /// <summary>
    /// ReceiveMessage is a callback received from the master CommunicationManager
    /// The message always consists of a memory variable to assign to and a value to assign
    /// </summary>
    /// <param name="aChannel">The channel on which the communication is sent</param>
    /// <param name="aVariableName">A memory variable to assign</param>
    /// <param name="aValue">The value to assign to the memory variable</param>
    public virtual void ReceiveMessage(string aChannel, string aVariableName, object aValue)
    {
        AI.WorkingMemory.SetItem<object>(aVariableName, aValue);
    }

    /// <summary>
    /// ReceiveDirectMessage is a message that can be sent directly between AI rather than broadcast to the
    /// entire channel
    /// </summary>
    /// <param name="aElement">The CommunicationElement of the sender</param>
    /// <param name="aChannel">The channel on which the communication is sent</param>
    /// <param name="aVariableName">A memory variable to assign</param>
    /// <param name="aValue">The value to assign to the memory variable</param>
    public virtual void ReceiveDirectMessage(CommunicationElement aElement, string aChannel, string aVariableName, object aValue)
    {
        AI.WorkingMemory.SetItem<object>(aVariableName, aValue);
    }

    /// <summary>
    /// Send a message out to all listeners on a channel
    /// </summary>
    /// <param name="aChannel">The channel on which the communication is sent</param>
    /// <param name="aVariableName">A memory variable to assign</param>
    /// <param name="aValue">The value to assign to the memory variable</param>
    public virtual void BroadcastMessage(string aChannel, string aVariableName, object aValue)
    {
        CommunicationManager.Instance.Broadcast(aChannel, aVariableName, aValue);
    }
}
