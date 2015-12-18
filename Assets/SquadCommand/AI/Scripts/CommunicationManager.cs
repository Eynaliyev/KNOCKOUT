using System.Collections.Generic;

/// <summary>
/// CommunicatonManager is a global static class that tracks channels and listeners, and manages distributing
/// messages broadcast or sent directly across communication channels.  This class works with the CommunicationElement
/// class for AI
/// </summary>
public class CommunicationManager 
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    private static CommunicationManager instance = new CommunicationManager();

    /// <summary>
    /// Private constructor
    /// </summary>
    private CommunicationManager() { }

    /// <summary>
    /// Public singleton instance accessor
    /// </summary>
    public static CommunicationManager Instance
    {
        get 
        {
            if (instance == null)
                instance = new CommunicationManager();
            return instance; 
        }
    }

    /// <summary>
    /// The list of channels and associated subscribers
    /// </summary>
    private Dictionary<string, List<CommunicationElement>> subscribers = new Dictionary<string,List<CommunicationElement>>();

    /// <summary>
    /// Called by subscribers to listen to all broadcasts on a channel
    /// </summary>
    /// <param name="aChannel">The name of the channel to susbscribe to.  These can be totally ad-hoc.</param>
    /// <param name="aSubscriber">The CommunicationElement of the subscriber</param>
    public void Subscribe(string aChannel, CommunicationElement aSubscriber)
    {
        if (string.IsNullOrEmpty(aChannel) || (aSubscriber == null))
            return;

        List<CommunicationElement> tElements;
        subscribers.TryGetValue(aChannel, out tElements);

        if (tElements == null)
            tElements = new List<CommunicationElement>();

        if (!tElements.Contains(aSubscriber))
            tElements.Add(aSubscriber);

        subscribers[aChannel] = tElements;
    }

    /// <summary>
    /// Called by channel subscribers to stop receiving broadcasts on a channel
    /// </summary>
    /// <param name="aChannel">The name of the channel to unsubscribe from</param>
    /// <param name="aSubscriber">The CommunicationElement of the AI unsubscribing</param>
    public void Unsubscribe(string aChannel, CommunicationElement aSubscriber)
    {
        if (string.IsNullOrEmpty(aChannel) || (aSubscriber == null))
            return;
        List<CommunicationElement> tElements;
        subscribers.TryGetValue(aChannel, out tElements);
        if (tElements != null)
            tElements.Remove(aSubscriber);
    }

    /// <summary>
    /// A public call to broadcast a message on a channel.  It is not necessary to be a subscriber (or even an AI)
    /// in order to broadcast a message
    /// </summary>
    /// <param name="aChannel">The name of the channel to broadcast on</param>
    /// <param name="aVariableName">The name of the variable recipients will assign values to</param>
    /// <param name="aValue">The value recipients will assign to the variable</param>
    public void Broadcast(string aChannel, string aVariableName, object aValue)
    {
        if (string.IsNullOrEmpty(aChannel))
            return;
        if (string.IsNullOrEmpty(aVariableName))
            return;

        //Check to see if the channel exists.  If it doesn't then it has no subscribers
        List<CommunicationElement> tElements;
        subscribers.TryGetValue(aChannel, out tElements);
        if (tElements == null)
            return;

        //Call ReceiveMessage on all subscribers
        for (int i = 0; i < tElements.Count; i++)
        {
            CommunicationElement tElement = tElements[i];
            if (tElement != null)
                tElement.ReceiveMessage(aChannel, aVariableName, aValue);
        }
    }

    /// <summary>
    /// SendTo can be used to send a message directly to a receiving AI.  When this method is called, the receiver
    /// will get a ReceiveDirectMessage callback.
    /// </summary>
    /// <param name="aSender">The CommunicationElement of the sender</param>
    /// <param name="aReceiver">The CommunicationElement of the receiver</param>
    /// <param name="aChannel">The channel to send a message on.  This is just to support context for the receiver.
    /// The receiver does not need to be a subscriber.</param>
    /// <param name="aVariableName">The name of the variable the receiver will assign values to</param>
    /// <param name="aValue">The value the receiver will assign to the variable</param>
    public void SendTo(CommunicationElement aSender, CommunicationElement aReceiver, string aChannel, string aVariableName, object aValue)
    {
        if (aReceiver != null)
            aReceiver.ReceiveDirectMessage(aSender, aChannel, aVariableName, aValue);
    }
}
