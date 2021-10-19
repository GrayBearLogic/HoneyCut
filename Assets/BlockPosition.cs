using UnityEngine;

public class BlockPosition : MonoBehaviour
{
    [SerializeField] private Bounds bounds;
    
    private Vector3 lastPosition;

    private void LateUpdate()
    {
        var relativePos = transform.position - bounds.center;

        if (Mathf.Abs(relativePos.x) > bounds.extents.x ||
            Mathf.Abs(relativePos.y) > bounds.extents.y ||
            Mathf.Abs(relativePos.z) > bounds.extents.z)
        {
            transform.position = lastPosition;
        }

        lastPosition = transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
    }
}