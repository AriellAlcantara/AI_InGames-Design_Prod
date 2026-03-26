using UnityEngine;

public enum AttackType
{
    Melee,
    Ranged
}

public class Meleeorranged : MonoBehaviour
{
    [Header("Attack Settings")]
    public AttackType attackType = AttackType.Melee;

    [Tooltip("Damage dealt per attack.")]
    public float damage = 10f;

    [Tooltip("Seconds between attacks.")]
    public float attackCooldown = 1f;

    [Tooltip("Range at which this unit can deal damage.")]
    public float attackRange = 2f;

    [Header("Movement")]
    [Tooltip("Speed at which this unit moves toward its target.")]
    public float moveSpeed = 3.5f;

    [Header("Ranged Settings")]
    [Tooltip("Prefab to spawn when using ranged attacks (optional).")]
    public GameObject projectilePrefab;

    [Tooltip("Speed of the projectile.")]
    public float projectileSpeed = 10f;

    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Targeting")]
    [Tooltip("How often (in seconds) the unit rescans for a better target.")]
    public float retargetInterval = 0.5f;

    // Internal state
    private Teamsystems teamSystem;
    private Teamsystems currentTarget;
    private float attackTimer;
    private float retargetTimer;

    void Start()
    {
        teamSystem = GetComponent<Teamsystems>();
        currentHealth = maxHealth;

        if (teamSystem == null)
        {
            Debug.LogWarning($"{gameObject.name}: Meleeorranged requires a Teamsystems component!");
        }

        // Default attack range based on type if left at default
        if (attackType == AttackType.Ranged && attackRange <= 2f)
        {
            attackRange = 10f;
        }
    }

    void Update()
    {
        if (teamSystem == null) return;

        // Tick cooldown
        attackTimer -= Time.deltaTime;
        retargetTimer -= Time.deltaTime;

        // Rescan for a better target periodically, not just when current target is null
        if (retargetTimer <= 0f)
        {
            currentTarget = teamSystem.FindPriorityEnemy(attackType);
            retargetTimer = retargetInterval;
        }

        if (currentTarget == null) return; // No enemies nearby

        float distToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distToTarget <= attackRange)
        {
            // In range — attack
            FaceTarget();
            if (attackTimer <= 0f)
            {
                Attack(currentTarget);
                attackTimer = attackCooldown;
            }
        }
        else if (distToTarget <= teamSystem.detectionRange)
        {
            // Detected but out of attack range — move toward target
            MoveToward(currentTarget.transform.position);
            FaceTarget();
        }
        else
        {
            // Target went out of detection range, clear it and rescan immediately
            currentTarget = null;
            retargetTimer = 0f;
        }
    }

    void MoveToward(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    void FaceTarget()
    {
        if (currentTarget == null) return;
        Vector3 dir = (currentTarget.transform.position - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void Attack(Teamsystems target)
    {
        if (target == null) return;

        if (attackType == AttackType.Melee)
        {
            // Direct damage
            Meleeorranged targetCombat = target.GetComponent<Meleeorranged>();
            if (targetCombat != null)
            {
                targetCombat.TakeDamage(damage);
            }
        }
        else if (attackType == AttackType.Ranged)
        {
            if (projectilePrefab != null)
            {
                // Spawn a projectile aimed at the target's current position (no homing)
                Vector3 spawnPos = transform.position + transform.forward * 1f;
                Vector3 direction = (target.transform.position - spawnPos).normalized;

                GameObject proj = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Init(direction, projectileSpeed, damage, teamSystem.team);
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: Projectile prefab is missing the Projectile component!");
                    Object.Destroy(proj, 10f);
                }
            }
            else
            {
                // No projectile prefab — apply damage directly (hitscan style)
                Meleeorranged targetCombat = target.GetComponent<Meleeorranged>();
                if (targetCombat != null)
                {
                    targetCombat.TakeDamage(damage);
                }
            }
        }
    }

    /// <summary>
    /// Applies damage to this unit. Destroys the GameObject when health reaches 0.
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} ({teamSystem.team}) has been destroyed!");
        Destroy(gameObject);
    }

    /// <summary>
    /// Returns the current health of this unit.
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
