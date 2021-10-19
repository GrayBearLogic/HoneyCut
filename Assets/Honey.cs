using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Honey : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material capMaterial;
    [Space]
    [SerializeField] private Transform startPointObj;
    [SerializeField] private Transform endPointObj;
    [Space] 
    [SerializeField] private float density = 0.01f;
    [SerializeField] private float rollAngle = 40f;
    [SerializeField] private float heightDestroy = 0f;

    [HideInInspector] public float wholeDistanceDiff = 0;

    private Mesh mesh;
    private Vector3[] initVertices;

    private float rollProgress = 0.0001f;
    private float lastWholeDistance = 0;

    public bool IsRolling { get; private set; }
    
    private void Start()
    {
        mesh = meshFilter.mesh;
        initVertices = mesh.vertices;
    }

    private void Update()
    {
        var startPoint = startPointObj.position;
        startPoint.z = 0;
        var endPoint = endPointObj.position;
        endPoint.z = 0;
        
        var wholeDistance = (endPoint - startPoint).magnitude;
        wholeDistanceDiff = wholeDistance - lastWholeDistance;
        if (endPoint.y < startPoint.y && wholeDistanceDiff > 0)
        {
            IsRolling = true;
            
            rollProgress = wholeDistance;
            var direction = (endPoint - startPoint).normalized;
            RollMesh(direction, startPoint, wholeDistance);
        }
        else
        {
            IsRolling = false;
        }
        lastWholeDistance = (endPoint - startPoint).magnitude;

    }

    private void RollMesh(Vector3 direction, Vector3 startPoint, float wholeDistance)
    { 
        var newVertices = new Vector3[initVertices.Length];
        Array.Copy(initVertices, newVertices, initVertices.Length);

        for (var distance = 0f; distance <= wholeDistance; distance += density)
        {
            var pivot = startPoint + direction * distance;
            var depthProportion = 1 - distance / wholeDistance;

            var rotation = Quaternion.Euler(0, 0, rollAngle * depthProportion);

            for (var vertexId = 0; vertexId < newVertices.Length; vertexId++)
            {
                if (initVertices[vertexId].y >= pivot.y)
                {
                    newVertices[vertexId] = RotateAroundPoint(newVertices[vertexId], pivot, rotation);
                }
            }
        }

        meshFilter.mesh.vertices = newVertices;
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();
    }
    
    public bool SliceUp(Vector3 slicePoint)
    {
        if (startPointObj.position.y <= endPointObj.position.y) return false;

        var slices = BLINDED_AM_ME.MeshCut.Cut(gameObject, slicePoint - transform.position, Vector3.up, capMaterial);

        slices[1].AddComponent<MeshCollider>().convex = true;
        slices[1].AddComponent<Rigidbody>();
        slices[1].AddComponent<DestroyOnHeight>().height = heightDestroy;

        mesh = meshFilter.mesh;
        initVertices = mesh.vertices;
        rollProgress = 0.0001f;

        return true;
    }
    
    private static Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }
}