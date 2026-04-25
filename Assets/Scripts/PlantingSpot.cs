using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlantingSpot : MonoBehaviour
{
    [Header("Accepted Plant")]
    [SerializeField] private string acceptedPlantId = "mint";
    [SerializeField] private string displayName = "Nane";
    [SerializeField] private bool acceptAnyPlant;

    [Header("Planted Result")]
    [SerializeField] private GameObject plantedVisual;
    [SerializeField] private GameObject plantedPrefab;
    [SerializeField] private Transform plantedSpawnPoint;
    [SerializeField] private bool canPlantOnlyOnce = true;

    private bool planted;

    public bool CanPlant => !canPlantOnlyOnce || !planted;

    private void Reset()
    {
        Collider spotCollider = GetComponent<Collider>();
        spotCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider spotCollider = GetComponent<Collider>();
        spotCollider.isTrigger = true;

        if (plantedVisual != null)
        {
            plantedVisual.SetActive(planted);
        }
    }

    public bool TryPlant()
    {
        if (!CanPlant || GameManager.Instance == null)
        {
            return false;
        }

        PlantInventoryEntry entry = FindPlantToUse(GameManager.Instance.SelectedPlantId);
        if (entry == null)
        {
            WorldController.SetQuestText(GameManager.Instance.HasSelectedPlant ? "Bu tarlaya secili bitki ekilemiyor." : "Once envanterden bitki sec.");
            return false;
        }

        if (!GameManager.Instance.TryConsumePlant(entry.plantId))
        {
            return false;
        }

        planted = true;
        ShowPlantedVisual();
        GameManager.Instance.ClearSelectedPlant();
        WorldController.SetQuestText($"{entry.displayName} ekildi.");
        return true;
    }

    private void OnMouseDown()
    {
        TryPlant();
    }

    private PlantInventoryEntry FindPlantToUse(string selectedPlantId)
    {
        selectedPlantId = NormalizePlantId(selectedPlantId);
        if (!string.IsNullOrEmpty(selectedPlantId))
        {
            foreach (PlantInventoryEntry entry in GameManager.Instance.PlantInventory)
            {
                if (entry == null || entry.count <= 0 || entry.plantId != selectedPlantId)
                {
                    continue;
                }

                return entry;
            }

            return null;
        }

        if (!acceptAnyPlant)
        {
            return null;
        }

        foreach (PlantInventoryEntry entry in GameManager.Instance.PlantInventory)
        {
            if (entry != null && entry.count > 0)
            {
                return entry;
            }
        }

        return null;
    }

    private void ShowPlantedVisual()
    {
        if (plantedVisual != null)
        {
            plantedVisual.SetActive(true);
            return;
        }

        if (plantedPrefab == null)
        {
            return;
        }

        Transform spawnPoint = plantedSpawnPoint != null ? plantedSpawnPoint : transform;
        plantedVisual = Instantiate(plantedPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    private string NormalizePlantId(string plantId)
    {
        return string.IsNullOrWhiteSpace(plantId) ? string.Empty : plantId.Trim().ToLowerInvariant();
    }
}
