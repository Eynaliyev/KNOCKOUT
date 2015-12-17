using RAIN.Action;
using RAIN.Core;
using RAIN.Entities;
using RAIN.Entities.Aspects;
using UnityEngine;

/// <summary>
/// FireWeapon is a RAIN behavior tree action that sets aiming for the weapon and then fires it using
/// the AimAndFirElement.
/// To use it, create a custom action in your behavior tree and choose the "Fire Weapon" action.
/// </summary>
[RAINAction("Fire Weapon")]
public class FireWeapon : RAINAction
{
    /// <summary>
    /// We store the AimAndFireElement so we don't have to grab it on each Start/Execute
    /// </summary>
    private AimAndFireElement _aimAndFire = null;

    /// <summary>
    /// Start finds and stores the AimAndFireElement
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        if (_aimAndFire == null)
            _aimAndFire = ai.GetCustomElement<AimAndFireElement>();
    }

    /// <summary>
    /// When executing, this action checks to see if the AI "enemy" variable has a value.  If so,
    /// and if the enemy has an aimpoint aspect, that is used as the aim target.  If the aimpoint doesn't
    /// exist, then the enemy is used directly.  If neither exists, firing still occurs but aiming does not.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE if the AimAndFireElement is missing, SUCCESS otherwise</returns>
    public override ActionResult Execute(AI ai)
    {
        if (_aimAndFire == null)
            return ActionResult.FAILURE;

        //Use the AI enemy variable as the aim target
        GameObject tAimObject = ai.WorkingMemory.GetItem<GameObject>("enemy");

        //If the target exists, set the aim target
        if (tAimObject != null)
        {
            RAINAspect tAimPoint = null;

            //Look for an aimpoint aspect and use that if possible
            EntityRig tEntity = tAimObject.GetComponentInChildren<EntityRig>();
            if (tEntity != null)
                tAimPoint = tEntity.Entity.GetAspect("aimpoint");

            //Otherwise just use the enemy object plus a default height
            if (tAimPoint == null)
                _aimAndFire.SetAimTarget(tAimObject.transform.position + new Vector3(0, 1.5f, 0));
            else
                _aimAndFire.SetAimTarget(tAimPoint.Position);
        }

        //Fire away
        _aimAndFire.FireWeapon();

        return ActionResult.SUCCESS;
    }
}