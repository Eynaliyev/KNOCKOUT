using RAIN.Core;
using RAIN.Serialization;
using UnityEngine;

/// <summary>
/// HealthElement is a CustomAIElement that appears in the Custom tab of a RAIN AI Rig
/// This Element is responsible for managing managing health, damage, threat, death, and regeneration.
/// </summary>
[RAINSerializableClass, RAINElement("Health")]
public class HealthElement : CustomAIElement
{
    /// <summary>
    /// The name of the default variable set in AI memory to represent current health
    /// </summary>
    public const string cnstDefaultHealthVariable = "health";

    /// <summary>
    /// The name of the default variable set in AI memory to represent death state
    /// </summary>
    public const string cnstDefaultDeathVariable = "dead";

    /// <summary>
    /// The name of the default variable set in AI memory to represent a threat
    /// </summary>
    public const string cnstDefaultThreatVariable = "threat";

    /// <summary>
    /// The name of the variable set in AI memory to represent current health (float)
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Variable to set for current health")]
    private string healthVariable = cnstDefaultHealthVariable;

    /// <summary>
    /// The name of the variable set in AI memory to represent death state (bool)
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Variable to set on death")]
    private string deathVariable = cnstDefaultDeathVariable;

    /// <summary>
    /// The name of the variable set in AI memory to represent current threat (game object)
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Variable to set for current threat")]
    private string threatVariable = cnstDefaultThreatVariable;

    /// <summary>
    /// Current health, usually capped between min and max
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Current/Starting Health")]
    private float currentHealth = 100f;

    /// <summary>
    /// Max health
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Maximum Health")]
    private float maxHealth = 100f;

    /// <summary>
    /// Min health (less than or equal means death)
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Minimum Health before death")]
    private float minHealth = 0f;

    /// <summary>
    /// Delay in seconds before regeneration starts.  This delay is reset whenever damage is taken
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Health Regeneration Delay in seconds")]
    private float regenDelay = 5f;

    /// <summary>
    /// Health value regenerated per second during regeneration
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Health Regeneraton Rate")]
    private float regenRate = 2f;

    /// <summary>
    /// The frequency in seconds at which the current threat is updated
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Threat Refresh Delay in seconds")]
    private float threatDelay = 3f;

