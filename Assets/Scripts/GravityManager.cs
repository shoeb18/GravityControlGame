using UnityEngine;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public Vector3 GravityDirection { get; private set; } = Vector3.down;
    public float GravityForce = 9.81f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ChangeGravity(Vector3 newDirection)
    {
        GravityDirection = newDirection.normalized;
    }

    // Helper to get the rotation needed to align 'Up' with the opposite of gravity
    public Quaternion GetTargetRotation()
    {
        // We want our "Up" to point opposite to gravity
        return Quaternion.FromToRotation(Vector3.up, -GravityDirection);
    }
}