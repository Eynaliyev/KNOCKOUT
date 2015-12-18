using RAIN.Animation;
using RAIN.Core;
using RAIN.Serialization;
using UnityEngine;

/// <summary>
/// AimAndFireElement is a CustomAIElement that appears in the Custom tab of a RAIN AI Rig
/// This Element is responsible for managing aiming control and firing/reloading states of the gun
/// </summary>
[RAINSerializableClass, RAINElement("Aim and Fire Control")]
public class AimAndFireElement : CustomAIElement
{
    /// <summary>
    /// After lining up the shot, an error can be applied.  This is measured in the radius of the firing cone at 1 meter
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Potential error at 1 meter")]
    private float _accuracy = 0.1f;

    /// <summary>
    /// Reaction time isn't used directly, but is passed on to the AI behavior for decision making as "reactionTime"
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Potential time from deciding to fire before shooting")]
    private float _reactionTime = 1.5f;

    /// <summary>
    /// Attach the gun prefab to this field
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Gun prefab to mount and use")]
    private GameObject _gunPrefab = null;

    /// <summary>
    /// Attach the gun mount point on the body to this field
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Mount point for gun")]
    private Transform _gunMount = null;

    /// <summary>
    /// Attach a joint on the body where the gun will be aimed from
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Aim point for gun, needs to be fixed to the body (not the gun)")]
    private Transform _gunAim = null;

    /// <summary>
    /// Store the mecanim animator so we can feed aiming angles to it
    /// </summary>
    private MecanimAnimator _animator = null;

    /// <summary>
    /// The gun controller that provides direct fire/reload methods.
    /// </summary>
    private GunController _gunController = null;

    /// <summary>
    /// Track whether we are actively firing the weapon
    /// </summary>
    private bool _isFiring = false;

    /// <summary>
    /// Track whether we are in the process of reloading
    /// </summary>
    private bool _isReloading = false;

    /// <summary>
    /// Track whether we have something to aim at, or whether the gun should be brought to a resting position
    /// </summary>
    private bool _hasAimTarget = false;

    /// <summary>
    /// The position of our current aim target.  This is only valid if hasAimTarget is true.
    /// </summary>
    private Vector3 _currentTarget = Vector3.zero;

    /// <summary>
    /// After lining up the shot, an error can be applied.  This is measured in the radius of the firing cone at 1 meter
    /// Accuracy of 0 means perfect accuracy.  Accuracy of .1 means .1 meter error at 1 meter distance.
    /// </summary>
    public float Accuracy
    {
        get { return _accuracy; }
        set { _accuracy = value; }
    }

    /// <summary>
    /// Reaction time isn't used directly, but is passed on to the AI behavior for decision making as "reactionTime"
    /// </summary>
    public float ReactionTime
    {
        get { return _reactionTime; }
        set { _reactionTime = value; }
    }

    /// <summary>
    /// Is the weapon currently being fired?  Reloading may override this behavior
    /// </summary>
    public bool IsFiring
    {
        get { return _isFiring; }
    }

    /// <summary>
    /// Is the weapon currently being reloaded?
    /// </summary>
    public bool IsReloading
    {
        get { return _isReloading; }
    }

    /// <summary>
    /// Does the AI have an aim target?
    /// </summary>
    public bool IsAimingAtTarget
    {
        get { return _hasAimTarget; }
    }

    /// <summary>
    /// Accessor to the current aim target - read only.
    /// </summary>
    public Vector3 AimTarget
    {
        get { return _currentTarget; }
    }

    /// <summary>
    /// Accessor to the Gun Controller - read only.
    /// </summary>
    public GunController Controller
    {
        get { return _gunController; }
    }

    /// <summary>
    /// Called whenever the AI is first initialized.  The AI Body may not be set at this point.
    /// </summary>
    public override void AIInit()
    {
        base.AIInit();

        _hasAimTarget = false;
    }

    /// <summary>
    /// Called when the AI is initialized and the body has been ste.
    /// Set up gun parenting and grab the AI Animator and the Gun Controller
    /// </summary>
    public override void BodyInit()
    {
        base.BodyInit();

        if (_gunPrefab == null || _gunMount == null)
            throw new System.Exception("AimAndFireELement requires a Gun Prefab and Gun Mount");

        GameObject tGun = (GameObject)GameObject.Instantiate(_gunPrefab);
        tGun.transform.parent = _gunMount;
        tGun.transform.localPosition = Vector3.zero;
        tGun.transform.localRotation = Quaternion.identity;

        _animator = AI.Animator as MecanimAnimator;
        if (_animator == null)
            throw new System.Exception("AimAndFireELement only works with a MecanimAnimator on your AI");

        _gunController = tGun.GetComponentInChildren<GunController>();
        if (_gunController == null)
            throw new System.Exception("AimAndFireELement requires a GunController located on the Gun Prefab");

        _gunController.shooter = AI.Body;
    }

