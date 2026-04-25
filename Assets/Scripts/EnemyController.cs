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

    [Header("Spawner")]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxAliveEnemies = 5;
    [SerializeField] private bool spawnOnStart = true;

    private readonly HashSet<EnemyController> aliveEnemies = new HashSet<EnemyController>();
    private Rigidbody rb;
    private Transform player;
    private float currentHealth;
    private float nextShootTime;
    private EnemyController ownerSpawner;
    private Coroutine spawnRoutine;
    private Vector3 moveForward;
    private bool hasLockedChaseDirection;

    public bool CanTakeDamage => role == EnemyRole.Enemy || role == EnemyRole.Boss;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        moveForward = transform.forward;
    }

    private void OnEnable()
    {
        if (role == EnemyRole.Spawner && spawnOnStart)
        {
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

        if (player == null)
        {
            rb.velocity = new Vector3(moveForward.x * moveSpeed, rb.velocity.y, moveForward.z * moveSpeed);
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        Vector3 moveDirection = direction.normalized;
        if (!hasLockedChaseDirection || Vector3.Dot(moveDirection, moveForward) >= 0f)
        {
            moveForward = moveDirection;
            hasLockedChaseDirection = true;
            transform.rotation = Quaternion.LookRotation(moveForward, Vector3.up);
        }

        rb.velocity = new Vector3(moveForward.x * moveSpeed, rb.velocity.y, moveForward.z * moveSpeed);
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

    public void TrySpawn()
    {
        if (role != EnemyRole.Spawner || enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0 || aliveEnemies.Count >= maxAliveEnemies)
        {
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (spawnPoint == null)
        {
            return;
        }

        EnemyController enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.SetSpawnerOwner(this);
        aliveEnemies.Add(enemy);
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            TrySpawn();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void ShootAtPlayer()
    {
        if (projectilePrefab == null || player == null)
        {
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (player.position - spawnPosition).normalized;
        WaterProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        projectile.Initialize(ProjectileOwner.Enemy, direction, projectileSpeed, gameObject);
    }

    private bool CanSeePlayerAhead()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f && (!hasLockedChaseDirection || Vector3.Dot(direction.normalized, moveForward) >= 0f);
    }

    private void OnDestroy()
    {
        if (ownerSpawner != null)
        {
            ownerSpawner.aliveEnemies.Remove(this);
        }
    }
}
