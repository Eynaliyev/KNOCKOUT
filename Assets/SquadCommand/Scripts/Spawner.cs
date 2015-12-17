using UnityEngine;

/// <summary>
/// Spawner is a simple component that spawns a team of AI at a specified interval
/// </summary>
public class Spawner : MonoBehaviour 
{
    /// <summary>
    /// The delay between spawns in seconds
    /// </summary>
    public float spawnDelay = 10f;

    /// <summary>
    /// The number of AI to spawn.  This will include 1 commander and the rest as soldiers.
    /// </summary>
    public int squadSize = 5;

    /// <summary>
    /// A timer tracking next spawn
    /// </summary>
    private float timer = 0f;

    /// <summary>
    /// The Commander prefab/object to replicate
    /// </summary>
    public GameObject Commander = null;

    /// <summary>
    /// The Soldier prefab/object to replicate
    /// </summary>
    public GameObject Soldier = null;

    /// <summary>
    /// Update the timer and check for respawn.  Create a team of 1 commander and squadSize - 1 soldiers
    /// </summary>
    public void Update()
    {
        if (timer <= 0f)
        {
            //1 commander
            if ((squadSize > 0) && (Commander != null))
            {
                GameObject tCommander = GameObject.Instantiate(Commander, gameObject.transform.position, this.transform.rotation) as GameObject;
                tCommander.SetActive(true);
            }

            //squadSize -1 soldiers, arrayed in line behind the spawn point
            if (Soldier != null)
            {
                for (int i = 1; i < squadSize; i++)
                {
                    GameObject tSoldier = GameObject.Instantiate(Soldier, this.transform.position - (gameObject.transform.forward * (float)i), this.transform.rotation) as GameObject;
                    tSoldier.SetActive(true);
                }
            }

            timer = spawnDelay;
        }
        else
            timer -= Time.deltaTime;
    }

}
