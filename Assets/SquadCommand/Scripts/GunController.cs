using UnityEngine;

/// <summary>
/// GunController is based on the Gun.js script found in the Unity Bootcamp demo scene.  It has been modified for
/// simplification
/// </summary>
public class GunController : MonoBehaviour
{
    /// <summary>
    /// Gun firing mode - semi, automatic, or bursting
    /// </summary>
    public enum FireMode
    {
        SEMI_AUTO,
        FULL_AUTO,
        BURST
    }

    //The parent object of the AI/enemy shooting the gun
    public GameObject shooter;

    //The name of the gun
    public string gunName;

    //The location of the muzzleTip - used for aiming and effects
    public Transform muzzleTip;

    //How many shots the gun can take in one second
    public float fireRate;

    //The firing mode - auto/semi/burst
    public FireMode fireMode;

    //Number of shots to fire when on burst mode
    public int burstRate;

    //Range of fire in meters
    public float fireRange;

    //The damage sent to a damage receiver on Hit
    public float baseDamage = 2f;

    //The damage sent to a UFPS Player
    public float ufpsDamage = 25f;

    //The damage sent to a UFPS Player
    public int ufpsPlayerLayer = 30;

    //The number of bullets that can be fired before reloading
    public int clipSize;

    //The number of clips currently carried
    public int totalClips;

    //Time to reload the weapon in seconds
    public float reloadTime;

    //autoReload forces reloading when firing with no ammo
    public bool autoReload = true;

    //number of rounds left in the clip
    public int currentRounds;

    //volume of a shot when fired
    public float shotVolume = 0.4f;

    //the shot sound
    public AudioClip shotSound;

    //reloading sound
    public AudioClip reloadSound;

    //out of ammo sound (when firing with no ammo)
    public AudioClip outOfAmmoSound;

    //What layers can the gun hit
    public LayerMask hitLayers;

    //What particles are displayed on hit (by the gun)
    public GameObject hitParticle;

    //A container for hit particles placed by the gun on a hit object
    private GameObject hitContainer;

    //Shooting particles
    public GunParticles shootingEmitter;

    //Casing particles
    public GunParticles capsuleEmitter;

    //Tracer fire emitter
    public ParticleSystem traceFire;

    //The light on the gun when firing (muzzle flash helper)
    public ShotLight shotLight;

    //The direction the gun is aiming (used to raycast hits)
    public Vector3 aimDirection;

    //reload time - tracks when reloading is complete
    private float reloadTimer;

    //Can the gun be fired
    private bool freeToShoot;

    //Is the gun reloading
    private bool reloading;

    //The last time the gun was shot - used for semi and burst mode
    private float lastShootTime;

    //delay between shots in all modes
    private float shootDelay;

    //Size of burst remaining
    private int cBurst;

    //Is ammo unlimited
    public bool unlimited = true;

    //Is the gun currently firing
    private bool isFiring = false;

    //The audio source we will use to play
    private AudioSource gunAudioSource = null;

    /// <summary>
    /// Is reloading accessor
    /// </summary>
    public bool Reloading
    {
        get { return reloading; }
    }

    /// <summary>
    /// Call this method to fire the gun at a particular target.  this sets the aimDirection
    /// </summary>
    /// <param name="aTargetLocation">the world space coordinates of the firing target</param>
    public void Fire(Vector3 aTargetLocation)
    {
        isFiring = true;
        if (cBurst <= 0)
            cBurst = burstRate;

        aimDirection = (aTargetLocation - muzzleTip.position).normalized;
    }

    /// <summary>
    /// Cause the gun to reload
    /// </summary>
    public void Reload()
    {
        if (totalClips > 0 && currentRounds < clipSize)
        {
            PlayReloadSound();

            reloading = true;
            reloadTimer = reloadTime;
        }
    }

