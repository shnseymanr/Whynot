using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum VillageStage
{
    Dry = 0,
    FieldWatered = 1,
    DamFilled = 2,
    Raided = 3
}

[System.Serializable]
public class PlantInventoryEntry
{
    public string plantId;
    public string displayName;
    public int count;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string villageSceneName = "VillageScene";
    [SerializeField] private string caveSceneName = "CaveScene";

    [Header("Saved Player State")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float currentWater;
    [SerializeField] private float requiredWaterToExit = 100f;

    [Header("Progress")]
    [SerializeField] private VillageStage villageStage = VillageStage.Dry;
    [SerializeField] private bool returnedFromCaveWithWater;
    [SerializeField] private List<PlantInventoryEntry> plantInventory = new List<PlantInventoryEntry>();

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxWater => maxWater;
    public float CurrentWater => currentWater;
    public float RequiredWaterToExit => requiredWaterToExit;
    public VillageStage CurrentVillageStage => villageStage;
    public bool CanUseCaveExit => currentWater >= requiredWaterToExit;
    public IReadOnlyList<PlantInventoryEntry> PlantInventory => plantInventory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void Start()
    {
        RefreshSceneState();
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        player.SetLimits(maxHealth, maxWater, false);
        player.SetValues(currentHealth, currentWater, false);
        player.SetRequiredWater(requiredWaterToExit);
        player.RefreshHUD();
    }

    public void SavePlayer(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        maxHealth = player.MaxHealth;
        currentHealth = player.CurrentHealth;
        maxWater = player.MaxWater;
        currentWater = player.CurrentWater;
        WorldController.RefreshHud();
    }

    public void GoToCave()
    {
        StartCoroutine(LoadSceneRoutine(caveSceneName));
    }

    public void ReturnToVillage(PlayerController player)
    {
        SavePlayer(player);
        returnedFromCaveWithWater = currentWater >= requiredWaterToExit;
        StartCoroutine(LoadSceneRoutine(villageSceneName));
    }

    public void PlayerFailedCave()
    {
        currentHealth = maxHealth;
        currentWater = 0f;
        StartCoroutine(LoadSceneRoutine(villageSceneName));
    }

    public void AddPlant(string plantId, string displayName, int amount = 1)
    {
        plantId = NormalizePlantId(plantId);
        if (string.IsNullOrEmpty(plantId) || amount <= 0)
        {
            return;
        }

        PlantInventoryEntry entry = FindPlantEntry(plantId);
        if (entry == null)
        {
            entry = new PlantInventoryEntry
            {
                plantId = plantId,
                displayName = string.IsNullOrWhiteSpace(displayName) ? plantId : displayName,
                count = 0
            };
            plantInventory.Add(entry);
        }

        entry.count += amount;
        WorldController.RefreshHud();
    }

    public bool TryConsumePlant(string plantId, int amount = 1)
    {
        plantId = NormalizePlantId(plantId);
        PlantInventoryEntry entry = FindPlantEntry(plantId);
        if (entry == null || entry.count < amount || amount <= 0)
        {
            return false;
        }

        entry.count -= amount;
        if (entry.count <= 0)
        {
            plantInventory.Remove(entry);
        }

        WorldController.RefreshHud();
        return true;
    }

    public int GetPlantCount(string plantId)
    {
        PlantInventoryEntry entry = FindPlantEntry(NormalizePlantId(plantId));
        return entry != null ? entry.count : 0;
    }

    public string GetPlantInventoryText()
    {
        if (plantInventory.Count == 0)
        {
            return "Bitkiler: -";
        }

        StringBuilder builder = new StringBuilder("Bitkiler");
        bool addedAny = false;

        foreach (PlantInventoryEntry entry in plantInventory)
        {
            if (entry == null || entry.count <= 0)
            {
                continue;
            }

            builder.AppendLine();
            builder.Append("- ");

            builder.Append(string.IsNullOrWhiteSpace(entry.displayName) ? entry.plantId : entry.displayName);
            if (entry.count > 1)
            {
                builder.Append(" (");
                builder.Append(entry.count);
                builder.Append(")");
            }

            addedAny = true;
        }

        return addedAny ? builder.ToString() : "Bitkiler: -";
    }

    public string GetQuestText()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (villageStage == VillageStage.Raided)
        {
            return "Koy saldiriya ugradi!";
        }

        if (activeScene == caveSceneName)
        {
            return CanUseCaveExit ? "Koye don ve gorevi tamamla." : "Su barini doldur.";
        }

        if (villageStage == VillageStage.Dry)
        {
            return "Magaraya git ve su topla.";
        }

        if (villageStage == VillageStage.FieldWatered)
        {
            return "Tekrar magaraya don.";
        }

        return "Koy nefes aliyor.";
    }

    public void RefreshSceneState()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        RegisterPlayer(player);

        foreach (WorldController worldController in FindObjectsByType<WorldController>(FindObjectsSortMode.None))
        {
            worldController.ApplyCurrentState();
        }

        WorldController.RefreshHud();
    }

    private void ApplyVillageReturnProgress()
    {
        if (!returnedFromCaveWithWater)
        {
            return;
        }

        returnedFromCaveWithWater = false;

        if (villageStage == VillageStage.Dry)
        {
            villageStage = VillageStage.FieldWatered;
        }
        else if (villageStage == VillageStage.FieldWatered)
        {
            villageStage = VillageStage.DamFilled;
            StartCoroutine(RaidAfterFadeRoutine());
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        WorldController fade = WorldController.FindFadeController();
        if (fade != null)
        {
            yield return fade.FadeOut();
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (operation != null && !operation.isDone)
        {
            yield return null;
        }

        fade = WorldController.FindFadeController();
        if (fade != null)
        {
            yield return fade.FadeIn();
        }
    }

    private PlantInventoryEntry FindPlantEntry(string plantId)
    {
        foreach (PlantInventoryEntry entry in plantInventory)
        {
            if (entry != null && entry.plantId == plantId)
            {
                return entry;
            }
        }

        return null;
    }

    private string NormalizePlantId(string plantId)
    {
        return string.IsNullOrWhiteSpace(plantId) ? string.Empty : plantId.Trim().ToLowerInvariant();
    }

    private IEnumerator RaidAfterFadeRoutine()
    {
        yield return null;

        WorldController fade = WorldController.FindFadeController();
        if (fade != null)
        {
            yield return fade.FadeOut();
        }

        villageStage = VillageStage.Raided;
        RefreshSceneState();

        if (fade != null)
        {
            yield return new WaitForSeconds(0.35f);
            yield return fade.FadeIn();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneState();

        if (scene.name == villageSceneName)
        {
            ApplyVillageReturnProgress();
            RefreshSceneState();
        }
    }
}
