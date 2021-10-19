using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// The side of the mesh
    /// </summary>
    public enum MeshSide
    {
        Positive = 0,
        Negative = 1
    }

    /// <summary>
    /// An object used to manage the positive and negative side mesh data for a sliced object
    /// </summary>
    internal class SlicesMetadata
    {
        private Mesh positiveSideMesh;
        private List<Vector3> positiveSideVertices;
        private List<int> positiveSideTriangles;
        private List<Vector2> positiveSideUvs;
        private List<Vector3> positiveSideNormals;

        private Mesh negativeSideMesh;
        private List<Vector3> negativeSideVertices;
        private List<int> negativeSideTriangles;
        private List<Vector2> negativeSideUvs;
        private List<Vector3> negativeSideNormals;

        private readonly List<Vector3> pointsAlongPlane;
        private Plane plane;
        private readonly Mesh mesh;
        private readonly bool isSolid;
        private readonly bool useSharedVertices;
        private readonly bool smoothVertices;
        private readonly bool createReverseTriangleWindings;

        public Mesh PositiveSideMesh
        {
            get
            {
                if (positiveSideMesh == null)
                {
                    positiveSideMesh = new Mesh();
                }

                SetMeshData(MeshSide.Positive);
                return positiveSideMesh;
            }
        }

        public Mesh NegativeSideMesh
        {
            get
            {
                if (negativeSideMesh == null)
                {
                    negativeSideMesh = new Mesh();
                }

                SetMeshData(MeshSide.Negative);

                return negativeSideMesh;
            }
        }

        public SlicesMetadata(Plane plane, Mesh mesh, bool isSolid, bool createReverseTriangleWindings, bool shareVertices, bool smoothVertices)
        {
            positiveSideTriangles = new List<int>();
            positiveSideVertices = new List<Vector3>();
            negativeSideTriangles = new List<int>();
            negativeSideVertices = new List<Vector3>();
            positiveSideUvs = new List<Vector2>();
            negativeSideUvs = new List<Vector2>();
            positiveSideNormals = new List<Vector3>();
            negativeSideNormals = new List<Vector3>();
            pointsAlongPlane = new List<Vector3>();
            this.plane = plane;
            this.mesh = mesh;
            this.isSolid = isSolid;
            this.createReverseTriangleWindings = createReverseTriangleWindings;
            useSharedVertices = shareVertices;
            this.smoothVertices = smoothVertices;

            ComputeNewMeshes();
        }

        private void AddTrianglesNormalAndUvs(MeshSide side, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            if (side == MeshSide.Positive)
            {
                AddTrianglesNormalsAndUvs(ref positiveSideVertices, ref positiveSideTriangles, ref positiveSideNormals, ref positiveSideUvs, vertex1, normal1, uv1, vertex2, normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            }
            else
            {
                AddTrianglesNormalsAndUvs(ref negativeSideVertices, ref negativeSideTriangles, ref negativeSideNormals, ref negativeSideUvs, vertex1, normal1, uv1, vertex2, normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            }
        }


        /// <summary>
        /// Adds the vertices to the mesh sets the triangles in the order that the vertices are provided.
        /// If shared vertices is false vertices will be added to the list even if a matching vertex already exists
        /// Does not compute normals
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="triangles"></param>
        /// <param name="uvs"></param>
        /// <param name="normals"></param>
        /// <param name="vertex1"></param>
        /// <param name="vertex1Uv"></param>
        /// <param name="normal1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex2Uv"></param>
        /// <param name="normal2"></param>
        /// <param name="vertex3"></param>
        /// <param name="vertex3Uv"></param>
        /// <param name="normal3"></param>
        /// <param name="shareVertices"></param>
        private void AddTrianglesNormalsAndUvs(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            int tri1Index = vertices.IndexOf(vertex1);

            if (addFirst)
            {
                ShiftTriangleIndexes(ref triangles);
            }

            //If a the vertex already exists we just add a triangle reference to it, if not add the vert to the list and then add the tri index
            if (tri1Index > -1 && shareVertices)
            {                
                triangles.Add(tri1Index);
            }
            else
            {
                if (normal1 == null)
                {
                    normal1 = ComputeNormal(vertex1, vertex2, vertex3);                    
                }

                int? i = null;
                if (addFirst)
                {
                    i = 0;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex1, (Vector3)normal1, uv1, i);
            }

            var tri2Index = vertices.IndexOf(vertex2);

            if (tri2Index > -1 && shareVertices)
            {
                triangles.Add(tri2Index);
            }
            else
            {
                if (normal2 == null)
                {
                    normal2 = ComputeNormal(vertex2, vertex3, vertex1);
                }
                
                int? i = null;
                
                if (addFirst)
                {
                    i = 1;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex2, (Vector3)normal2, uv2, i);
            }

            var tri3Index = vertices.IndexOf(vertex3);

            if (tri3Index > -1 && shareVertices)
            {
                triangles.Add(tri3Index);
            }
            else
            {               
                if (normal3 == null)
                {
                    normal3 = ComputeNormal(vertex3, vertex1, vertex2);
                }

                int? i = null;
                if (addFirst)
                {
                    i = 2;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex3, (Vector3)normal3, uv3, i);
            }
        }

        private void AddVertNormalUv(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles, Vector3 vertex, Vector3 normal, Vector2 uv, int? index)
        {
            if (index != null)
            {
                int i = (int)index;
                vertices.Insert(i, vertex);
                uvs.Insert(i, uv);
                normals.Insert(i, normal);
                triangles.Insert(i, i);
            }
            else
            {
                vertices.Add(vertex);
                normals.Add(normal);
                uvs.Add(uv);
                triangles.Add(vertices.IndexOf(vertex));
            }
        }

        private static void ShiftTriangleIndexes(ref List<int> triangles)
        {
            for (var j = 0; j < triangles.Count; j += 3)
            {
                triangles[j] += + 3;
                triangles[j + 1] += 3;
                triangles[j + 2] += 3;
            }
        }

        /// <summary>
        /// Will render the inside of an object
        /// This is heavy as it duplicates all the vertices and creates opposite winding direction
        /// </summary>
        private void AddReverseTriangleWinding()
        {
            var positiveVertsStartIndex = positiveSideVertices.Count;
            //Duplicate the original vertices
            positiveSideVertices.AddRange(positiveSideVertices);
            positiveSideUvs.AddRange(positiveSideUvs);
            positiveSideNormals.AddRange(FlipNormals(positiveSideNormals));

            var numPositiveTriangles = positiveSideTriangles.Count;

            //Add reverse windings
            for (var i = 0; i < numPositiveTriangles; i += 3)
            {
                positiveSideTriangles.Add(positiveVertsStartIndex + positiveSideTriangles[i]);
                positiveSideTriangles.Add(positiveVertsStartIndex + positiveSideTriangles[i + 2]);
                positiveSideTriangles.Add(positiveVertsStartIndex + positiveSideTriangles[i + 1]);
            }

            var negativeVertexStartIndex = negativeSideVertices.Count;
            //Duplicate the original vertices
            negativeSideVertices.AddRange(negativeSideVertices);
            negativeSideUvs.AddRange(negativeSideUvs);
            negativeSideNormals.AddRange(FlipNormals(negativeSideNormals));

            var numNegativeTriangles = negativeSideTriangles.Count;

            //Add reverse windings
            for (var i = 0; i < numNegativeTriangles; i += 3)
            {
                negativeSideTriangles.Add(negativeVertexStartIndex + negativeSideTriangles[i]);
                negativeSideTriangles.Add(negativeVertexStartIndex + negativeSideTriangles[i + 2]);
                negativeSideTriangles.Add(negativeVertexStartIndex + negativeSideTriangles[i + 1]);
            }
        }

        /// <summary>
        /// Join the points along the plane to the halfway point
        /// </summary>
        private void JoinPointsAlongPlane()
        {
            var halfway = GetHalfwayPoint(out _);

            for (var i = 0; i < pointsAlongPlane.Count; i += 2)
            {
                var firstVertex = pointsAlongPlane[i];
                var secondVertex = pointsAlongPlane[i + 1];

                var normal3 = ComputeNormal(halfway, secondVertex, firstVertex);
                normal3.Normalize();

                var direction = Vector3.Dot(normal3, plane.normal);

                if(direction > 0)
                {                                        
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, -normal3, Vector2.zero, firstVertex, -normal3, Vector2.zero, secondVertex, -normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, normal3, Vector2.zero, secondVertex, normal3, Vector2.zero, firstVertex, normal3, Vector2.zero, false, true);
                }
                else
                {
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, normal3, Vector2.zero, secondVertex, normal3, Vector2.zero, firstVertex, normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, -normal3, Vector2.zero, firstVertex, -normal3, Vector2.zero, secondVertex, -normal3, Vector2.zero, false, true);
                }               
            }
        }

        /// <summary>
        /// For all the points added along the plane cut, get the half way between the first and furthest point
        /// </summary>
        /// <returns></returns>
        private Vector3 GetHalfwayPoint(out float distance)
        {
            if(pointsAlongPlane.Count > 0)
            {
                var firstPoint = pointsAlongPlane[0];
                var furthestPoint = Vector3.zero;
                distance = 0f;

                foreach (var point in pointsAlongPlane)
                {
                    var currentDistance = Vector3.Distance(firstPoint, point);

                    if (currentDistance > distance)
                    {
                        distance = currentDistance;
                        furthestPoint = point;
                    }
                }

                return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
            }

            distance = 0;
            return Vector3.zero;
        }

        /// <summary>
        /// Setup the mesh object for the specified side
        /// </summary>
        /// <param name="side"></param>
        private void SetMeshData(MeshSide side)
        {
            if (side == MeshSide.Positive)
            {
                positiveSideMesh.vertices = positiveSideVertices.ToArray();
                positiveSideMesh.triangles = positiveSideTriangles.ToArray();
                positiveSideMesh.normals = positiveSideNormals.ToArray();
                positiveSideMesh.uv = positiveSideUvs.ToArray();
            }
            else
            {
                negativeSideMesh.vertices = negativeSideVertices.ToArray();
                negativeSideMesh.triangles = negativeSideTriangles.ToArray();
                negativeSideMesh.normals = negativeSideNormals.ToArray();
                negativeSideMesh.uv = negativeSideUvs.ToArray();                
            }
        }

        /// <summary>
        /// Compute the positive and negative meshes based on the plane and mesh
        /// </summary>
        private void ComputeNewMeshes()
        {
            var meshTriangles = mesh.triangles;
            var meshVerts = mesh.vertices;
            var meshNormals = mesh.normals;
            var meshUvs = mesh.uv;

            for (var i = 0; i < meshTriangles.Length; i += 3)
            {
                //We need the verts in order so that we know which way to wind our new mesh triangles.
                var vert1 = meshVerts[meshTriangles[i]];
                var vert1Index = Array.IndexOf(meshVerts, vert1);
                var uv1 = meshUvs[vert1Index];
                var normal1 = meshNormals[vert1Index];
                var vert1Side = plane.GetSide(vert1);

                var vert2 = meshVerts[meshTriangles[i + 1]];
                var vert2Index = Array.IndexOf(meshVerts, vert2);
                var uv2 = meshUvs[vert2Index];
                var normal2 = meshNormals[vert2Index];
                var vert2Side = plane.GetSide(vert2);

                var vert3 = meshVerts[meshTriangles[i + 2]];
                var vert3Side = plane.GetSide(vert3);
                var vert3Index = Array.IndexOf(meshVerts, vert3);
                var normal3 = meshNormals[vert3Index];
                var uv3 = meshUvs[vert3Index];

                //All verts are on the same side
                if (vert1Side == vert2Side && vert2Side == vert3Side)
                {
                    //Add the relevant triangle
                    var side = vert1Side ? MeshSide.Positive : MeshSide.Negative;
                    AddTrianglesNormalAndUvs(side, vert1, normal1, uv1, vert2, normal2, uv2, vert3, normal3, uv3, true, false);
                }
                else
                {
                    //we need the two points where the plane intersects the triangle.
                    Vector3 intersection1;
                    Vector3 intersection2;

                    Vector2 intersection1Uv;
                    Vector2 intersection2Uv;

                    var side1 = (vert1Side) ? MeshSide.Positive : MeshSide.Negative;
                    var side2 = (vert1Side) ? MeshSide.Negative : MeshSide.Positive;

                    //vert 1 and 2 are on the same side
                    if (vert1Side == vert2Side)
                    {
                        //Cast a ray from v2 to v3 and from v3 to v1 to get the intersections                       
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert3, uv3, vert1, uv1, out intersection2Uv);

                        //Add the positive or negative triangles
                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, vert2, null, uv2, intersection1, null, intersection1Uv, useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, useSharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, null, uv3, intersection2, null, intersection2Uv, useSharedVertices, false);

                    }
                    //vert 1 and 3 are on the same side
                    else if (vert1Side == vert3Side)
                    {
                        //Cast a ray from v1 to v2 and from v2 to v3 to get the intersections                       
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection2Uv);

                        //Add the positive triangles
                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, vert3, null, uv3, useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, vert3, null, uv3, useSharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, null, uv2, intersection2, null, intersection2Uv, useSharedVertices, false);
                    }
                    //Vert1 is alone
                    else
                    {
                        //Cast a ray from v1 to v2 and from v1 to v3 to get the intersections                       
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert3, uv3, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, useSharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, null, uv2, vert3, null, uv3, useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, null, uv3, intersection2, null, intersection2Uv, useSharedVertices, false);
                    }

                    //Add the newly created points on the plane.
                    pointsAlongPlane.Add(intersection1);
                    pointsAlongPlane.Add(intersection2);
                }
            }

            //If the object is solid, join the new points along the plane otherwise do the reverse winding
            if (isSolid)
            {
                JoinPointsAlongPlane();
            }
            else if (createReverseTriangleWindings)
            {
                AddReverseTriangleWinding();
            }

            if (smoothVertices)
            {
                SmoothVertices();
            }

        }

        /// <summary>
        /// Casts a reay from vertex1 to vertex2 and gets the point of intersection with the plan, calculates the new uv as well.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex1Uv">The vertex1 uv.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="vertex2Uv">The vertex2 uv.</param>
        /// <param name="uv">The uv.</param>
        /// <returns>Point of intersection</returns>
        private Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
        {
            var distance = GetDistanceRelativeToPlane(vertex1, vertex2, out var pointOfIntersection);
            uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
            return pointOfIntersection;
        }

        /// <summary>
        /// Computes the distance based on the plane.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="pointOfIntersection">The point ofintersection.</param>
        /// <returns></returns>
        private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfIntersection)
        {
            var ray = new Ray(vertex1, (vertex2 - vertex1));
            plane.Raycast(ray, out var distance);
            pointOfIntersection = ray.GetPoint(distance);
            return distance;
        }

        /// <summary>
        /// Get a uv between the two provided uvs by the distance.
        /// </summary>
        /// <param name="uv1">The uv1.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="distance">The distance.</param>
        /// <returns></returns>
        private static Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
        {
            var uv = Vector2.Lerp(uv1, uv2, distance);
            return uv;
        }

        /// <summary>
        /// Gets the point perpendicular to the face defined by the provided vertices        
        //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex3"></param>
        /// <returns></returns>
        private static Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            var side1 = vertex2 - vertex1;
            var side2 = vertex3 - vertex1;

            var normal = Vector3.Cross(side1, side2);

            return normal;
        }

        /// <summary>
        /// Reverese the normals in a given list
        /// </summary>
        /// <param name="currentNormals"></param>
        /// <returns></returns>
        private static IEnumerable<Vector3> FlipNormals(IEnumerable<Vector3> currentNormals)
        {
            return currentNormals.Select(normal => -normal).ToList();
        }

        //
        private void SmoothVertices()
        {
            DoSmoothing(ref positiveSideVertices, ref positiveSideNormals, ref positiveSideTriangles);
            DoSmoothing(ref negativeSideVertices, ref negativeSideNormals, ref negativeSideTriangles);
        }

        private static void DoSmoothing(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles)
        {
            normals.ForEach(x => x = Vector3.zero );

            for (var i = 0; i < triangles.Count; i += 3)
            {
                var vertIndex1 = triangles[i];
                var vertIndex2 = triangles[i + 1];
                var vertIndex3 = triangles[i + 2];

                var triangleNormal = ComputeNormal(vertices[vertIndex1], vertices[vertIndex2], vertices[vertIndex3]);

                normals[vertIndex1] += triangleNormal;
                normals[vertIndex2] += triangleNormal;
                normals[vertIndex3] += triangleNormal;
            }

            normals.ForEach(x =>
            {
                x.Normalize();
            });
        }
    }
}
