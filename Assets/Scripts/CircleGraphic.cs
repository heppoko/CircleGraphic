using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeppokoUtil
{
    /// <summary>
    /// Draw circle on canvas.
    /// </summary>
    public class CircleGraphic : MaskableGraphic, ILayoutElement
    {
        /// <summary>Edges count for outer boundary.</summary>
        [Range(3, 200)]
        public int outerEdgesCount = 16;

        /// <summary>Rate of visible cicle.</summary>
        [Range(0.0f, 1.0f)]
        public float visibleRate = 1.0f;

        /// <summary>Hole's radius rate against RectTransform's width and height.</summary>
        [Range(0.0f, 1.0f)]
        public float holeRate = 0.0f;

        /// <summary>One of the points of the Arc.</summary>
        public Image.Origin360 origin;

        /// <summary>Whether the Image should be filled clockwise (true) or counter-clockwise (false).</summary>
        public bool clockWise = true;

        /// <summary>Sprite for this image.</summary>        
        public Sprite _sprite;
        public Sprite sprite
        {
            get
            {
                return _sprite;
            }
            set
            {
                if (_sprite != null)
                {
                    if (_sprite != value)
                    {
                        _sprite = value;
                        SetAllDirty();
                    }
                }
                else if (value != null)
                {
                    _sprite = value;
                    SetAllDirty();
                }

                if (_sprite != null)
                {
                    float x = sprite.uv[0].x;
                    float y = sprite.uv[1].y;

                    spriteUVWidth = sprite.uv[1].x - sprite.uv[0].x;
                    spriteUVHeight = sprite.uv[0].y - sprite.uv[1].y;
                    spriteCenterX = x + spriteUVWidth * 0.5f;
                    spriteCenterY = y + spriteUVHeight * 0.5f;
                }
            }
        }

        /// <summary>`mainTexture` from Sprite or Material.</summary>
        public override Texture mainTexture
        {
            get
            {
                if (_sprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return _sprite.texture;
            }
        }

        /// <summary>Vertices count when rate = 1.0f.</summary>
        private int maxVerticesCount
        {
            get
            {
                if (holeRate > 0)
                {
                    return outerEdgesCount * 2 + 2;
                }
                else
                {
                    return outerEdgesCount + 2;
                }
            }
        }

        /// <summary>Indicies count when rate = 1.0f.</summary>
        private int maxIndiciesCount
        {
            get
            {
                if (holeRate > 0)
                {
                    return outerEdgesCount * 6;
                }
                else
                {
                    return outerEdgesCount * 3;
                }
            }
        }

        /// <summary>Current outer edges count</summary>
        private int visibleOuterEdgesCount
        {
            get
            {
                return (int)(visibleRate * outerEdgesCount);
            }
        }

        private float spriteUVWidth { get; set; } = 1.0f;

        private float spriteUVHeight { get; set; } = 1.0f;
        
        private float spriteCenterX { get; set; } = 0.5f;
        
        private float spriteCenterY { get; set; } = 0.5f;
        
        private float angleOffset { get; set; }
        
        private RectTransform rectTransformCache { get; set; }

        /// <summary>Triangle's vertices.</summary>
        [System.NonSerialized]
        private List<UIVertex> vertices = new List<UIVertex>();

        /// <summary>Triangle's indicies.</summary>
        [System.NonSerialized]
        private List<int> indices = new List<int>();

        #region ILayoutElement
        /// <summary>
        /// See Image.m_CachedReferencePixelsPerUnit.
        /// </summary>
        private float cachedReferencePixelsPerUnit = 100;

        /// <summary>
        /// See Image.pixelsPerUnit.
        /// </summary>
        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (sprite != null)
                {
                    spritePixelsPerUnit = sprite.pixelsPerUnit;
                }

                if (canvas != null)
                {
                    cachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                }

                return spritePixelsPerUnit / cachedReferencePixelsPerUnit;
            }
        }

        /// <summary>
        /// See ILayoutElement.minWidth.
        /// </summary>
        public virtual float minWidth { get { return 0; } }

        /// <summary>
        /// See ILayoutElement.minHeight.
        /// </summary>
        public virtual float minHeight { get { return 0; } }

        /// <summary>
        /// Returns sprite's width.
        /// </summary>
        public virtual float preferredWidth
        {
            get
            {
                if (sprite == null)
                {
                    return 0;
                }
                return sprite.rect.size.x / pixelsPerUnit;
            }
        }

        /// <summary>
        /// Returns sprite's height.
        /// </summary>
        public float preferredHeight
        {
            get
            {
                if (sprite == null)
                {
                    return 0;
                }
                return sprite.rect.size.y / pixelsPerUnit;
            }
        }

        /// <summary>
        /// See ILayoutElement.flexibleWidth.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// See ILayoutElement.flexibleHeight.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// See ILayoutElement.layoutPriority.
        /// </summary>
        public virtual int layoutPriority { get { return 0; } }

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputHorizontal.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputVertical.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() { }
        #endregion

        /// <summary>
        /// In case that CanvasRenderer doesn't exist. (2020.1 or later?)
        /// </summary>
        protected override void Reset()
        {
            if (gameObject.GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            base.Reset();
        }

        /// <summary>
        /// In case that CanvasRenderer doesn't exist. (2020.1 or later?)
        /// </summary>
        protected override void Awake()
        {
            if (gameObject.GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            base.Awake();
        }

        /// <summary>
        /// Show circle with specified visible rate.
        /// </summary>
        /// <param name="rate">rate of visible circle. Range from 0.0f to 1.0f.</param>
        public void Show(float rate)
        {
            this.visibleRate = Mathf.Clamp(rate, 0.0f, 1.0f);
            SetAllDirty();
        }

        /// <summary>
        /// Update the UI renderer mesh.
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // Get current vertices.
            vh.Clear();

            CalcAngleOffset();

            if (holeRate > 0)
            {
                // Create Vertices if not exists.
                CreateVerticesWithHole();

                // Activate valid indices.
                CreateIndiciesWithHole();
            }
            else
            {
                // Create Vertices if not exists.
                CreateVertices();

                // Activate valid indices.
                CreateIndicies();
            }

            // Set modified vertices and indicies.
            vh.AddUIVertexStream(vertices, indices);
        }

        /// <summary>Calculate start angle based on Origin setting.</summary>
        private void CalcAngleOffset()
        {
            switch (origin)
            {
                case Image.Origin360.Top:
                    angleOffset = 0.0f * Mathf.PI / 2;
                    break;

                case Image.Origin360.Right:
                    angleOffset = 1.0f * Mathf.PI / 2;
                    break;

                case Image.Origin360.Bottom:
                    angleOffset = 2.0f * Mathf.PI / 2;
                    break;

                case Image.Origin360.Left:
                    angleOffset = 3.0f * Mathf.PI / 2;
                    break;
            }
        }

        /// <summary>Create vertices of the circle.</summary>
        private void CreateVertices()
        {
            vertices.Clear();
            vertices.Capacity = maxVerticesCount;

            if (visibleOuterEdgesCount == 0)
            {
                return;
            }

            if (rectTransformCache == null)
            {
                rectTransformCache = GetComponent<RectTransform>();
            }

            float rx = rectTransformCache.sizeDelta.x;
            float ry = rectTransformCache.sizeDelta.y;

            // Create vertex at center.
            var center = new UIVertex
            {
                position = Vector3.zero,
                uv0 = new Vector2(spriteCenterX, spriteCenterY),
                color = this.color
            };
            vertices.Add(center);

            float angleOffset = 0.0f;
            switch (origin)
            {
                case Image.Origin360.Top:
                    angleOffset = 0.0f * Mathf.PI / 2;
                    break;

                case Image.Origin360.Right:
                    angleOffset = 1.0f * Mathf.PI / 2;
                    break;

                case Image.Origin360.Bottom:
                    angleOffset = 2.0f * Mathf.PI / 2;
                    break;

                case Image.Origin360.Left:
                    angleOffset = 3.0f * Mathf.PI / 2;
                    break;

            }

            // Create vertices on edges.
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

                var v = new UIVertex
                {
                    position = new Vector3(rx * Mathf.Sin(angle), ry * Mathf.Cos(angle)),
                    uv0 = new Vector2(spriteCenterX + spriteUVWidth * 0.5f * Mathf.Sin(angle), spriteCenterY + spriteUVHeight * 0.5f * Mathf.Cos(angle)),
                    color = this.color
                };

                vertices.Add(v);
            }
        }

        /// <summary>Create vertices of the holed circle.</summary>
        private void CreateVerticesWithHole()
        {
            vertices.Clear();
            vertices.Capacity = maxVerticesCount;

            if (visibleOuterEdgesCount == 0)
            {
                return;
            }

            if (rectTransformCache == null)
            {
                rectTransformCache = GetComponent<RectTransform>();
            }

            float rx = rectTransformCache.sizeDelta.x;
            float ry = rectTransformCache.sizeDelta.y;

            // Create vertices on edges.
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

                // Inner vertex
                {
                    var v = new UIVertex
                    {
                        position = new Vector3(holeRate * rx * Mathf.Sin(angle), holeRate * ry * Mathf.Cos(angle)),
                        uv0 = new Vector2(spriteCenterX + holeRate * spriteUVWidth * 0.5f * Mathf.Sin(angle), spriteCenterY + holeRate * spriteUVHeight * 0.5f * Mathf.Cos(angle)),
                        color = this.color
                    };

                    vertices.Add(v);
                }

                // Outer vertex
                {
                    var v = new UIVertex
                    {
                        position = new Vector3(rx * Mathf.Sin(angle), ry * Mathf.Cos(angle)),
                        uv0 = new Vector2(spriteCenterX + spriteUVWidth * 0.5f * Mathf.Sin(angle), spriteCenterY + spriteUVHeight * 0.5f * Mathf.Cos(angle)),
                        color = this.color
                    };

                    vertices.Add(v);
                }
            }
        }

        /// <summary>Create indicies of the circle.</summary>
        private void CreateIndicies()
        {
            indices.Clear();
            indices.Capacity = maxIndiciesCount;

            if (clockWise)
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    indices.Add(0);
                    indices.Add(i + 1);
                    indices.Add(i + 2);
                }
            }
            else
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    indices.Add(0);
                    indices.Add(i + 2);
                    indices.Add(i + 1);
                }
            }
        }

        /// <summary>Create indicies of the holed circle.</summary>
        private void CreateIndiciesWithHole()
        {
            indices.Clear();
            indices.Capacity = maxIndiciesCount;

            if (clockWise)
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    indices.Add(i * 2 + 0);
                    indices.Add(i * 2 + 1);
                    indices.Add(i * 2 + 3);

                    indices.Add(i * 2 + 0);
                    indices.Add(i * 2 + 3);
                    indices.Add(i * 2 + 2);
                }
            }
            else
            {
                for (int i = 0; i < visibleOuterEdgesCount; i++)
                {
                    indices.Add(i * 2 + 0);
                    indices.Add(i * 2 + 3);
                    indices.Add(i * 2 + 1);

                    indices.Add(i * 2 + 0);
                    indices.Add(i * 2 + 2);
                    indices.Add(i * 2 + 3);
                }
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Show Inspector view for CircleGraphic component.
    /// </summary>
    [CustomEditor(typeof(CircleGraphic))]
    public class CircleGraphicEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_sprite"), new GUIContent("Source Image"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"), new GUIContent("Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"), new GUIContent("Material"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"), new GUIContent("RaycastTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outerEdgesCount"), new GUIContent("OuterEdgesCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visibleRate"), new GUIContent("VisibleRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("holeRate"), new GUIContent("HoleRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("origin"), new GUIContent("Origin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clockWise"), new GUIContent("ClockWise"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
