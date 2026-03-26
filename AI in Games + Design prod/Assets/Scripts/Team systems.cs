using UnityEngine;
using System.Collections.Generic;

public enum Team
{
    Red,
    Blue
}

public class Teamsystems : MonoBehaviour
{
    [Header("Team Assignment")]
    public Team team = Team.Red;

    [Header("Detection")]
    [Tooltip("How far this unit can detect other units.")]
    public float detectionRange = 15f;

    // All active units in the scene
    private static List<Teamsystems> allUnits = new List<Teamsystems>();

    /// <summary>
    /// Returns true if the other unit is on the same team (ally).
    /// </summary>
    public bool IsAlly(Teamsystems other)
    {
        return other != null && other.team == this.team;
    }

    /// <summary>
    /// Returns true if the other unit is on the opposing team (enemy).
    /// </summary>
    public bool IsEnemy(Teamsystems other)
    {
        return other != null && other.team != this.team;
    }

    /// <summary>
    /// Finds the closest enemy unit within detection range. Returns null if none found.
    /// </summary>
    public Teamsystems FindClosestEnemy()
    {
        Teamsystems closest = null;
        float closestDist = Mathf.Infinity;

        for (int i = 0; i < allUnits.Count; i++)
        {
            Teamsystems unit = allUnits[i];
            if (unit == this || unit == null) continue;
            if (!IsEnemy(unit)) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist <= detectionRange && dist < closestDist)
            {
                closestDist = dist;
                closest = unit;
            }
        }

        return closest;
    }

    /// <summary>
    /// Finds the best enemy target using priority rules:
    /// 1. Always prefer the closest enemy overall.
    /// 2. If a same-type enemy (melee vs melee, ranged vs ranged) is within a reasonable
    ///    extra distance compared to the absolute closest, prefer the same-type target instead.
    /// This means a melee unit charging right at you will always get targeted,
    /// but if two enemies are roughly the same distance, same-type wins.
    /// </summary>
    public Teamsystems FindPriorityEnemy(AttackType myAttackType)
    {
        Teamsystems closestSameType = null;
        float closestSameTypeDist = Mathf.Infinity;

        Teamsystems closestAnyType = null;
        float closestAnyTypeDist = Mathf.Infinity;

        for (int i = 0; i < allUnits.Count; i++)
        {
            Teamsystems unit = allUnits[i];
            if (unit == this || unit == null) continue;
            if (!IsEnemy(unit)) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist > detectionRange) continue;

            // Track closest enemy of any type
            if (dist < closestAnyTypeDist)
            {
                closestAnyTypeDist = dist;
                closestAnyType = unit;
            }

            // Track closest enemy that matches our attack type
            Meleeorranged enemyCombat = unit.GetComponent<Meleeorranged>();
            if (enemyCombat != null && enemyCombat.attackType == myAttackType)
            {
                if (dist < closestSameTypeDist)
                {
                    closestSameTypeDist = dist;
                    closestSameType = unit;
                }
            }
        }

        // If no enemies at all, return null
        if (closestAnyType == null) return null;

        // If no same-type enemy exists, just return the closest
        if (closestSameType == null) return closestAnyType;

        // Only prefer same-type if it's not much farther than the absolute closest enemy.
        // If the closest enemy (any type) is significantly closer, target them instead.
        // This ensures a melee unit running at you gets targeted over a distant same-type.
        float sameTypeExtraDistance = closestSameTypeDist - closestAnyTypeDist;
        if (sameTypeExtraDistance <= 3f)
        {
            // Same-type enemy is close enough — prefer it
            return closestSameType;
        }
        else
        {
            // Closest enemy of any type is way nearer — switch to them
            return closestAnyType;
        }
    }

    /// <summary>
    /// Returns a list of all enemies within detection range.
    /// </summary>
    public List<Teamsystems> FindAllEnemiesInRange()
    {
        List<Teamsystems> enemies = new List<Teamsystems>();

        for (int i = 0; i < allUnits.Count; i++)
        {
            Teamsystems unit = allUnits[i];
            if (unit == this || unit == null) continue;
            if (!IsEnemy(unit)) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist <= detectionRange)
            {
                enemies.Add(unit);
            }
        }

        return enemies;
    }

    /// <summary>
    /// Returns a list of all allies within detection range (excluding self).
    /// </summary>
    public List<Teamsystems> FindAllAlliesInRange()
    {
        List<Teamsystems> allies = new List<Teamsystems>();

        for (int i = 0; i < allUnits.Count; i++)
        {
            Teamsystems unit = allUnits[i];
            if (unit == this || unit == null) continue;
            if (!IsAlly(unit)) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist <= detectionRange)
            {
                allies.Add(unit);
            }
        }

        return allies;
    }

    void OnEnable()
    {
        if (!allUnits.Contains(this))
            allUnits.Add(this);
    }

    void OnDisable()
    {
        allUnits.Remove(this);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection range in the Scene view
        Gizmos.color = (team == Team.Red) ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
