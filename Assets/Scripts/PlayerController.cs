using UnityEngine;

/// <summary>
/// PlayerController Script Handles movement (WASD), jump (Space), grounding checks and links to Animator.
/// Uses CharacterController for movement.
/// </summary>

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runMultiplier = 1.0f;
    public float jumpHeight = 1.6f;
    public float gravityScale = 1.0f; // local scale for gravity applied when using CharacterController

    [Header("Grounding")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Animation")]
    public Animator animator;

    // internal
    private CharacterController cc;
    private Vector3 velocity;
    private bool isGrounded;
    
    private Vector3 lastMoveInput;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            groundCheck = go.transform;
        }
    }

    void Update()
    {
        HandleMovement();
        UpdateAnimator();
    }

    private void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // keep grounded

        // Input
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical"); // W/S

        // Move relative to camera forward/right
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = cam.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 move = (camForward * v + camRight * h).normalized;
        if (move.magnitude > 0.01f)
        {
            // rotate character towards movement direction
            transform.forward = Vector3.Slerp(transform.forward, move, Time.deltaTime * 10f);
        }

        cc.Move(move * moveSpeed * Time.deltaTime);
        lastMoveInput = move * moveSpeed;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics.gravity.y) * gravityScale);
            if (animator) animator.SetBool("Jump", true);
        }

        // Apply gravity scaled (charactercontroller)
        velocity.y += Physics.gravity.y * Time.deltaTime * gravityScale;
        cc.Move(velocity * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator)
        {
            float speed = lastMoveInput.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("Grounded", isGrounded);
            if (isGrounded) animator.SetBool("Jump", false);
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }
}
