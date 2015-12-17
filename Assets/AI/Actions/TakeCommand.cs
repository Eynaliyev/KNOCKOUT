using RAIN.Action;
using RAIN.Core;
using RAIN.Entities;
using RAIN.Entities.Aspects;
using UnityEngine;

/// <summary>
/// TakeCommand is a RAIN behavior tree action that turns an ordinary soldier into a commander.
/// To use it, create a custom action in your behavior tree and choose the "Take Command" action.
/// NOTE: This action returns RUNNING when executing, so it must be run in a Parallel
/// </summary>
[RAINAction("Take Command")]
public class TakeCommand : RAINAction
{
    /// <summary>
    /// The formationHarness is added and tracked so it can be removed on Stop
    /// </summary>
    private FormationHarnessElement _formationElement = new FormationHarnessElement() { Name = "Formation Harness" };

    /// <summary>
    /// Start does all the setup work for this action, assigning the "defcommander" aspect and removing
    /// the "defsoldier" aspect.  A Formation Harness is added, set to "Column" as the current formation
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        //Add the commander aspect and remove the soldier aspect.
        EntityRig rig = ai.Body.GetComponentInChildren<EntityRig>();
        if (rig.Entity.GetAspect("defcommander") == null)
        {
            VisualAspect tCommanderAspect = new VisualAspect() { AspectName = "defcommander" };
            rig.Entity.AddAspect(tCommanderAspect);
            VisualAspect tSoldierAspect = rig.Entity.GetAspect("defsoldier") as VisualAspect;
            if (tSoldierAspect != null)
                tCommanderAspect.MountPoint = tSoldierAspect.MountPoint;
            else tCommanderAspect.Position = new Vector3(0f, 1f, 0f);

        }

        //Add the formation harness set to the Column formation
        _formationElement.CurrentHarness = "Column";
        ai.AddCustomElement(_formationElement);
    }

    /// <summary>
    /// Execute does no work, but always returns RUNNING
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>ActionResult.RUNNING - this action should always be used in a Parallel</returns>
    public override ActionResult Execute(AI ai)
    {
        return ActionResult.RUNNING;
    }

    /// <summary>
    /// Stop removes the commander aspect and the formation element
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Stop(AI ai)
    {
        EntityRig rig = ai.Body.GetComponentInChildren<EntityRig>();
        rig.Entity.RemoveAspect(rig.Entity.GetAspect("defcommander"));

        ai.RemoveCustomElement(_formationElement);
        base.Stop(ai);
    }
}