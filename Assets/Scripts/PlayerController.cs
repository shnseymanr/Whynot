using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class PlantCarryPrefab
{
    public string plantId;
    public GameObject prefab;
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private const RigidbodyConstraints ControlledRigidbodyConstraints =
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationY |
        RigidbodyConstraints.FreezeRotationZ;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private bool cameraRelativeMovement = true;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayDistance = 1.15f;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float currentWater;
    [SerializeField] private float requiredWaterForExit = 100f;

    [Header("Shooting")]
    [SerializeField] private bool aimWithMouse = true;
    [SerializeField] private float mouseAimDeadZone = 8f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private WaterProjectile waterProjectilePrefab;
    [SerializeField] private WaterProjectile iceProjectilePrefab;
    [SerializeField] private float waterCostPerShot = 5f;
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
    private Collider playerCollider;
    private Animator visualAnimator;
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
        playerCollider = GetComponent<Collider>();
        visualAnimator = GetComponentInChildren<Animator>();
        if (visualAnimator != null)
        {
            visualAnimator.enabled = true;
            visualAnimator.applyRootMotion = false;
        }

        rb.constraints = ControlledRigidbodyConstraints;
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
        float verticalInput = Input.GetAxisRaw("Vertical");
        moveInput = GetMovementDirection(horizontalInput, verticalInput);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverBlockingUi())
        {
            TurnToMouseNow();
            if (!TryPlantAtMousePosition())
            {
                TryShoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            TurnToMouseNow();
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
        UpdateCarriedPlantVisual();

        if (moveInput.sqrMagnitude > 0.001f)
        {
            RotateTowards(moveInput);
        }
    }

    private void FixedUpdate()
    {
        rb.constraints = ControlledRigidbodyConstraints;
        rb.angularVelocity = Vector3.zero;

        Vector3 velocity = moveInput * moveSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;

// --- SES EKLEMESİ: Yürüme ---
    bool isMoving = moveInput.sqrMagnitude > 0.01f && IsGrounded();
    GameSoundManager.Instance.PlayWalk(isMoving);

    if (jumpRequested && IsGrounded())
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        // --- SES EKLEMESİ: Zıplama ---
        GameSoundManager.Instance.PlaySFX(GameSoundManager.Instance.jumpClip);
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
if (currentHealth > 0f)
    {
        GameSoundManager.Instance.PlayDamageSFX();
    }
        GameManager.Instance?.SavePlayer(this);

        if (currentHealth <= 0f)
        {
            GameSoundManager.Instance.PlaySFX(GameSoundManager.Instance.deathClip);
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
        // --- SES EKLEMESİ: Ateş Etme ---
    if (IsIceModeActive)
        GameSoundManager.Instance.PlaySFX(GameSoundManager.Instance.ultimateShootClip);
    else
        GameSoundManager.Instance.PlaySFX(GameSoundManager.Instance.normalShootClip);
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

    private void UpdateAimDirection(bool instant = false)
    {
        Camera aimCamera = GetAimCamera();
        if (!aimWithMouse || aimCamera == null)
        {
            aimDirection = transform.forward;
            return;
        }

        Vector3 aimPivot = transform.position;
        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
        Plane aimPlane = new Plane(Vector3.up, aimPivot);
        if (aimPlane.Raycast(ray, out float distance))
        {
            Vector3 aimPoint = ray.GetPoint(distance);
            Vector3 direction = aimPoint - aimPivot;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                aimDirection = direction.normalized;
                return;
            }
        }

        Vector3 playerScreenPosition = aimCamera.WorldToScreenPoint(aimPivot);
        Vector2 screenDirection = (Vector2)Input.mousePosition - new Vector2(playerScreenPosition.x, playerScreenPosition.y);
        if (screenDirection.sqrMagnitude >= mouseAimDeadZone * mouseAimDeadZone)
        {
            aimDirection = new Vector3(screenDirection.x, 0f, screenDirection.y).normalized;
        }
    }

    private void TurnToMouseNow()
    {
        UpdateAimDirection(true);
        RotateTowards(aimDirection, true);
    }

    private Camera GetAimCamera()
    {
        if (Camera.main != null)
        {
            return Camera.main;
        }

        return FindFirstObjectByType<Camera>();
    }

    private Vector3 GetMovementDirection(float horizontalInput, float verticalInput)
    {
        Vector3 input = new Vector3(horizontalInput, 0f, verticalInput);
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        if (!cameraRelativeMovement || Camera.main == null)
        {
            return input;
        }

        Transform cameraTransform = Camera.main.transform;
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

        Vector3 right = cameraTransform.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;

        return (right * input.x + forward * input.z).normalized;
    }

    private void RotateTowards(Vector3 direction, bool instant = false)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion nextRotation = instant || turnSpeed <= 0f
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        ApplyRotationAroundPivot(nextRotation);
    }

    private void ApplyRotationAroundPivot(Quaternion nextRotation)
    {
        if (rb != null)
        {
            rb.constraints = ControlledRigidbodyConstraints;
            rb.angularVelocity = Vector3.zero;
            rb.rotation = nextRotation;
            transform.rotation = nextRotation;
            return;
        }

        transform.rotation = nextRotation;
    }

    private bool IsGrounded()
    {
        if (groundCheck != null && Physics.Raycast(groundCheck.position + Vector3.up * 0.05f, Vector3.down, groundCheckRadius + 0.15f, ~0, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        Bounds bounds = playerCollider != null
            ? playerCollider.bounds
            : new Bounds(transform.position, new Vector3(0.8f, 2f, 0.8f));

        Vector3 rayOrigin = bounds.center;
        float rayDistance = bounds.extents.y + Mathf.Max(0.15f, groundRayDistance * 0.25f);
        return Physics.Raycast(rayOrigin, Vector3.down, rayDistance, ~0, QueryTriggerInteraction.Ignore);
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
            GameSoundManager.Instance.PlaySFX(GameSoundManager.Instance.collectSeedClip);
            return;
        }

        if (closestPlantingSpot != null)
        {
            closestPlantingSpot.TryPlant();
            GameSoundManager.Instance.PlaySFX(GameSoundManager.Instance.plantSeedClip);
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

    private bool IsPointerOverBlockingUi()
    {
        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        EventSystem.current.RaycastAll(pointerData, results);
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<Button>() != null)
            {
                return true;
            }
        }

        return false;
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
