using UnityEngine;
namespace Starfield_Skybox.Demo.Script
{
    public class LookCamera : MonoBehaviour
    {
        public float speedNormal = 10.0f;
        public float speedFast   = 50.0f;

        public float mouseSensitivityX = 5.0f;
        public float mouseSensitivityY = 5.0f;

        float _rotY;

        void Start()
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().freezeRotation = true;
            }
        }

        void Update()
        {
            // rotation
            if (Input.GetMouseButton(1))
            {
                float rotX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivityX;
                _rotY += Input.GetAxis("Mouse Y") * mouseSensitivityY;
                _rotY = Mathf.Clamp(_rotY, -89.5f, 89.5f);
                transform.localEulerAngles = new Vector3(-_rotY, rotX, 0.0f);
            }

            if (Input.GetKey(KeyCode.U))
            {
                gameObject.transform.localPosition = new Vector3(0.0f, 3500.0f, 0.0f);
            }

        }
    }
}
