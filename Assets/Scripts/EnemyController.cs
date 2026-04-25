using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyRole
{
    Enemy,
    Boss,
    Spawner
}

public class EnemyController : MonoBehaviour
{
    [Header("Role")]
    [SerializeField] private EnemyRole role = EnemyRole.Enemy;

    [Header("Enemy/Boss")]
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float shootInterval = 1.5f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private WaterProjectile projectilePrefab;
    [SerializeField] private bool moveTowardNegativeZ = true;
    [SerializeField] private float shootOnlyWithinDistance = 35f;
    [SerializeField] private bool useVisibleModelAsFireOrigin = true;

    [Header("Spawner")]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxAliveEnemies = 5;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float fallbackSpawnSpacing = 2f;

    private readonly HashSet<EnemyController> aliveEnemies = new HashSet<EnemyController>();
    private Rigidbody rb;
    private Transform player;
    private float currentHealth;
    private float nextShootTime;
    private EnemyController ownerSpawner;
    private Coroutine spawnRoutine;
    private Vector3 moveForward;
    private static EnemyController fallbackEnemyPrefab;
    private bool hasBeenVisibleByCamera;
    private Renderer visibleRenderer;

    public bool CanTakeDamage => role == EnemyRole.Enemy || role == EnemyRole.Boss;
    public bool HasBeenVisibleByCamera => hasBeenVisibleByCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        CacheVisibleRenderer();
        SetMoveDirection();
    }

    private void OnEnable()
    {
        if (role == EnemyRole.Spawner && spawnOnStart)
        {
            AutoAssignSpawnerReferences();
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }
    }

    private void Update()
    {
        if (role == EnemyRole.Spawner)
        {
            return;
        }

        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            player = playerController != null ? playerController.transform : null;
            return;
        }

        if (Time.time >= nextShootTime && CanSeePlayerAhead())
        {
            ShootAtPlayer();
            nextShootTime = Time.time + shootInterval;
        }
    }

    private void FixedUpdate()
    {
        if (role == EnemyRole.Spawner || rb == null)
        {
            return;
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, moveForward.z * moveSpeed);
    }

    public void TakeDamage(float amount)
    {
        if (!CanTakeDamage)
        {
            return;
        }

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetSpawnerOwner(EnemyController spawner)
    {
        ownerSpawner = spawner;
    }

    public void MarkVisibleByCamera()
    {
        hasBeenVisibleByCamera = true;
    }

    public void TrySpawn()
    {
        if (role != EnemyRole.Spawner || aliveEnemies.Count >= maxAliveEnemies)
        {
            return;
        }

        AutoAssignSpawnerReferences();
        if (enemyPrefab == null)
        {
            return;
        }

        Transform spawnPoint = GetSpawnPoint();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : GetFallbackSpawnPosition();
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        EnemyController enemy = Instantiate(enemyPrefab, spawnPosition, spawnRotation);
        enemy.role = EnemyRole.Enemy;
        enemy.moveTowardNegativeZ = moveTowardNegativeZ;
        enemy.SetMoveDirection();
        enemy.SetSpawnerOwner(this);
        aliveEnemies.Add(enemy);
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            TrySpawn();
            yield return new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
        }
    }

    private void AutoAssignSpawnerReferences()
    {
        if (role != EnemyRole.Spawner)
        {
            return;
        }

        if (enemyPrefab == null)
        {
            enemyPrefab = fallbackEnemyPrefab != null ? fallbackEnemyPrefab : FindEnemyPrefabInScene();
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in transform)
            {
                children.Add(child);
            }

            spawnPoints = children.ToArray();
        }
    }

    private EnemyController FindEnemyPrefabInScene()
    {
        foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy != null && enemy != this && enemy.role == EnemyRole.Enemy)
            {
                fallbackEnemyPrefab = enemy;
                return enemy;
            }
        }

        return null;
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (spawnPoint != null)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private Vector3 GetFallbackSpawnPosition()
    {
        int lane = aliveEnemies.Count % 3 - 1;
        return transform.position + new Vector3(lane * fallbackSpawnSpacing, 0f, 0f);
    }

    private void SetMoveDirection()
    {
        moveForward = moveTowardNegativeZ ? Vector3.back : Vector3.forward;
        transform.rotation = Quaternion.LookRotation(moveForward, Vector3.up);
    }

    private void ShootAtPlayer()
    {
        if (projectilePrefab == null || player == null)
        {
            return;
        }

        Vector3 spawnPosition = GetProjectileSpawnPosition();
        Vector3 direction = (player.position - spawnPosition).normalized;
        WaterProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        projectile.Initialize(ProjectileOwner.Enemy, direction, projectileSpeed, gameObject);
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        if (useVisibleModelAsFireOrigin)
        {
            CacheVisibleRenderer();
            if (visibleRenderer != null)
            {
                Bounds bounds = visibleRenderer.bounds;
                return bounds.center + moveForward * Mathf.Max(0.2f, bounds.extents.z + 0.35f);
            }
        }

        return firePoint != null ? firePoint.position : transform.position;
    }

    private bool CanSeePlayerAhead()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        float sqrDistance = direction.sqrMagnitude;
        return sqrDistance > 0.001f
            && sqrDistance <= shootOnlyWithinDistance * shootOnlyWithinDistance
            && Vector3.Dot(direction.normalized, moveForward) >= 0f;
    }

    private void CacheVisibleRenderer()
    {
        if (visibleRenderer != null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float largestBounds = 0f;
        foreach (Renderer candidate in renderers)
        {
            if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            float size = candidate.bounds.size.sqrMagnitude;
            if (size > largestBounds)
            {
                largestBounds = size;
                visibleRenderer = candidate;
            }
        }
    }

    private void OnDestroy()
    {
        if (ownerSpawner != null)
        {
            ownerSpawner.aliveEnemies.Remove(this);
        }
    }
}
