using UnityEngine;

public class water : MonoBehaviour
{
    public float waterGrowth = 0.25f;
    public float growthFast = 3f;

    [SerializeField] private bool growWithSpaceForTesting = false;

    private float targetGrowth = 0f;
    private float currentGrowth = 0f;

    private Renderer plantRenderer;

    private void Start()
    {
        plantRenderer = GetComponentInChildren<Renderer>();
        SetGrowthShaderValue(currentGrowth);
    }

    private void Update()
    {
        if (growWithSpaceForTesting && Input.GetKeyDown(KeyCode.Space))
        {
            ApplyWater();
        }

        if (!Mathf.Approximately(currentGrowth, targetGrowth))
        {
            currentGrowth = Mathf.Lerp(currentGrowth, targetGrowth, Time.deltaTime * growthFast);
            SetGrowthShaderValue(currentGrowth);
        }
    }

    public void ApplyWater()
    {
        targetGrowth = Mathf.Min(1f, targetGrowth + waterGrowth);
    }

    private void SetGrowthShaderValue(float growth)
    {
        if (plantRenderer == null)
        {
            return;
        }

        plantRenderer.material.SetFloat("_growthChange", growth);
    }
}
