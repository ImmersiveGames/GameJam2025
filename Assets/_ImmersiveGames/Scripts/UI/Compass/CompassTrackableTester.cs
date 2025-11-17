using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UI.Compass
{
    public class CompassTrackableTester : MonoBehaviour
    {
        [Header("Arraste aqui um objeto com CompassTarget")]
        public GameObject targetObject;

        private ICompassTrackable _trackable;

        private void Start()
        {
            if (targetObject != null)
            {
                _trackable = targetObject.GetComponent<ICompassTrackable>();
                if (_trackable == null)
                {
                    Debug.LogError("[CompassTrackableTester] O objeto não possui um componente que implemente ICompassTrackable.");
                }
                else
                {
                    Debug.Log("[CompassTrackableTester] Trackable encontrado com sucesso.");
                }
            }
            else
            {
                Debug.LogWarning("[CompassTrackableTester] Nenhum objeto atribuído. Arraste um GameObject com CompassTarget no Inspector.");
            }
        }

        private void Update()
        {
            // Pressionar T para testar
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (_trackable == null)
                {
                    Debug.LogWarning("[CompassTrackableTester] Nenhum trackable encontrado para testar.");
                    return;
                }

                Debug.Log(
                    $"[CompassTrackableTester]\n" +
                    $"Transform: {_trackable.Transform.name}\n" +
                    $"TargetType: {_trackable.TargetType}\n" +
                    $"IsActive: {_trackable.IsActive}\n" +
                    $"Posição atual: {_trackable.Transform.position}"
                );
            }
        }
    }
}