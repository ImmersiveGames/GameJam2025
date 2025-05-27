using _ImmersiveGames.Scripts.EaterSystem;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.UI
{
    public class HungryUI : MonoBehaviour
    {
        [SerializeField]
        private WorldEaterNpc eater;
        [SerializeField]
        private Image hungryBar;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            eater = FindFirstObjectByType<WorldEaterNpc>();
        }

        // Update is called once per frame
        private void Update()
        {
            hungryBar.fillAmount = eater.GetHungry();
        }
    }
}
