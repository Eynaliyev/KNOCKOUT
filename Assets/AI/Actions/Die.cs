using RAIN.Action;
using RAIN.Core;
using RAIN.Entities;
using UnityEngine;

/// <summary>
/// Die is a RAIN behavior tree action that updates the AI Body to remove entities, aspects, rigid bodies
/// and colliders on death.
/// To use it, create a custom action in your behavior tree and choose the "Die" action.
/// Note that after Die executes, the AI Body will be essentially unusable and any subsequent behaviors that
/// rely on movement, collisions, or entities/aspects will likely fail.  This is meant to be the last behavior executed.
/// </summary>
[RAINAction("Die")]
public class Die : RAINAction
{
    /// <summary>
    /// Remove colliders, entity rigs, rigid bodies, and character controllers
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>ActionResult.SUCCESS</returns>
    public override ActionResult Execute(AI ai)
    {
        //COLLIDERS
        Collider[] activeColliders = ai.Body.GetComponents<Collider>();
        foreach (Collider tCollider in activeColliders)
            if (tCollider != null)
                GameObject.DestroyImmediate(tCollider);

        //ENTITY RIGS ARE DEACTIVATED, NOT DESTROYED
        EntityRig tEntityRig = ai.Body.GetComponentInChildren<EntityRig>();
        if (tEntityRig != null)
            tEntityRig.Entity.DeactivateEntity();

        //RIGID BODIES (only 1 expected)
        Rigidbody tRigidBody = ai.Body.GetComponent<Rigidbody>();
        if (tRigidBody != null)
            tRigidBody.isKinematic = true;

        //CHARACTER CONTROLLERS (only 1 expected)
        CharacterController tCController = ai.Body.GetComponent<CharacterController>();
        if (tCController != null)
            GameObject.DestroyImmediate(tCController);

        return ActionResult.SUCCESS;
    }
}