    /// <summary>
    /// Awake grabs our audio source for shot firing
    /// </summary>
    private void Awake()
    {
        gunAudioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Since guns can be swapped, OnEnable sets up the gun
    /// </summary>
    private void OnEnable()
    {
        reloadTimer = 0.0f;
        reloading = false;
        freeToShoot = true;
        shootDelay = 1.0f / fireRate;

        cBurst = burstRate;

        totalClips--;
        currentRounds = clipSize;

        if (shootingEmitter != null)
            shootingEmitter.ChangeState(false);

        if (capsuleEmitter != null)
            capsuleEmitter.ChangeState(false);

        if (shotLight != null)
            shotLight.enabled = false;

        hitContainer = GameObject.Find("ParticleGarbage");
        if (hitContainer == null)
            hitContainer = new GameObject("ParticleGarbage");
    }

    /// <summary>
    /// disable emitters and lights on disable
    /// </summary>
    private void OnDisable()
    {
        if (shootingEmitter != null)
            shootingEmitter.ChangeState(false);

        if (capsuleEmitter != null)
            capsuleEmitter.ChangeState(false);

        if (shotLight != null)
            shotLight.enabled = false;
    }

    /// <summary>
    /// Do reloading and firing each update, then set firing to false (must be retriggered)
    /// </summary>
    private void Update()
    {
        HandleReloading();

        HandleFiring();

        isFiring = false;
    }

    /// <summary>
    /// If reloading, wait for the timer, then reset the rounds
    /// </summary>
    private void HandleReloading()
    {
        if (reloading)
        {
            reloadTimer -= Time.deltaTime;

            if (reloadTimer <= 0.0)
            {
                reloading = false;

                if (!unlimited)
                    totalClips--;

                currentRounds = clipSize;
            }
        }
    }

    /// <summary>
    /// If firing and not reloading and not empty, take the shot
    /// </summary>
    private void HandleFiring()
    {
        //Firing and not reloading?
        if (isFiring && !reloading)
        {
            //Not out of ammo?
            if (currentRounds > 0)
            {
                //If we aren't waiting for the prior shot to finish
                if (Time.time > lastShootTime && freeToShoot && cBurst > 0)
                {
                    lastShootTime = Time.time + shootDelay;

                    switch (fireMode)
                    {
                        case FireMode.SEMI_AUTO:
                            freeToShoot = false;
                            break;
                        case FireMode.BURST:
                            cBurst--;
                            break;
                    }

                    PlayShotSound();

                    if (shootingEmitter != null)
                        shootingEmitter.ChangeState(true);

                    if (capsuleEmitter != null)
                        capsuleEmitter.ChangeState(true);

                    if (shotLight != null)
                        shotLight.enabled = true;

                    CheckRaycastHit();

                    currentRounds--;
                    if (currentRounds <= 0)
                        Reload();
                }
            }
            else if (autoReload && freeToShoot)
            {
                //If we ran out of bullets, turn off emitters
                //Then reload if we aren't already
                if (shootingEmitter != null)
                    shootingEmitter.ChangeState(false);

                if (capsuleEmitter != null)
                    capsuleEmitter.ChangeState(false);

                if (shotLight != null)
                    shotLight.enabled = false;

                if (!reloading)
                    Reload();
            }
        }
        else
        {
            //not firing, so turn off emitters
            if (shootingEmitter != null)
                shootingEmitter.ChangeState(false);

            if (capsuleEmitter != null)
                capsuleEmitter.ChangeState(false);

            if (shotLight != null)
                shotLight.enabled = false;
        }
    }

    /// <summary>
    /// Click
    /// </summary>
    private void PlayOutOfAmmoSound()
    {
        gunAudioSource.PlayOneShot(outOfAmmoSound, 1.5f);
    }

    /// <summary>
    /// Shink shink
    /// </summary>
    private void PlayReloadSound()
    {
        gunAudioSource.PlayOneShot(reloadSound, 1.5f);
    }

    /// <summary>
    /// Pop
    /// </summary>
    private void PlayShotSound()
    {
        gunAudioSource.PlayOneShot(shotSound);
    }

    /// <summary>
    /// Check to see if our aimDirection raycast hits a valid target.  Set off tracer fire and particles
    /// Send a Hit message if we hit a valid target
    /// </summary>
    private void CheckRaycastHit()
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(muzzleTip.position, aimDirection, out raycastHit, fireRange, hitLayers))
        {

            // Check if hidden Object is on UFPS Player Layer and do UFPS Damage

            if(raycastHit.collider.gameObject.layer == ufpsPlayerLayer)
            {

                raycastHit.collider.gameObject.SendMessageUpwards("Damage", ufpsDamage/100, SendMessageOptions.DontRequireReceiver);

            }

            // Else do SQ Damage

            else
            {

                raycastHit.collider.gameObject.SendMessage("Hit", new Damage() { hit = raycastHit, damage = baseDamage, damageFrom = shooter }, SendMessageOptions.DontRequireReceiver);

            }

            // Adjust the trace lifetime (-2 is for the distance the trace is out in front of us)
            traceFire.startLifetime = Mathf.Max(0, (raycastHit.distance - 10) / traceFire.startSpeed);

            // Make the tracer aim properly
            shootingEmitter.transform.rotation = Quaternion.FromToRotation(Vector3.forward, raycastHit.point - shootingEmitter.transform.position);

            // Generate the particles
            GameObject tParticle = (GameObject)GameObject.Instantiate(hitParticle, raycastHit.point, Quaternion.FromToRotation(Vector3.forward, raycastHit.normal));
            tParticle.transform.parent = hitContainer.transform;
            UnityEngine.Object.Destroy(tParticle, 1);
        }
        else
        {
            // Set the trace back to full
            traceFire.startLifetime = 1;

            shootingEmitter.transform.rotation = Quaternion.FromToRotation(Vector3.forward, (shootingEmitter.transform.position + aimDirection * fireRange) - shootingEmitter.transform.position);
        }
    }
}