    /// <summary>
    /// Tell the gun to reload
    /// </summary>
    public void Reload()
    {
        if (!_gunController.Reloading)
            _gunController.Reload();
    }

    /// <summary>
    /// Set firing mode on.  This will cause the firing animation to start and firing to
    /// occur on the gun controller when not reloading.
    /// </summary>
    public void FireWeapon()
    {
        _isFiring = true;
    }

    /// <summary>
    /// Set the Aim Target and mark that we have a valid aim target
    /// </summary>
    /// <param name="aTarget"></param>
    public void SetAimTarget(Vector3 aTarget)
    {
        _hasAimTarget = true;
        _currentTarget = aTarget;
    }

    /// <summary>
    /// Remove the aim target
    /// </summary>
    public void SetNoAim()
    {
        _hasAimTarget = false;
    }

    /// <summary>
    /// Called on AI prior to Update.  Here we can set memory variables before the Behavior Tree runs.
    /// </summary>
    public override void Pre()
    {
        base.Pre();

        AI.WorkingMemory.SetItem<float>("reactionTime", ReactionTime);
    }

    /// <summary>
    /// Act occurs after Pre and Think in the AI loop.  Set up gun angles if we have an aim target.
    /// Fire the gun if we are firing, then check to see if the gun is reloading.  If so, play the reloading
    /// animation.
    /// </summary>
    public override void Act()
    {
        base.Act();

        Vector3 tNewTarget;
        float tAimTilt;

        if (_hasAimTarget)
        {
            // Move our target into our aim space and remove x and -z and determine our aim
            Vector3 tTargetInAim = _gunAim.transform.InverseTransformPoint(_currentTarget);
            tTargetInAim.x = 0;
            tTargetInAim.z = Mathf.Abs(tTargetInAim.z);

            tAimTilt = Vector3.Angle(tTargetInAim, Vector3.forward);
            tAimTilt *= Mathf.Sign(Vector3.Dot(tTargetInAim, Vector3.up));

            //Calculate accuracy from distance to target
            float tDistanceToTarget = Vector3.Distance(_currentTarget, AI.Body.transform.position);
            float tModifiedAccuracy = _accuracy * tDistanceToTarget;

            //Calculate the aim error from a normal distribution
            float tXOffset = RAIN.Utility.MathUtils.RandomFromNormalDistribution(0f - tModifiedAccuracy, tModifiedAccuracy);
            float tYOffset = RAIN.Utility.MathUtils.RandomFromNormalDistribution(0f - tModifiedAccuracy, tModifiedAccuracy);

            //Create an offset vector from the calculated error, then translate it into local space
            Vector3 tVectorOffsetGlobal = new Vector3(tXOffset, tYOffset, 0f);
            Vector3 tVectorOffsetLocal = AI.Body.transform.rotation * tVectorOffsetGlobal;

            //Apply the error
            tNewTarget = _currentTarget + tVectorOffsetLocal;
        }
        else
        {
            //No aim target, so remove aim tilt
            tAimTilt = 0;
            tNewTarget = Vector3.zero;
        }

        //Pass the aim tilt to the mecanim animator
        _animator.UnityAnimator.SetFloat("AimingTilt", tAimTilt);

        //Fire the weapon and play the firing animation
        if (_isFiring)
        {
            _animator.UnityAnimator.SetFloat("Firing", 1.0f);
            _gunController.Fire(tNewTarget);
        }
        else
            _animator.UnityAnimator.SetFloat("Firing", 0);

        //If the weapon is reloading, play the reload animation
        if (_gunController.Reloading)
        {
            if (!_isReloading)
                _animator.UnityAnimator.SetTrigger("Reload");

            _isReloading = true;
        }
        else if (!_gunController.Reloading)
            _isReloading = false;

        //Firing only happens for one frame.  The AI/Player must call it each frame if firing will repeat
        _isFiring = false;
    }
}
