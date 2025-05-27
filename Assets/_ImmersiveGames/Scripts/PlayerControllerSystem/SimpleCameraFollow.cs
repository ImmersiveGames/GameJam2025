using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class SimpleCameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;

        [Header("Follow Settings")]
        public Vector3 offset = new Vector3(0f, 10f, -10f);
        public float followSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            transform.LookAt(target); // Opcional: manter a câmera sempre olhando pro alvo
        }
    }
}