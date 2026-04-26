using UnityEngine;
using PixPlays.ElementalVFX;

public enum ProjectileOwner
{
    Player,
    Enemy
}

public enum ProjectileElement
{
    Water,
    Ice
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WaterProjectile : MonoBehaviour
{
    [SerializeField] private ProjectileOwner owner;
    [SerializeField] private ProjectileElement element = ProjectileElement.Water;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float waterGainOnPlayerHit = 15f;
    [SerializeField] private float lifeTime = 4f;

    [Header("Optional Asset VFX")]
    [SerializeField] private bool playAttachedVfx = true;
    [SerializeField] private float vfxTravelDistance = 18f;
    [SerializeField] private float vfxDuration = 1.1f;
    [SerializeField] private float vfxRadius = 1f;

    private Rigidbody rb;
    private GameObject source;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(ProjectileOwner projectileOwner, Vector3 direction, float projectileSpeed, GameObject projectileSource, ProjectileElement projectileElement = ProjectileElement.Water)
    {
        owner = projectileOwner;
        element = projectileElement;
        speed = projectileSpeed;
        source = projectileSource;

        Vector3 shotDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        rb.velocity = shotDirection * speed;
        transform.rotation = Quaternion.LookRotation(shotDirection, Vector3.up);
        PlayAssetVfx(shotDirection);
    }

    private void PlayAssetVfx(Vector3 shotDirection)
    {
        if (!playAttachedVfx)
        {
            return;
        }

        BaseVfx vfx = GetComponent<BaseVfx>();
        if (vfx == null)
        {
            vfx = GetComponentInChildren<BaseVfx>(true);
        }

        if (vfx == null)
        {
            return;
        }

        vfx.gameObject.SetActive(true);
        Vector3 target = transform.position + shotDirection.normalized * vfxTravelDistance;
        vfx.Play(new VfxData(transform.position, target, vfxDuration, vfxRadius));
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        if (source != null && (other.gameObject == source || other.transform.IsChildOf(source.transform)))
        {
            return;
        }

        if (owner == ProjectileOwner.Player)
        {
            if (element == ProjectileElement.Water)
            {
                water plant = other.GetComponentInParent<water>();
                if (plant == null)
                {
                    plant = other.GetComponentInChildren<water>();
                }

                if (plant != null)
                {
                    hasHit = true;
                    plant.ApplyWater();
                    Destroy(gameObject);
                    return;
                }
            }

            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.CanTakeDamage)
            {
                hasHit = true;
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                hasHit = true;
                player.TakeDamage(damage);
                player.AddWater(waterGainOnPlayerHit);
                Destroy(gameObject);
            }
        }
    }
}
