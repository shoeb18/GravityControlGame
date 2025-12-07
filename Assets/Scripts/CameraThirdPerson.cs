using UnityEngine;

public class CameraThirdPerson : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 5.0f;
    public float heightOffset = 1.5f;

    [Header("Input")]
    public float sensitivityX = 4.0f;
    public float sensitivityY = 2.0f;
    public float yMinLimit = -40f;
    public float yMaxLimit = 80f;

    [Header("Smoothing")]
    public float followSpeed = 10f; 
    public float rotationSpeed = 10f; 

    [Header("Collision")]
    public LayerMask collisionLayers; // Layers that camera have to ingnore (uncheck player and ui)
    public float collisionBuffer = 0.2f; // Pus camera slightly away from wall

    private float currentX = 0.0f;
    private float currentY = 20.0f;
    private float currentDistance; // actual distance being used (changes when hitting walls)

    void Start()
    {
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        currentDistance = distance;
    }

    void LateUpdate()
    {
        if (!target) return;

        // mouse Input
        currentX += Input.GetAxis("Mouse X") * sensitivityX;
        currentY -= Input.GetAxis("Mouse Y") * sensitivityY;
        currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

        // rotation
        Vector3 gravityUp = -GravityManager.Instance.GravityDirection;
        Quaternion gravityOrientation = Quaternion.FromToRotation(Vector3.up, gravityUp);
        Quaternion localRotation = Quaternion.Euler(currentY, currentX, 0);
        Quaternion targetRotation = gravityOrientation * localRotation;

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        // calculate ideal position
        Vector3 focusPoint = target.position + (gravityUp * heightOffset);
        Vector3 dir = targetRotation * -Vector3.forward; 
        
        
        // collision checks for blocked area
        Vector3 targetPosition = focusPoint + (dir * distance); // Max distance position
        
        // Raycast from the Player's Head towards the Camera's ideal position
        RaycastHit hit;
        if (Physics.Linecast(focusPoint, targetPosition, out hit, collisionLayers))
        {
            // If we hit a wall, move the camera to the hit point (minus a small buffer)
            currentDistance = Vector3.Distance(focusPoint, hit.point) - collisionBuffer;
            // Clamp so we don't zoom inside the player's head (min 0.5f)
            currentDistance = Mathf.Max(currentDistance, 0.5f);
        }
        else
        {
            // If No wall? then Smoothly return to max distance
            currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * 20f);
        }

        // collison end
        // Final Position
        Vector3 finalPosition = focusPoint + (dir * currentDistance);
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * followSpeed);
    }
}