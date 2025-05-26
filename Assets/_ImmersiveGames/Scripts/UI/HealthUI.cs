using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] WorldEaterNPC eater;
    [SerializeField] Image healthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        eater = GameObject.FindFirstObjectByType<WorldEaterNPC>();
    }

    // Update is called once per frame
    void Update()
    {
        healthBar.fillAmount = eater.GetHealth();
    }
}
