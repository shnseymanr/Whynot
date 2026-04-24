using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private bool allowDepthMovement = true;
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float currentWater;
    [SerializeField] private float requiredWaterForExit = 100f;

    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private WaterProjectile projectilePrefab;
    [SerializeField] private float waterCostPerShot = 10f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float fireCooldown = 0.25f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool jumpRequested;
    private float nextFireTime;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxWater => maxWater;
    public float CurrentWater => currentWater;
    public float RequiredWaterForExit => requiredWaterForExit;
    public bool HasEnoughWaterForExit => currentWater >= requiredWaterForExit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        GameManager.Instance?.RegisterPlayer(this);
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = allowDepthMovement ? Input.GetAxisRaw("Vertical") : 0f;
        moveInput = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            TryShoot();
        }

        if (faceMoveDirection && moveInput.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveInput, Vector3.up);
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = moveInput * moveSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;

        if (jumpRequested && IsGrounded())
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }

        jumpRequested = false;
    }

    public void SetLimits(float newMaxHealth, float newMaxWater, bool refreshHud = true)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        maxWater = Mathf.Max(1f, newMaxWater);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentWater = Mathf.Clamp(currentWater, 0f, maxWater);

        if (refreshHud)
        {
            RefreshHUD();
        }
    }

    public void SetValues(float health, float water, bool refreshHud = true)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        currentWater = Mathf.Clamp(water, 0f, maxWater);

        if (refreshHud)
        {
            RefreshHUD();
        }
    }

    public void SetRequiredWater(float value)
    {
        requiredWaterForExit = Mathf.Clamp(value, 0f, maxWater);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        GameManager.Instance?.SavePlayer(this);

        if (currentHealth <= 0f)
        {
            GameManager.Instance?.PlayerFailedCave();
        }
    }

    public void AddWater(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentWater = Mathf.Min(maxWater, currentWater + amount);
        GameManager.Instance?.SavePlayer(this);
    }

    public bool SpendWater(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentWater < amount)
        {
            return false;
        }

        currentWater -= amount;
        GameManager.Instance?.SavePlayer(this);
        return true;
    }

    public void RefreshHUD()
    {
        WorldController.RefreshHud();
    }

    private void TryShoot()
    {
        if (Time.time < nextFireTime || projectilePrefab == null || firePoint == null)
        {
            return;
        }

        if (!SpendWater(waterCostPerShot))
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        WaterProjectile projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        projectile.Initialize(ProjectileOwner.Player, firePoint.forward, projectileSpeed, gameObject);
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return Mathf.Abs(rb.velocity.y) < 0.05f;
        }

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
