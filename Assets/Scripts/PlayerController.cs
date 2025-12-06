using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public float rotationSpeed = 15f; // Increased for snappier turning
    
    [Header("Gravity Settings")]
    public Transform hologramMesh;
    public float gravitySwitchSpeed = 2f;

    [Header("References")]
    public Transform cameraTransform; // Drag Main Camera (or Camera Pivot) here
    public Animator animator;

    private Rigidbody rb;
    private bool _isGrounded;
    private float _distToGround;

    // We track the 'Up' direction we want to align to (opposite of gravity)
    private Vector3 _currentUpDirection = Vector3.up;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
        rb.freezeRotation = true; 
        
        _distToGround = GetComponent<CapsuleCollider>().bounds.extents.y;
        _currentUpDirection = -GravityManager.Instance.GravityDirection;

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
        HandleMovement(); // Updated movement logic
        CheckGroundStatus();
    }

    // --- IMPROVED MOVEMENT LOGIC ---
    void HandleMovement()
    {
        // 1. Get Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        // 2. Define the surface normal (Our "Up" is opposite to gravity)
        Vector3 surfaceNormal = -GravityManager.Instance.GravityDirection;

        // 3. Calculate Move Direction relative to Camera
        if (inputDir.magnitude >= 0.1f)
        {
            // Get Camera vectors
            Vector3 camFwd = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Project camera vectors onto the current surface plane
            // This ensures "Forward" is always along the floor/wall, not into it
            camFwd = Vector3.ProjectOnPlane(camFwd, surfaceNormal).normalized;
            camRight = Vector3.ProjectOnPlane(camRight, surfaceNormal).normalized;

            // Combine inputs with projected camera vectors
            Vector3 targetMoveDir = (camFwd * inputDir.z + camRight * inputDir.x).normalized;

            // 4. Rotate Character to face movement
            // LookRotation aligns 'forward' to moveDir and 'up' to surfaceNormal
            Quaternion targetRotation = Quaternion.LookRotation(targetMoveDir, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            // 5. Apply Velocity (Preserve falling speed)
            Vector3 currentVelocity = rb.linearVelocity;
            
            // Project current velocity onto the 'Up' vector to keep the falling/jumping component
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, surfaceNormal);
            
            // Apply new horizontal velocity
            rb.linearVelocity = verticalVelocity + (targetMoveDir * moveSpeed);
        }
        else
        {
            // If no input, just align body to surface normal (keep feet down)
            // But don't change facing direction
            Quaternion currentLook = Quaternion.LookRotation(transform.forward, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentLook, rotationSpeed * Time.fixedDeltaTime);

            // Dampen horizontal velocity (stop sliding)
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, surfaceNormal);
            rb.linearVelocity = Vector3.Lerp(currentVelocity, verticalVelocity, Time.fixedDeltaTime * 10f);
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            // Jump in the direction of surface normal
            rb.AddForce(surfaceNormal * jumpForce, ForceMode.Impulse);
            if(animator) animator.SetTrigger("Jump");
        }
    }
    // -------------------------------

    void HandleGravityInput()
    {
        Vector3 inputDir = Vector3.zero;

        // Use Camera-Relative directions for Gravity Selection too
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
                hologramMesh.position = transform.position;
                
                // Align hologram feet to the NEW gravity direction
                // If gravity goes 'Left', feet point 'Right'
                Quaternion targetRot = Quaternion.FromToRotation(Vector3.up, -inputDir); 
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
        _isGrounded = Physics.Raycast(transform.position, -transform.up, _distToGround + 0.2f);
        if(GameManager.Instance) GameManager.Instance.UpdateFallState(_isGrounded);
    }
    
    void HandleAnimation()
    {
        if(!animator) return;
        
        // Calculate speed excluding vertical movement (gravity)
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
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup"))
        {
            if(GameManager.Instance) GameManager.Instance.CollectCube();
            Destroy(other.gameObject);
        }
    }
}