using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlantCollectible : MonoBehaviour
{
    [Header("Plant")]
    [SerializeField] private string plantId = "mint";
    [SerializeField] private string displayName = "Nane";
    [SerializeField] private int amount = 1;
    [SerializeField] private bool destroyOnCollect = true;

    private bool collected;

    public string PlantId => plantId;
    public string DisplayName => displayName;
    public bool CanCollect => !collected;

    private void Reset()
    {
        Collider plantCollider = GetComponent<Collider>();
        plantCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider plantCollider = GetComponent<Collider>();
        plantCollider.isTrigger = true;
    }

    public bool Collect()
    {
        if (collected || GameManager.Instance == null)
        {
            return false;
        }

        collected = true;
        GameManager.Instance.AddPlant(plantId, displayName, amount);
        WorldController.SetQuestText($"{displayName} toplandi.");

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            Collect();
        }
    }
}
