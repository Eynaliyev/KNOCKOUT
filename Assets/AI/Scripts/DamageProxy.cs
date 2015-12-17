using UnityEngine;

/// <summary>
/// DamageProxy is a Unity component used by the HealthElement to receive Unity messages.  This component
/// is automatically created on the AI Body by the HealthElement.
/// </summary>
public class DamageProxy : MonoBehaviour 
{
    /// <summary>
    /// The HealthElement that will receive incoming damage notifications
    /// </summary>
    public HealthElement healthElement;

    /// <summary>
    /// A method that will receive Unity messages and forward them to the HealthElement
    /// </summary>
    /// <param name="aDamage">A damage struct with information about the Hit</param>
    private void Hit(Damage aDamage)
    {
        //Forward the hit to the HealthElement
        healthElement.ReceiveDamage(aDamage);
    }
}
