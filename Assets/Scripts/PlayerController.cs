using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public float rotationSpeed = 15f; 
    
    [Header("Gravity Settings")]
    public Transform hologramMesh;
    public float gravitySwitchSpeed = 2f;

    [Header("References")]
    public Transform cameraTransform; 
    public Animator animator;

    private Rigidbody rb;
    private bool _isGrounded;
    private float _distToGround;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
        rb.freezeRotation = true; 
        
        _distToGround = GetComponent<CapsuleCollider>().bounds.extents.y;

        if(hologramMesh) hologramMesh.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleGravityInput();
        HandleAnimation();
    }

    void FixedUpdate()
    {
        ApplyGravity();

        // Calculate and Apply Orientation FIRST
        // This ensures the capsule is upright before we try to move it
        Quaternion targetRotation = CalculateOrientation();
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        // Apply Velocity based on the new rotation
        HandleVelocity();
        
        // ground status check
        CheckGroundStatus();
    }


    Quaternion CalculateOrientation()
    {
        // calculate direction (Opposite to gravity)
        Vector3 targetUp = -GravityManager.Instance.GravityDirection;
        
        Vector3 targetForward;
        
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.magnitude > 0.1f)
        {
            // Moving - Face the direction of camera-relative movement
            Vector3 camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, targetUp).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, targetUp).normalized;
            targetForward = (camFwd * inputDir.z + camRight * inputDir.x).normalized;
        }
        else
        {
            // Idle/Falling -Keep facing the current direction, but flattened on the new surface
            targetForward = Vector3.ProjectOnPlane(transform.forward, targetUp).normalized;

            // If looking straight up/down relative to new gravity, use Camera forward to prevent spinning
            if (targetForward.sqrMagnitude < 0.01f)
            {
                targetForward = Vector3.ProjectOnPlane(cameraTransform.forward, targetUp).normalized;
            }
        }

        // Return the corrected rotation
        return Quaternion.LookRotation(targetForward, targetUp);
    }

    // movement
    void HandleVelocity()
    {
        Vector3 targetUp = -GravityManager.Instance.GravityDirection;
        
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Only apply movement force if input exists
        if (new Vector3(h, 0, v).magnitude > 0.1f)
        {
            // transform.forward because we already calculated the correct rotation 
            Vector3 moveDir = transform.forward * moveSpeed;

            // preserve falling speed (Vertical Velocity)
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, targetUp);
            
            rb.linearVelocity = verticalVelocity + moveDir;
        }
        else
        {
            // stop sliding if no input
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, targetUp);
            // Lerp horizontal velocity to zero
            rb.linearVelocity = Vector3.Lerp(currentVelocity, verticalVelocity, Time.fixedDeltaTime * 10f);
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            rb.AddForce(targetUp * jumpForce, ForceMode.Impulse);
            if(animator) animator.SetTrigger("Jump");
        }
    }

    // Gravity Inputs
    void HandleGravityInput()
    {
        Vector3 inputDir = Vector3.zero;

        // Camera-Relative directions
        if (Input.GetKey(KeyCode.UpArrow)) inputDir = cameraTransform.forward;
        else if (Input.GetKey(KeyCode.DownArrow)) inputDir = -cameraTransform.forward;
        else if (Input.GetKey(KeyCode.LeftArrow)) inputDir = -cameraTransform.right;
        else if (Input.GetKey(KeyCode.RightArrow)) inputDir = cameraTransform.right;

        if (inputDir != Vector3.zero)
        {
            inputDir = SnapToAxis(inputDir);

            if (hologramMesh)
            {
                hologramMesh.gameObject.SetActive(true);
                hologramMesh.position = transform.position + (transform.up * 1.75f); // hologram offset

                Vector3 targetUp = -inputDir; // hologram rotaton
                
                // Project player's current forward onto the new gravity wall so the hologram matches player facing
                Vector3 targetFwd = Vector3.ProjectOnPlane(transform.forward, targetUp).normalized;
                
                // fallback if projection fails
                if(targetFwd == Vector3.zero) targetFwd = Vector3.ProjectOnPlane(cameraTransform.forward, targetUp).normalized;

                // lookRotation to fix the Inverse/Backward issue
                Quaternion targetRot = Quaternion.LookRotation(targetFwd, targetUp);
                
                hologramMesh.rotation = Quaternion.Slerp(hologramMesh.rotation, targetRot, Time.deltaTime * 10f);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                GravityManager.Instance.ChangeGravity(inputDir);
                if(hologramMesh) hologramMesh.gameObject.SetActive(false);
            }
        }
        else
        {
             if(hologramMesh) hologramMesh.gameObject.SetActive(false);
        }
    }

    void ApplyGravity()
    {
        rb.AddForce(GravityManager.Instance.GravityDirection * GravityManager.Instance.GravityForce * rb.mass, ForceMode.Force);
    }

    void CheckGroundStatus()
    {
        float offset = 0.5f;
        Vector3 origin = transform.position + (transform.up * offset);
        float radius = 0.3f;
        float castDistance = offset + 0.1f;

        // sphere cast to check isgrounded or not
        _isGrounded = Physics.SphereCast(origin, radius, -transform.up, out RaycastHit hit, castDistance);
        
        if(GameManager.Instance) GameManager.Instance.UpdateFallState(_isGrounded);
    }
    
    // Animations
    void HandleAnimation()
    {
        if(!animator) return;
        
        Vector3 velocity = rb.linearVelocity;
        Vector3 surfaceNormal = -GravityManager.Instance.GravityDirection;
        Vector3 moveVelocity = Vector3.ProjectOnPlane(velocity, surfaceNormal);
        
        animator.SetFloat("Speed", moveVelocity.magnitude);
        animator.SetBool("IsGrounded", _isGrounded);
    }

    Vector3 SnapToAxis(Vector3 v)
    {
        float max = 0;
        Vector3 best = Vector3.zero;
        if(Mathf.Abs(v.x) > max) { max = Mathf.Abs(v.x); best = new Vector3(Mathf.Sign(v.x),0,0); }
        if(Mathf.Abs(v.y) > max) { max = Mathf.Abs(v.y); best = new Vector3(0,Mathf.Sign(v.y),0); }
        if(Mathf.Abs(v.z) > max) { max = Mathf.Abs(v.z); best = new Vector3(0,0,Mathf.Sign(v.z)); }
        return best;
    }
    
    // Collect Pickups
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup"))
        {
            if (audioSource) audioSource.Play();
            if(GameManager.Instance) GameManager.Instance.CollectCube();
            Destroy(other.gameObject);
        }
    }
}