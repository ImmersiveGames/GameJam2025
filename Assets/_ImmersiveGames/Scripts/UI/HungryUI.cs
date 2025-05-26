using UnityEngine;
using UnityEngine.UI;

public class HungryUI : MonoBehaviour
{
    [SerializeField] WorldEaterNPC eater;
    [SerializeField] Image hungryBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        eater = GameObject.FindFirstObjectByType<WorldEaterNPC>();
    }

    // Update is called once per frame
    void Update()
    {
        hungryBar.fillAmount = eater.GetHungry();
    }
}
