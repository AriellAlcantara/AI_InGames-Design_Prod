using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public float speed = 10f;
    [HideInInspector] public float damage = 10f;
    [HideInInspector] public Team ownerTeam;

    private Vector3 moveDirection;
    private bool initialized = false;

    /// <summary>
    /// Called by the unit that fires this projectile to set its direction and stats.
    /// The projectile flies in a straight line (no homing).
    /// </summary>
    public void Init(Vector3 direction, float speed, float damage, Team ownerTeam)
    {
        this.moveDirection = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.ownerTeam = ownerTeam;
        this.initialized = true;

        // Face the travel direction
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // Despawn after 10 seconds if it doesn't hit anything
        Destroy(gameObject, 10f);
    }

    void Update()
    {
        if (!initialized) return;

        // Move in a straight line every frame — no homing
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // Ignore hits on objects from the same team
        Teamsystems hitTeam = hitObject.GetComponent<Teamsystems>();
        if (hitTeam == null) 
        {
            // Hit something that isn't a unit (wall, ground, etc.) — destroy projectile
            Destroy(gameObject);
            return;
        }

        if (hitTeam.team == ownerTeam)
        {
            // Friendly fire — ignore, don't destroy the projectile
            return;
        }

        // Hit an enemy — deal damage
        Meleeorranged targetCombat = hitObject.GetComponent<Meleeorranged>();
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
