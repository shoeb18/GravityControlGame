using UnityEngine;

/// <summary>
/// Simple third-person follow camera with optional rotation using mouse.
/// Keeps camera at offset from player and smooths position/rotation.
/// </summary>

public class CameraThirdPerson : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2.2f, -4f);
    public float followSpeed = 10f;
    public float rotationSpeed = 120f;

    // Optional mouse rotation
    public bool allowMouseRotate = true;
    public float yaw = 0f;
    public float pitch = 15f; // slight downward angle
    public float minPitch = -20f;
    public float maxPitch = 60f;

    void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (allowMouseRotate)
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position + rot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}