    /// <summary>
    /// The name of the variable to set when broadcasting threat information
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "If set, threats will be sent to the team channel using this variable name")]
    private string broadcastThreatVariable = null;

    /// <summary>
    /// Current regeneration delay time remaining
    /// </summary>
    private float _regenTime = 0f;

    /// <summary>
    /// Current threat delay time remaining
    /// </summary>
    private float _threatTime = 0f;

    /// <summary>
    /// Current threat (recent damage giver)
    /// </summary>
    private GameObject _threat = null;

    /// <summary>
    /// Current health
    /// </summary>
    public float CurrentHealth
    {
        get { return currentHealth; }
        set 
        { 
            currentHealth = value;
            if (Initialized && !string.IsNullOrEmpty(healthVariable))
                AI.WorkingMemory.SetItem<float>(healthVariable, currentHealth);
        }
    }

    /// <summary>
    /// Max health
    /// </summary>
    public float MaxHealth
    {
        get { return maxHealth; }
        set { maxHealth = value; }
    }

    /// <summary>
    /// Min health (less than or equal means death)
    /// </summary>
    public float MinHealth
    {
        get { return minHealth; }
        set { minHealth = value; }
    }

    /// <summary>
    /// Delay in seconds before regeneration starts.  This delay is reset whenever damage is taken
    /// </summary>
    public float RegenerationDelay
    {
        get { return regenDelay; }
        set { regenDelay = value; }
    }

    /// <summary>
    /// Health value regenerated per second during regeneration
    /// </summary>
    public float RegenerationRate
    {
        get { return regenRate; }
        set { regenRate = value; }
    }

    /// <summary>
    /// The frequency in seconds at which the current threat is updated
    /// </summary>
    public float ThreatRefreshDelay
    {
        get { return threatDelay; }
        set { threatDelay = value; }
    }

    /// <summary>
    /// An AI initialization method that occurs after the body is set.  Here we set up a proxy on the AI Body
    /// for receiving Damage messages from other objects via Unity messages
    /// </summary>
    public override void BodyInit()
    {
        base.BodyInit();

        DamageProxy proxy = AI.Body.GetComponent<DamageProxy>();
        if (proxy == null)
            proxy = AI.Body.AddComponent<DamageProxy>();

        proxy.healthElement = this;
    }

    /// <summary>
    /// Set up current health and remove death setting from AI memory on Start
    /// </summary>
    public override void Start()
    {
        CurrentHealth = Mathf.Max(MinHealth, Mathf.Min(CurrentHealth, MaxHealth));

        if (!string.IsNullOrEmpty(deathVariable))
            AI.WorkingMemory.RemoveItem(deathVariable);
    }

    /// <summary>
    /// Check for death and regeneration.  Update and broadcast threat information if needed
    /// </summary>
    public override void Act()
    {
        //Check for death
        if (CurrentHealth <= MinHealth)
        {
            if (!string.IsNullOrEmpty(deathVariable))
                AI.WorkingMemory.SetItem<bool>(deathVariable, true);
            return;
        }

        //Regenerate if needed
        if (_regenTime <= 0f)
            CurrentHealth = Mathf.Max(MinHealth, Mathf.Min(CurrentHealth + (RegenerationRate * AI.DeltaTime), MaxHealth));
        else
            _regenTime -= AI.DeltaTime;

        //Update threat information if needed
        if ((_threatTime <= 0f) && (_threat != null))
        {
            //Clear threat information
            _threat = null;
            if (!string.IsNullOrEmpty(broadcastThreatVariable))
            {
                string channel = AI.WorkingMemory.GetItem<string>("teamComm");
                CommunicationManager.Instance.Broadcast(channel, broadcastThreatVariable, _threat);
            }
            if (!string.IsNullOrEmpty(threatVariable))
                AI.WorkingMemory.SetItem<GameObject>(threatVariable, _threat);
        }
        else
            _threatTime -= AI.DeltaTime;
    }

    /// <summary>
    /// ReceiveDamage is the callback received from the DamageProxy to indicate damage received
    /// </summary>
    /// <param name="aDamage">A struct containing damage info</param>
    public void ReceiveDamage(Damage aDamage)
    {
        //Check to see if the damage came from an AI shooter
        AIRig tShooter = aDamage.damageFrom.GetComponentInChildren<AIRig>();

        // No friendly fire, so ignore damage from the same team
        if (tShooter != null)
        {
            TeamElement tMyTeam = AI.GetCustomElement<TeamElement>();
            TeamElement tShooterTeam = tShooter.AI.GetCustomElement<TeamElement>();

            if (tShooterTeam != null && tMyTeam != null && tShooterTeam.Team == tMyTeam.Team)
                return;
        }

        // Otherwise take damage
        //Reset the regeneration delay and reduce current health
        _regenTime = RegenerationDelay;
        CurrentHealth = Mathf.Max(MinHealth, Mathf.Min(CurrentHealth - aDamage.damage, MaxHealth));

        // Send threat information based on who shot us
        if (_threat == null)
        {
            _threat = aDamage.damageFrom;
            _threatTime = ThreatRefreshDelay;

            if (!string.IsNullOrEmpty(broadcastThreatVariable))
            {
                string channel = AI.WorkingMemory.GetItem<string>("teamComm");
                CommunicationManager.Instance.Broadcast(channel, broadcastThreatVariable, _threat);
            }

            if (!string.IsNullOrEmpty(threatVariable))
                AI.WorkingMemory.SetItem<GameObject>(threatVariable, _threat);
        }
    }
}
