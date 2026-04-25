using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class PlantCarryPrefab
{
    public string plantId;
    public GameObject prefab;
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float fullWaterJumpMultiplier = 0.45f;
    [SerializeField] private float jumpPenaltyStartWaterRatio = 0.45f;
    [SerializeField] private float fallGravityMultiplier = 3.2f;
    [SerializeField] private bool allowDepthMovement = true;
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField] private bool aimWithMouse = true;
    [SerializeField] private float turnSpeed = 0f;
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
    [SerializeField] private WaterProjectile waterProjectilePrefab;
    [SerializeField] private WaterProjectile iceProjectilePrefab;
    [SerializeField] private float waterCostPerShot = 0f;
    [SerializeField] private float projectileSpeed = 40f;
    [SerializeField] private float fireCooldown = 0.12f;

    [Header("Ultimate")]
    [SerializeField] private KeyCode ultimateKey = KeyCode.V;
    [SerializeField] private float ultimateRechargeDuration = 10f;
    [SerializeField] private int iceShotsPerUltimate = 5;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float interactionRadius = 1.6f;
    [SerializeField] private LayerMask interactionLayer = ~0;
    [SerializeField] private float plantingClickDistance = 150f;

    [Header("Plant Carry Preview")]
    [SerializeField] private Transform plantCarryPoint;
    [SerializeField] private Vector3 carriedPlantLocalOffset = new Vector3(0.75f, 0.15f, 0.35f);
    [SerializeField] private Vector3 carriedPlantLocalEuler;
    [SerializeField] private Vector3 carriedPlantLocalScale = Vector3.one * 0.6f;
    [SerializeField] private List<PlantCarryPrefab> carriedPlantPrefabs = new List<PlantCarryPrefab>();

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 aimDirection = Vector3.forward;
    private bool jumpRequested;
    private float nextFireTime;
    private float ultimateCharge;
    private int iceShotsRemaining;
    private string shownCarriedPlantId;
    private GameObject carriedPlantInstance;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxWater => maxWater;
    public float CurrentWater => currentWater;
    public float RequiredWaterForExit => requiredWaterForExit;
    public bool HasEnoughWaterForExit => currentWater >= requiredWaterForExit;
    public bool IsIceModeActive => iceShotsRemaining > 0;
    public int IceShotsRemaining => iceShotsRemaining;
    public float UltimateCharge01 => ultimateRechargeDuration > 0f ? Mathf.Clamp01(ultimateCharge / ultimateRechargeDuration) : 1f;
    public bool IsUltimateReady => UltimateCharge01 >= 1f;
    public string ActiveProjectileMode => IsIceModeActive ? "Buz" : "Su";

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        iceShotsRemaining = 0;
    }

    private void Start()
    {
        ultimateCharge = ultimateRechargeDuration;
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

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
        {
            if (!TryPlantAtMousePosition())
            {
                TryShoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            TryShoot();
        }

        if (Input.GetKeyDown(ultimateKey))
        {
            TryActivateUltimate();
        }

        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }

        RechargeUltimate();
        UpdateAimDirection();
        UpdateCarriedPlantVisual();

        if (aimWithMouse && aimDirection.sqrMagnitude > 0.001f)
        {
            RotateTowards(aimDirection);
        }
        else if (faceMoveDirection && moveInput.sqrMagnitude > 0.001f)
        {
            RotateTowards(moveInput);
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = moveInput * moveSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;

        if (jumpRequested && IsGrounded())
        {
            rb.velocity = new Vector3(rb.velocity.x, GetCurrentJumpForce(), rb.velocity.z);
        }

        if (rb.velocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
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
        ProjectileElement shotElement = IsIceModeActive ? ProjectileElement.Ice : ProjectileElement.Water;
        WaterProjectile projectilePrefab = GetProjectilePrefab(shotElement);

        if (Time.time < nextFireTime || projectilePrefab == null || firePoint == null)
        {
            return;
        }

        if (!SpendWater(waterCostPerShot))
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        Vector3 shotDirection = aimWithMouse && aimDirection.sqrMagnitude > 0.001f ? aimDirection : firePoint.forward;
        WaterProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shotDirection, Vector3.up));
        projectile.Initialize(ProjectileOwner.Player, shotDirection, projectileSpeed, gameObject, shotElement);

        if (IsIceModeActive)
        {
            iceShotsRemaining = Mathf.Max(0, iceShotsRemaining - 1);
            RefreshHUD();
        }
    }

    private void TryActivateUltimate()
    {
        if (!IsUltimateReady || IsIceModeActive)
        {
            return;
        }

        iceShotsRemaining = iceShotsPerUltimate;
        ultimateCharge = 0f;
        RefreshHUD();
    }

    private void RechargeUltimate()
    {
        if (ultimateCharge >= ultimateRechargeDuration)
        {
            return;
        }

        ultimateCharge = Mathf.Min(ultimateRechargeDuration, ultimateCharge + Time.deltaTime);
        WorldController.RefreshHud();
    }

    private void UpdateAimDirection()
    {
        if (!aimWithMouse || Camera.main == null)
        {
            aimDirection = transform.forward;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        if (aimPlane.Raycast(ray, out float distance))
        {
            Vector3 aimPoint = ray.GetPoint(distance);
            Vector3 direction = aimPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                aimDirection = direction.normalized;
            }
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        if (turnSpeed <= 0f)
        {
            transform.rotation = targetRotation;
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return Mathf.Abs(rb.velocity.y) < 0.05f;
        }

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private float GetCurrentJumpForce()
    {
        float waterRatio = maxWater > 0f ? Mathf.Clamp01(currentWater / maxWater) : 0f;
        float penaltyRatio = Mathf.InverseLerp(jumpPenaltyStartWaterRatio, 1f, waterRatio);
        float jumpMultiplier = Mathf.Lerp(1f, fullWaterJumpMultiplier, penaltyRatio);
        return jumpForce * jumpMultiplier;
    }

    private void TryInteract()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactionLayer, QueryTriggerInteraction.Collide);
        float closestDistance = float.PositiveInfinity;
        PlantCollectible closestPlant = null;
        PlantingSpot closestPlantingSpot = null;

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            PlantCollectible plant = nearbyCollider.GetComponentInParent<PlantCollectible>();
            if (plant != null && plant.CanCollect)
            {
                float distance = Vector3.SqrMagnitude(plant.transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlant = plant;
                    closestPlantingSpot = null;
                }
            }

            PlantingSpot plantingSpot = nearbyCollider.GetComponentInParent<PlantingSpot>();
            if (plantingSpot != null && plantingSpot.CanPlant)
            {
                float distance = Vector3.SqrMagnitude(plantingSpot.transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlant = null;
                    closestPlantingSpot = plantingSpot;
                }
            }
        }

        if (closestPlant != null)
        {
            closestPlant.Collect();
            return;
        }

        if (closestPlantingSpot != null)
        {
            closestPlantingSpot.TryPlant();
        }
    }

    private WaterProjectile GetProjectilePrefab(ProjectileElement element)
    {
        if (element == ProjectileElement.Ice)
        {
            return iceProjectilePrefab;
        }

        return waterProjectilePrefab;
    }

    private bool TryPlantAtMousePosition()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasSelectedPlant || Camera.main == null)
        {
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, plantingClickDistance, interactionLayer, QueryTriggerInteraction.Collide))
        {
            return false;
        }

        PlantingSpot plantingSpot = hit.collider.GetComponentInParent<PlantingSpot>();
        if (plantingSpot == null)
        {
            plantingSpot = hit.collider.GetComponentInChildren<PlantingSpot>();
        }

        return plantingSpot != null && plantingSpot.TryPlant();
    }

    private void UpdateCarriedPlantVisual()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.HasSelectedPlant)
        {
            ClearCarriedPlantVisual();
            return;
        }

        string selectedPlantId = NormalizePlantId(gameManager.SelectedPlantId);
        if (carriedPlantInstance != null && shownCarriedPlantId == selectedPlantId)
        {
            return;
        }

        ClearCarriedPlantVisual();

        GameObject prefab = GetCarriedPlantPrefab(selectedPlantId);
        if (prefab == null)
        {
            return;
        }

        Transform parent = plantCarryPoint != null ? plantCarryPoint : transform;
        carriedPlantInstance = Instantiate(prefab, parent);
        carriedPlantInstance.SetActive(true);
        carriedPlantInstance.transform.localPosition = plantCarryPoint != null ? Vector3.zero : carriedPlantLocalOffset;
        carriedPlantInstance.transform.localRotation = plantCarryPoint != null ? Quaternion.identity : Quaternion.Euler(carriedPlantLocalEuler);
        carriedPlantInstance.transform.localScale = carriedPlantLocalScale;
        shownCarriedPlantId = selectedPlantId;
        PrepareCarriedPlantVisual(carriedPlantInstance);
    }

    private GameObject GetCarriedPlantPrefab(string plantId)
    {
        foreach (PlantCarryPrefab entry in carriedPlantPrefabs)
        {
            if (entry != null && NormalizePlantId(entry.plantId) == plantId && entry.prefab != null)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    private void ClearCarriedPlantVisual()
    {
        shownCarriedPlantId = string.Empty;
        if (carriedPlantInstance != null)
        {
            Destroy(carriedPlantInstance);
            carriedPlantInstance = null;
        }
    }

    private void PrepareCarriedPlantVisual(GameObject visual)
    {
        foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
        {
            visualCollider.enabled = false;
        }

        foreach (Rigidbody visualRigidbody in visual.GetComponentsInChildren<Rigidbody>())
        {
            visualRigidbody.isKinematic = true;
            visualRigidbody.useGravity = false;
        }
    }

    private string NormalizePlantId(string plantId)
    {
        return string.IsNullOrWhiteSpace(plantId) ? string.Empty : plantId.Trim().ToLowerInvariant();
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
