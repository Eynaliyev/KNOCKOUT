using UnityEngine;

/// <summary>
/// ShotLight is an update of the script provided in BootCammp.  This provides a timed light the is turned on
/// when a shot is fired and turned off after some delay
/// </summary>
public class ShotLight : MonoBehaviour
{
    /// <summary>
    /// time delay from light on to light off
    /// </summary>
    public float time = 0.02f;

    /// <summary>
    /// timer tracking delay countdown
    /// </summary>
    private float timer;

    /// <summary>
    /// the light for the firing of our particles
    /// </summary>
    private Light shotLight;

    private void Awake()
    {
        shotLight = GetComponent<Light>();
    }

    /// <summary>
    /// When enabled, enable the light and reset the timer
    /// </summary>
    private void OnEnable()
    {
        if (shotLight == null)
            Destroy(this);
        else
        {
            timer = time;
            shotLight.enabled = false;
        }
    }

    /// <summary>
    /// When disabled, turn off the light and reset the timer
    /// </summary>
    private void OnDisable()
    {
        if (shotLight == null)
            Destroy(this);
        else
        {
            timer = time;
            shotLight.enabled = false;
        }
    }

    //Check the timer and turn off the light if time expires
    private void LateUpdate()
    {
        if (timer > 0f)
            timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = time;
            shotLight.enabled = !shotLight.enabled;
        }
    }
}