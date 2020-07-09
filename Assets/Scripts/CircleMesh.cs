using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Heppoko
{
    /// <summary>Draw circle mesh.</summary>
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CircleMesh : MonoBehaviour
    {
        /// <summary>Material for mesh</summary>
        public Material material;

        /// <summary>Edges count for outer boundary.</summary>
        [Range(3, 200)]
        public int outerEdgesCount = 16;

        /// <summary>Rate of visible cicle.</summary>
        [Range(0.0f, 1.0f)]
        public float visibleRate = 1.0f;

        /// <summary>Hole's radius rate against RectTransform's width and height.</summary>
        [Range(0.0f, 1.0f)]
        public float holeRate = 0.0f;

        /// <summary>Start point of the arc.</summary>
        public float angleOffset = 0.0f;

        /// <summary>Whether the Image should be filled clockwise (true) or counter-clockwise (false).</summary>
        public bool clockWise = true;

        /// <summary>Mesh width</summary>
        public float meshWidth = 1.0f;

        /// <summary>Mesh height</summary>
        public float meshHeight = 1.0f;

        /// <summary>Current outer edges count</summary>
        private int visibleOuterEdgesCount
        {
            get
            {
                return (int)(visibleRate * outerEdgesCount);
            }
        }

        private bool isDirty { get; set; } = false;

        private Mesh mesh { get; set; }

        private MeshFilter meshFilter { get; set; }

        private MeshRenderer meshRenderer { get; set; }

        private float lastOuterEdgesCount { get; set; } = -1.0f;

        private float lastWidth { get; set; } = -1.0f;

        private float lastHeight { get; set; } = -1.0f;

        private float lastVisibleRate { get; set; } = -1.0f;

        private float lastHoleRate { get; set; } = -1.0f;

        [System.NonSerialized]
        private List<Vector3> vertices = new List<Vector3>();

        [System.NonSerialized]
        private List<Vector2> uvs = new List<Vector2>();

        [System.NonSerialized]
        private List<int> triangles = new List<int>();

        /// <summary>
        /// Show circle with specified visible rate.
        /// </summary>
        /// <param name="rate">rate of visible circle. Range from 0.0f to 1.0f.</param>
        public void Show(float rate)
        {
            visibleRate = Mathf.Clamp(rate, 0.0f, 1.0f);
            MarkAsDirty();
        }

        /// <summary>Force update</summary>
        public void MarkAsDirty()
        {
            isDirty = true;
        }

        /// <summary>Need to re-create mesh's vertices?</summary>
        private bool IsDirty()
        {
            if (mesh == null) return true;
            if (isDirty) return true;
            if (!Mathf.Approximately(outerEdgesCount, lastOuterEdgesCount)) return true;
            if (!Mathf.Approximately(meshWidth, lastWidth)) return true;
            if (!Mathf.Approximately(meshHeight, lastHeight)) return true;
            if (!Mathf.Approximately(visibleRate, lastVisibleRate)) return true;
            if (!Mathf.Approximately(holeRate, lastHoleRate)) return true;

            return false;
        }

        private void Update()
        {
            if (IsDirty())
            {
                CreateMesh();

                if (holeRate > 0)
                {
                    CreateVerticesWithHole();
                }
                else
                {
                    CreateVertices();
                }

                UpdateProperties();
            }
        }

        /// <summary>Update dirty info.</summary>
        private void UpdateProperties()
        {
            isDirty = false;
            lastOuterEdgesCount = outerEdgesCount;
            lastWidth = meshWidth;
            lastHeight = meshHeight;
            lastVisibleRate = visibleRate;
            lastHoleRate = holeRate;
        }

        /// <summary>Create mesh if not exists.</summary>
        private void CreateMesh()
        {
            if (mesh != null) return;

            mesh = new Mesh();

            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }

        /// <summary>Create vertices for circle.</summary>
        private void CreateVertices()
        {

            triangles.Clear();
            triangles.Capacity = outerEdgesCount * 3;

            vertices.Clear();
            vertices.Capacity = outerEdgesCount;
            vertices.Add(new Vector3(0, 0, 0));

            uvs.Clear();
            uvs.Capacity = outerEdgesCount;
            uvs.Add(new Vector2(0.5f, 0.5f));

            if (clockWise)
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    triangles.Add(0);
                    triangles.Add(i + 1);
                    triangles.Add(i + 2);
                }
            }
            else
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    triangles.Add(0);
                    triangles.Add(i + 2);
                    triangles.Add(i + 1);
                }
            }

            for (int i = 0; i < visibleOuterEdgesCount + 1; i++)
            {
                // Calculate angle of i th vertex.

                float angle;
                if (clockWise)
                {
                    angle = (2.0f * Mathf.PI * i) / outerEdgesCount + angleOffset;
                }
                else
                {
                    angle = -(2.0f * Mathf.PI * i) / outerEdgesCount + angleOffset;
                }

                Vector3 pos = new Vector3(meshWidth * Mathf.Sin(angle), meshHeight * Mathf.Cos(angle));
                vertices.Add(pos);

                Vector2 uv0 = new Vector2(0.5f + 0.5f * Mathf.Sin(angle), 0.5f + 0.5f * Mathf.Cos(angle));
                uvs.Add(uv0);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
        }

        /// <summary>Create vertices of the holed circle.</summary>
        private void CreateVerticesWithHole()
        {
            triangles.Clear();
            triangles.Capacity = (outerEdgesCount * 2 + 2) * 3;

            vertices.Clear();
            vertices.Capacity = outerEdgesCount * 2 + 2;

            uvs.Clear();
            uvs.Capacity = outerEdgesCount * 2 + 2;

            if (clockWise)
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    triangles.Add(i * 2 + 0);
                    triangles.Add(i * 2 + 1);
                    triangles.Add(i * 2 + 3);

                    triangles.Add(i * 2 + 0);
                    triangles.Add(i * 2 + 3);
                    triangles.Add(i * 2 + 2);
                }
            }
            else
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    triangles.Add(i * 2 + 0);
                    triangles.Add(i * 2 + 3);
                    triangles.Add(i * 2 + 1);

                    triangles.Add(i * 2 + 0);
                    triangles.Add(i * 2 + 2);
                    triangles.Add(i * 2 + 3);
                }
            }

            for (int i = 0; i < visibleOuterEdgesCount + 1; i++)
            {
                // Calculate angle of i th vertex.

                float angle;
                if (clockWise)
                {
                    angle = (2.0f * Mathf.PI * i) / outerEdgesCount + angleOffset / 180.0f * Mathf.PI;
                }
                else
                {
                    angle = -(2.0f * Mathf.PI * i) / outerEdgesCount + angleOffset / 180.0f * Mathf.PI;
                }

                // Inner vertex
                {
                    Vector3 pos = new Vector3(holeRate * meshWidth * Mathf.Sin(angle), holeRate * meshHeight * Mathf.Cos(angle));
                    vertices.Add(pos);

                    Vector2 uv0 = new Vector2(0.5f + holeRate * 0.5f * Mathf.Sin(angle), 0.5f + holeRate * 0.5f * Mathf.Cos(angle));
                    uvs.Add(uv0);
                }

                // Outer vertex
                {
                    Vector3 pos = new Vector3(meshWidth * Mathf.Sin(angle), meshHeight * Mathf.Cos(angle));
                    vertices.Add(pos);

                    Vector2 uv0 = new Vector2(0.5f + 0.5f * Mathf.Sin(angle), 0.5f + 0.5f * Mathf.Cos(angle));
                    uvs.Add(uv0);
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
        }
    }
}
