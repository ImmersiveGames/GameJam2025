using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    public class TesteInstance : MonoBehaviour
    {
        [SerializeField] private GameObject prefabPlanet;
        [SerializeField] private GameObject prefabPlayer2;
        [SerializeField] private int quant = 3;

        private void Start()
        {
            for (int i = 0; i < quant; i++)
            {
                if(prefabPlanet)
                    Instantiate(prefabPlanet, transform);
            }
            if(prefabPlayer2)
                Instantiate(prefabPlayer2, transform);
        }
    }
}