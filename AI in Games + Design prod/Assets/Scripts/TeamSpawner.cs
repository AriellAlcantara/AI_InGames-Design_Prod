using UnityEngine;
using System.Collections.Generic;

public class TeamSpawner : MonoBehaviour
{
    [Header("Red Team Prefabs")]
    [Tooltip("First melee prefab for Red team.")]
    public GameObject redMelee1Prefab;

    [Tooltip("Second melee prefab for Red team.")]
    public GameObject redMelee2Prefab;

    [Tooltip("First ranged prefab for Red team.")]
    public GameObject redRanged1Prefab;

    [Tooltip("Second ranged prefab for Red team.")]
    public GameObject redRanged2Prefab;

    [Header("Blue Team Prefabs")]
    [Tooltip("First melee prefab for Blue team.")]
    public GameObject blueMelee1Prefab;

    [Tooltip("Second melee prefab for Blue team.")]
    public GameObject blueMelee2Prefab;

    [Tooltip("First ranged prefab for Blue team.")]
    public GameObject blueRanged1Prefab;

    [Tooltip("Second ranged prefab for Blue team.")]
    public GameObject blueRanged2Prefab;

    [Header("Spawn Points")]
    [Tooltip("Assign empty GameObjects in the scene as Red team spawn locations. The forward (Z) direction of each point defines 'toward the enemy'.")]
    public Transform[] redSpawnPoints;

    [Tooltip("Assign empty GameObjects in the scene as Blue team spawn locations. The forward (Z) direction of each point defines 'toward the enemy'.")]
    public Transform[] blueSpawnPoints;

    [Header("Red Wave Settings")]
    [Tooltip("Number of Red Melee 1 units to spawn per wave.")]
    public int redMelee1PerWave = 1;

    [Tooltip("Number of Red Melee 2 units to spawn per wave.")]
    public int redMelee2PerWave = 1;

    [Tooltip("Number of Red Ranged 1 units to spawn per wave.")]
    public int redRanged1PerWave = 1;

    [Tooltip("Number of Red Ranged 2 units to spawn per wave.")]
    public int redRanged2PerWave = 1;

    [Header("Blue Wave Settings")]
    [Tooltip("Number of Blue Melee 1 units to spawn per wave.")]
    public int blueMelee1PerWave = 1;

    [Tooltip("Number of Blue Melee 2 units to spawn per wave.")]
    public int blueMelee2PerWave = 1;

    [Tooltip("Number of Blue Ranged 1 units to spawn per wave.")]
    public int blueRanged1PerWave = 1;

    [Tooltip("Number of Blue Ranged 2 units to spawn per wave.")]
    public int blueRanged2PerWave = 1;

    [Header("General Spawn Settings")]
    [Tooltip("Time in seconds between spawn waves. Set to 0 to only spawn once on Start.")]
    public float timeBetweenWaves = 10f;

    [Tooltip("If true, the first wave spawns immediately on Start.")]
    public bool spawnOnStart = true;

    [Tooltip("Maximum total units alive per team at once. 0 = unlimited.")]
    public int maxUnitsPerTeam = 10;

    [Tooltip("Maximum total units a team can ever spawn. Once reached, that team stops spawning permanently.")]
    public int maxTotalSpawnsPerTeam = 20;

    [Header("Formation Settings")]
    [Tooltip("Spacing between each unit along the diagonal line.")]
    public float unitSpacing = 2f;

    [Tooltip("How far back the ranged row is placed behind the melee row.")]
    public float rowDepth = 4f;

    // Track spawned units so we can enforce max caps
    private List<GameObject> redUnitsAlive = new List<GameObject>();
    private List<GameObject> blueUnitsAlive = new List<GameObject>();

    // Track total spawned (never decreases, even when units die)
    private int redTotalSpawned = 0;
    private int blueTotalSpawned = 0;

