using UnityEngine;
using System.Collections;
//Thanks to Johannes Holweg from Germany for creating this script to be used as an interface with UFPS


namespace red{//Create any namespace you want.  Just for organizing
    public class UFPS_Rain_Bridge : MonoBehaviour {

        public float ufpsDamageMultiplier = 10.0f;
        private DamageProxy proxy;
        private Damage adamage;
        // Use this for initialization
        void Start ()
        {
            proxy = this.gameObject.GetComponentInParent<DamageProxy>();
         }
        public virtual void Damage(float damage)
        {
            adamage.damage = damage * ufpsDamageMultiplier;
            proxy.healthElement.ReceiveDamage(adamage);
        }
    }
}