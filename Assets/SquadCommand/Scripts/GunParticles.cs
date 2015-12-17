using UnityEngine;

/// <summary>
/// GunParticles is an update of a script from BootCamp.  This is used to track particles emitted from the gun
/// during firing
/// </summary>
public class GunParticles : MonoBehaviour
{
    /// <summary>
    /// The particle system that we will play/stop
    /// </summary>
    private ParticleSystem _system;

    /// <summary>
    /// Gets our particle system, which should be located on the same GameObject
    /// </summary>
	void Awake()
	{
        _system = gameObject.GetComponent<ParticleSystem>();
	}
	
    /// <summary>
    /// Play or Stop the particle system
    /// </summary>
    /// <param name="aNewState">true = on, false = off</param>
	public void ChangeState(bool aNewState)
	{
        if (aNewState && _system.isStopped)
            _system.Play();
        else if (!aNewState && _system.isPlaying)
            _system.Stop();
	}
}