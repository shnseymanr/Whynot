using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum VillageStage
{
    Dry = 0,
    FieldWatered = 1,
    DamFilled = 2,
    Raided = 3
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

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxWater => maxWater;
    public float CurrentWater => currentWater;
    public float RequiredWaterToExit => requiredWaterToExit;
    public VillageStage CurrentVillageStage => villageStage;
    public bool CanUseCaveExit => currentWater >= requiredWaterToExit;

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
            currentWater = 0f;
        }
        else if (villageStage == VillageStage.FieldWatered)
        {
            villageStage = VillageStage.DamFilled;
            currentWater = 0f;
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
