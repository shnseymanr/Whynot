using UnityEngine;

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

    private Rigidbody rb;
    private GameObject source;

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
        ApplyElementVisual();
    }

    private void ApplyElementVisual()
    {
        Renderer projectileRenderer = GetComponentInChildren<Renderer>();
        if (projectileRenderer == null)
        {
            return;
        }

        projectileRenderer.material.color = element == ProjectileElement.Ice
            ? new Color(0.75f, 0.95f, 1f, 1f)
            : new Color(0.05f, 0.35f, 1f, 1f);

        transform.localScale = element == ProjectileElement.Ice ? Vector3.one * 1.35f : Vector3.one * 0.9f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (source != null && (other.gameObject == source || other.transform.IsChildOf(source.transform)))
        {
            return;
        }

        if (owner == ProjectileOwner.Player)
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.CanTakeDamage)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                player.AddWater(waterGainOnPlayerHit);
                Destroy(gameObject);
            }
        }
    }
}
