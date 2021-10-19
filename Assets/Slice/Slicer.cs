using UnityEngine;

namespace Assets.Scripts
{
    internal static class Slicer
    {
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {
            const bool isSolid = true;
            const bool reverseWindTriangles = false;
            const bool shareVertices = false;
            const bool smoothVertices = false;

            var mesh = objectToCut.GetComponent<MeshFilter>().mesh;

            var slicesMeta = new SlicesMetadata(plane, mesh, isSolid, reverseWindTriangles, shareVertices, smoothVertices);

            var positiveObject = CreateMeshGameObject(objectToCut);
            positiveObject.name = $"{objectToCut.name}_positive";

            var negativeObject = CreateMeshGameObject(objectToCut);
            negativeObject.name = $"{objectToCut.name}_negative";

            var positiveSideMeshData = slicesMeta.PositiveSideMesh;
            var negativeSideMeshData = slicesMeta.NegativeSideMesh;
            positiveSideMeshData.RecalculateNormals();
            negativeSideMeshData.RecalculateNormals();

            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

            

            return new[] { positiveObject, negativeObject };
        }

        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;

            var meshGameObject = new GameObject();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();

            meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }
    }
}