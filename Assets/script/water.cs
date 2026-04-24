using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class water : MonoBehaviour
{
    public float waterGrowth = 0.25f;

    public float growthFast = 3f;     
    private float targetGrowth = 0f;  
    private float currentGrowth = 0f;

    private Renderer plantRenderer;
    void Start()
    {
        plantRenderer = GetComponent<Renderer>();
        plantRenderer.material.SetFloat("_growthChange", currentGrowth);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (targetGrowth < 1f)
            {
                targetGrowth += waterGrowth;

                if (targetGrowth > 1f)
                {
                    targetGrowth = 1f;
                }
            }
        }

        if (currentGrowth != targetGrowth)
        {
            // Mathf.Lerp ile iki sayý arasýnda yumuþak bir geçiþ yapýyoruz
            currentGrowth = Mathf.Lerp(currentGrowth, targetGrowth, Time.deltaTime * growthFast);

            // Shader'a o anki yumuþatýlmýþ sayýyý gönderiyoruz
            plantRenderer.material.SetFloat("_growthChange", currentGrowth);
        }
    }
}
