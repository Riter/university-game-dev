using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalkController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public Transform cameraPivot;
    public Transform resetPoint;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -75f;
    public float maxPitch = 75f;

    private CharacterController characterController;
    private float pitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            cameraPivot = transform;
        }
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        UpdateCursorLock();
        UpdateLook();
        UpdateMovement();
    }

    public void ResetToSpawn()
    {
        if (resetPoint == null)
        {
            return;
        }

        characterController.enabled = false;
        transform.position = resetPoint.position;
        transform.rotation = resetPoint.rotation;
        characterController.enabled = true;

        pitch = 0f;
        cameraPivot.localRotation = Quaternion.identity;
    }

    private void UpdateCursorLock()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private void UpdateLook()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 lookDelta = Mouse.current.delta.ReadValue();
        transform.Rotate(Vector3.up, lookDelta.x * mouseSensitivity);

        pitch = Mathf.Clamp(pitch - lookDelta.y * mouseSensitivity, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateMovement()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            input.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 movement = (forward * input.y + right * input.x) * moveSpeed;
        characterController.SimpleMove(movement);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
