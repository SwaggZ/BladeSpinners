using UnityEngine;

namespace BladeSpinners.Gameplay.PartDebugging
{
    public class DebugFlyCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float boostMultiplier = 3f;
        [SerializeField] private float lookSensitivity = 2.2f;

        private float yaw;
        private float pitch;
        private bool cursorLocked;

        private void Start()
        {
            Vector3 e = transform.rotation.eulerAngles;
            yaw = e.y;
            pitch = e.x;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                SetCursorLock(true);
            if (Input.GetMouseButtonUp(1))
                SetCursorLock(false);

            if (cursorLocked)
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f);
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            float upDown = 0f;

            if (Input.GetKey(KeyCode.E)) upDown += 1f;
            if (Input.GetKey(KeyCode.Q)) upDown -= 1f;

            Vector3 move = (transform.forward * vertical + transform.right * horizontal + Vector3.up * upDown).normalized;
            transform.position += move * speed * Time.deltaTime;
        }

        private void SetCursorLock(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}