    private float waveTimer;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnWave();
        }

        waveTimer = timeBetweenWaves;
    }

    void Update()
    {
        if (timeBetweenWaves <= 0f) return;

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            SpawnWave();
            waveTimer = timeBetweenWaves;
        }
    }

    /// <summary>
    /// Spawns a full wave of Red and Blue units at their respective spawn points.
    /// </summary>
    public void SpawnWave()
    {
        SpawnRequest[] redMelee = new SpawnRequest[]
        {
            new SpawnRequest(redMelee1Prefab, "Melee1", redMelee1PerWave),
            new SpawnRequest(redMelee2Prefab, "Melee2", redMelee2PerWave)
        };
        SpawnRequest[] redRanged = new SpawnRequest[]
        {
            new SpawnRequest(redRanged1Prefab, "Ranged1", redRanged1PerWave),
            new SpawnRequest(redRanged2Prefab, "Ranged2", redRanged2PerWave)
        };

        SpawnRequest[] blueMelee = new SpawnRequest[]
        {
            new SpawnRequest(blueMelee1Prefab, "Melee1", blueMelee1PerWave),
            new SpawnRequest(blueMelee2Prefab, "Melee2", blueMelee2PerWave)
        };
        SpawnRequest[] blueRanged = new SpawnRequest[]
        {
            new SpawnRequest(blueRanged1Prefab, "Ranged1", blueRanged1PerWave),
            new SpawnRequest(blueRanged2Prefab, "Ranged2", blueRanged2PerWave)
        };

        SpawnUnitsForTeam(Team.Red, redMelee, redRanged, redSpawnPoints, redUnitsAlive, ref redTotalSpawned);
        SpawnUnitsForTeam(Team.Blue, blueMelee, blueRanged, blueSpawnPoints, blueUnitsAlive, ref blueTotalSpawned);
    }

    /// <summary>
    /// Spawns melee and ranged units for one team in a diagonal formation.
    /// Melee units form a diagonal line in front, ranged units form a diagonal line behind.
    /// </summary>
    private void SpawnUnitsForTeam(Team team, SpawnRequest[] meleeRequests, SpawnRequest[] rangedRequests,
                                    Transform[] spawnPoints, List<GameObject> aliveList, ref int totalSpawned)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"TeamSpawner: No spawn points assigned for {team} team!");
            return;
        }

        if (maxTotalSpawnsPerTeam > 0 && totalSpawned >= maxTotalSpawnsPerTeam)
        {
            return;
        }

        aliveList.RemoveAll(u => u == null);

        // Build flat lists for front row (melee) and back row (ranged)
        List<SpawnEntry> meleeEntries = BuildSpawnEntries(meleeRequests);
        List<SpawnEntry> rangedEntries = BuildSpawnEntries(rangedRequests);

        for (int s = 0; s < spawnPoints.Length; s++)
        {
            Transform spawnPoint = spawnPoints[s];
            if (spawnPoint == null) continue;

            Vector3 forward = spawnPoint.forward;
            Vector3 right = spawnPoint.right;

            // --- Front row: All melee units in a diagonal line ---
            SpawnRow(team, meleeEntries, spawnPoint, forward, right, 0f, aliveList, ref totalSpawned);

            // --- Back row: All ranged units in a diagonal line behind melee ---
            SpawnRow(team, rangedEntries, spawnPoint, forward, right, -rowDepth, aliveList, ref totalSpawned);
        }
    }

    /// <summary>
    /// Spawns a row of units in a diagonal line at the given depth offset from the spawn point.
    /// </summary>
    private void SpawnRow(Team team, List<SpawnEntry> entries, Transform spawnPoint,
                           Vector3 forward, Vector3 right, float depthOffset,
                           List<GameObject> aliveList, ref int totalSpawned)
    {
        int totalInRow = entries.Count;

        for (int i = 0; i < totalInRow; i++)
        {
            if (maxUnitsPerTeam > 0 && aliveList.Count >= maxUnitsPerTeam) break;
            if (maxTotalSpawnsPerTeam > 0 && totalSpawned >= maxTotalSpawnsPerTeam) break;

            SpawnEntry entry = entries[i];
            if (entry.prefab == null)
            {
                Debug.LogWarning($"TeamSpawner: No {entry.label} prefab assigned for {team} team!");
                continue;
            }

            Vector3 pos = GetDiagonalPosition(spawnPoint.position, forward, right, i, totalInRow, depthOffset);
            GameObject unit = Instantiate(entry.prefab, pos, spawnPoint.rotation);
            totalSpawned++;
            unit.name = $"{team}_{entry.label}_{totalSpawned}";

            Teamsystems teamSys = unit.GetComponent<Teamsystems>();
            if (teamSys != null)
                teamSys.team = team;

            aliveList.Add(unit);
        }
    }

    /// <summary>
    /// Expands SpawnRequests (prefab + count) into a flat list of individual SpawnEntries.
    /// e.g. Melee1 x2 + Melee2 x1 becomes [Melee1, Melee1, Melee2]
    /// </summary>
    private List<SpawnEntry> BuildSpawnEntries(SpawnRequest[] requests)
    {
        List<SpawnEntry> entries = new List<SpawnEntry>();
        foreach (SpawnRequest req in requests)
        {
            for (int i = 0; i < req.count; i++)
            {
                entries.Add(new SpawnEntry(req.prefab, req.label));
            }
        }
        return entries;
    }

    /// <summary>
    /// Calculates a position along a diagonal line for a formation.
    /// </summary>
    private Vector3 GetDiagonalPosition(Vector3 origin, Vector3 forward, Vector3 right,
                                         int index, int totalInRow, float depthOffset)
    {
        float centered = index - (totalInRow - 1) * 0.5f;

        Vector3 lateralOffset = right * centered * unitSpacing;
        Vector3 diagonalForwardOffset = forward * centered * unitSpacing * 0.5f;
        Vector3 rowOffset = forward * depthOffset;

        return origin + lateralOffset + diagonalForwardOffset + rowOffset;
    }

    /// <summary>
    /// Returns how many Red units are currently alive.
    /// </summary>
    public int GetRedAliveCount()
    {
        redUnitsAlive.RemoveAll(u => u == null);
        return redUnitsAlive.Count;
    }

    /// <summary>
    /// Returns how many Blue units are currently alive.
    /// </summary>
    public int GetBlueAliveCount()
    {
        blueUnitsAlive.RemoveAll(u => u == null);
        return blueUnitsAlive.Count;
    }

    /// <summary>
    /// Returns how many total units Red has ever spawned.
    /// </summary>
    public int GetRedTotalSpawned()
    {
        return redTotalSpawned;
    }

    /// <summary>
    /// Returns how many total units Blue has ever spawned.
    /// </summary>
    public int GetBlueTotalSpawned()
    {
        return blueTotalSpawned;
    }

    /// <summary>
    /// Returns true if the given team has reached its max total spawn cap.
    /// </summary>
    public bool IsTeamSpawnedOut(Team team)
    {
        if (maxTotalSpawnsPerTeam <= 0) return false;
        int total = (team == Team.Red) ? redTotalSpawned : blueTotalSpawned;
        return total >= maxTotalSpawnsPerTeam;
    }

    void OnDrawGizmos()
    {
        int redMeleeTotal = redMelee1PerWave + redMelee2PerWave;
        int redRangedTotal = redRanged1PerWave + redRanged2PerWave;
        int blueMeleeTotal = blueMelee1PerWave + blueMelee2PerWave;
        int blueRangedTotal = blueRanged1PerWave + blueRanged2PerWave;

        DrawFormationGizmos(redSpawnPoints, Color.red, redMeleeTotal, redRangedTotal);
        DrawFormationGizmos(blueSpawnPoints, Color.blue, blueMeleeTotal, blueRangedTotal);
    }

    private void DrawFormationGizmos(Transform[] spawnPoints, Color teamColor, int meleeCount, int rangedCount)
    {
        if (spawnPoints == null) return;

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;

            Vector3 forward = point.forward;
            Vector3 right = point.right;

            // Draw melee positions (front row) — brighter
            Gizmos.color = teamColor;
            for (int i = 0; i < meleeCount; i++)
            {
                Vector3 pos = GetDiagonalPosition(point.position, forward, right, i, meleeCount, 0f);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
            }

            // Draw ranged positions (back row) — darker
            Color darkerColor = teamColor * 0.6f;
            darkerColor.a = 1f;
            Gizmos.color = darkerColor;
            for (int i = 0; i < rangedCount; i++)
            {
                Vector3 pos = GetDiagonalPosition(point.position, forward, right, i, rangedCount, -rowDepth);
                Gizmos.DrawWireSphere(pos, 0.4f);
            }
        }
    }

    /// <summary>
    /// Defines a prefab type with a per-wave count.
    /// </summary>
    private struct SpawnRequest
    {
        public GameObject prefab;
        public string label;
        public int count;

        public SpawnRequest(GameObject prefab, string label, int count)
        {
            this.prefab = prefab;
            this.label = label;
            this.count = count;
        }
    }

    /// <summary>
    /// A single unit to spawn (expanded from SpawnRequest).
    /// </summary>
    private struct SpawnEntry
    {
        public GameObject prefab;
        public string label;

        public SpawnEntry(GameObject prefab, string label)
        {
            this.prefab = prefab;
            this.label = label;
        }
    }
}
