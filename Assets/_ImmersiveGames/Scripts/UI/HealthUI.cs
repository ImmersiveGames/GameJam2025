using _ImmersiveGames.Scripts.EaterSystem;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.UI
{
    public class HealthUI : MonoBehaviour
    {
        [SerializeField]
        private WorldEaterNpc eater;
        [SerializeField]
        private Image healthBar;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            eater = GameObject.FindFirstObjectByType<WorldEaterNpc>();
        }

        // Update is called once per frame
        private void Update()
        {
            healthBar.fillAmount = eater.GetHealth();
        }
    }
}
