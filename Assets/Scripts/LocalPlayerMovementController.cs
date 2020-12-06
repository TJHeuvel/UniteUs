using UnityEngine;
using UnityEngine.InputSystem;

class LocalPlayerMovementController : MonoBehaviour
{
    [SerializeField] private float collisionRadius = 0.6f, skinWidth = 0.01f;
    private float movementSpeed => LobbyManager.Instance.GameSettings.Value.MovementSpeed;

    private RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

    void Update()
    {
        Vector3 dir = Vector2.zero;
        if (Keyboard.current.wKey.isPressed)
            dir += Vector3.up;
        if (Keyboard.current.sKey.isPressed)
            dir += Vector3.down;
        if (Keyboard.current.dKey.isPressed)
            dir += Vector3.right;
        if (Keyboard.current.aKey.isPressed)
            dir += Vector3.left;
        
        dir.Normalize();
        float distance = movementSpeed * Time.deltaTime;

        //TODO: Improve this, movement is pretty crap. 
        int hits = Physics2D.CircleCastNonAlloc(transform.position, collisionRadius, dir, hitBuffer, distance);

        for (int i = 0; i < hits; i++)
            distance = Mathf.Min(distance, hitBuffer[i].distance) - skinWidth;            

        transform.position += dir * distance;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}
