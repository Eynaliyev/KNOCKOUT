using RAIN.Core;
using RAIN.Serialization;
using System.Collections.Generic;

/// <summary>
/// FormationHarnessElement is a CustomAIElement that appears in the Custom tab of a RAIN AI Rig
/// This Element is responsible for managing formations that have been associated with an AI.  
/// To associate a formation, create a game object parented to the AI Body that has an attached
/// FormationHarness component.  Name the game object to match the name of the harness.  Set formation
/// parameters (spread, distance, max positions, etc.) on the formation.  The FormationHarnessElement will
/// create a list of all associated formations on BodyInit (an AI initialization call) and then manage their
/// activation.
/// </summary>
[RAINSerializableClass, RAINElement("Formation Manager")]
public class FormationHarnessElement : CustomAIElement 
{
    /// <summary>
    /// A list of attached harnesses
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show)]
    private List<FormationHarness> harnesses = new List<FormationHarness>();

    /// <summary>
    /// The name of the current active harness
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show)]
    private string currentHarness;

    /// <summary>
    /// The current formation mode set on the active harness
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show)]
    private string formationMode;

    /// <summary>
    /// The current active harness
    /// </summary>
    private FormationHarness activeHarness;

    /// <summary>
    /// An AI initialization call that occurs after the body is set.  The harness list is created during this
    /// callback.
    /// </summary>
    public override void BodyInit()
    {
        base.BodyInit();
        if (AI.Body == null)
            return;

        InitializeHarnesses();
    }

    /// <summary>
    /// Initialize the harness list.  This can be called at runtime to reset the harness list.
    /// </summary>
    public virtual void InitializeHarnesses()
    {
        harnesses.Clear();
        FormationHarness[] tHarnesses = AI.Body.GetComponentsInChildren<FormationHarness>(true);
        for (int i = 0; i < tHarnesses.Length; i++)
        {
            if (!harnesses.Contains(tHarnesses[i]))
                harnesses.Add(tHarnesses[i]);
        }
        SetActiveHarness(currentHarness, formationMode);
    }

    /// <summary>
    /// The active harness is updated during Pre, which happens prior to Think and Act during Update
    /// </summary>
    public override void Pre()
    {
        base.Pre();
        SetActiveHarness(currentHarness, formationMode);
    }

    /// <summary>
    /// The name of the current harness.  When set, the harness may be switched.
    /// </summary>
    public string CurrentHarness
    {
        get
        {
            return currentHarness;
        }

        set
        {
            SetActiveHarness(value, formationMode);
        }
    }

    public FormationHarness ActiveHarness
    {
        get { return activeHarness; }
    }

    /// <summary>
    /// Get or set the formation mode on the current active harness
    /// </summary>
    public string FormationMode
    {
        get
        {
            if (activeHarness != null)
                return activeHarness.FormationMode;

            return null;
        }

        set
        {
            if ((activeHarness != null) && (!string.IsNullOrEmpty(value)))
                activeHarness.FormationMode = value;
            if (activeHarness != null)
                formationMode = activeHarness.FormationMode;
        }
    }

    /// <summary>
    /// Set the active harness and formation mode by name
    /// </summary>
    /// <param name="aHarnessName">The name of the Harness to switch to</param>
    /// <param name="aFormationMode">The formation mode to use on the new harness</param>
    private void SetActiveHarness(string aHarnessName, string aFormationMode = null)
    {
        currentHarness = aHarnessName;

        activeHarness = null;
        for (int i = 0; i < harnesses.Count; i++)
        {
            FormationHarness tHarness = harnesses[i];
            if (tHarness == null)
                continue;

            if ((activeHarness == null) && (tHarness.name == aHarnessName))
            {
                tHarness.gameObject.SetActive(true);
                activeHarness = tHarness;
            }
            else
                tHarness.gameObject.SetActive(false);
        }

        FormationMode = aFormationMode;
    }
}
