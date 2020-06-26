using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(DoodaRuleTile), true)]
    [CanEditMultipleObjects]
    public class DoodaRuleTileEditor : Editor
    {
        private const string s_XIconString = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABoSURBVDhPnY3BDcAgDAOZhS14dP1O0x2C/LBEgiNSHvfwyZabmV0jZRUpq2zi6f0DJwdcQOEdwwDLypF0zHLMa9+NQRxkQ+ACOT2STVw/q8eY1346ZlE54sYAhVhSDrjwFymrSFnD2gTZpls2OvFUHAAAAABJRU5ErkJggg==";
        private const string s_Arrow0 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPzZExDoQwDATzE4oU4QXXcgUFj+YxtETwgpMwXuFcwMFSRMVKKwzZcWzhiMg91jtg34XIntkre5EaT7yjjhI9pOD5Mw5k2X/DdUwFr3cQ7Pu23E/BiwXyWSOxrNqx+ewnsayam5OLBtbOGPUM/r93YZL4/dhpR/amwByGFBz170gNChA6w5bQQMqramBTgJ+Z3A58WuWejPCaHQAAAABJRU5ErkJggg==";
        private const string s_Arrow1 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPxYzBDYAgEATpxYcd+PVr0fZ2siZrjmMhFz6STIiDs8XMlpEyi5RkO/d66TcgJUB43JfNBqRkSEYDnYjhbKD5GIUkDqRDwoH3+NgTAw+bL/aoOP4DOgH+iwECEt+IlFmkzGHlAYKAWF9R8zUnAAAAAElFTkSuQmCC";
        private const string s_Arrow2 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAC0SURBVDhPjVE5EsIwDMxPKFKYF9CagoJH8xhaMskLmEGsjOSRkBzYmU2s9a58TUQUmCH1BWEHweuKP+D8tphrWcAHuIGrjPnPNY8X2+DzEWE+FzrdrkNyg2YGNNfRGlyOaZDJOxBrDhgOowaYW8UW0Vau5ZkFmXbbDr+CzOHKmLinAXMEePyZ9dZkZR+s5QX2O8DY3zZ/sgYcdDqeEVp8516o0QQV1qeMwg6C91toYoLoo+kNt/tpKQEVvFQAAAAASUVORK5CYII=";
        private const string s_Arrow3 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAB2SURBVDhPzY1LCoAwEEPnLi48gW5d6p31bH5SMhp0Cq0g+CCLxrzRPqMZ2pRqKG4IqzJc7JepTlbRZXYpWTg4RZE1XAso8VHFKNhQuTjKtZvHUNCEMogO4K3BhvMn9wP4EzoPZ3n0AGTW5fiBVzLAAYTP32C2Ay3agtu9V/9PAAAAAElFTkSuQmCC";
        private const string s_Arrow5 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPnY3BCYBADASvFx924NevRdvbyoLBmNuDJQMDGjNxAFhK1DyUQ9fvobCdO+j7+sOKj/uSB+xYHZAxl7IR1wNTXJeVcaAVU+614uWfCT9mVUhknMlxDokd15BYsQrJFHeUQ0+MB5ErsPi/6hO1AAAAAElFTkSuQmCC";
        private const string s_Arrow6 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACaSURBVDhPxZExEkAwEEVzE4UiTqClUDi0w2hlOIEZsV82xCZmQuPPfFn8t1mirLWf7S5flQOXjd64vCuEKWTKVt+6AayH3tIa7yLg6Qh2FcKFB72jBgJeziA1CMHzeaNHjkfwnAK86f3KUafU2ClHIJSzs/8HHLv09M3SaMCxS7ljw/IYJWzQABOQZ66x4h614ahTCL/WT7BSO51b5Z5hSx88AAAAAElFTkSuQmCC";
        private const string s_Arrow7 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABQSURBVDhPYxh8QNle/T8U/4MKEQdAmsz2eICx6W530gygr2aQBmSMphkZYxqErAEXxusKfAYQ7XyyNMIAsgEkaYQBkAFkaYQBsjXSGDAwAAD193z4luKPrAAAAABJRU5ErkJggg==";
        private const string s_Arrow8 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPxZE9DoAwCIW9iUOHegJXHRw8tIdx1egJTMSHAeMPaHSR5KVQ+KCkCRF91mdz4VDEWVzXTBgg5U1N5wahjHzXS3iFFVRxAygNVaZxJ6VHGIl2D6oUXP0ijlJuTp724FnID1Lq7uw2QM5+thoKth0N+GGyA7IA3+yM77Ag1e2zkey5gCdAg/h8csy+/89v7E+YkgUntOWeVt2SfAAAAABJRU5ErkJggg==";
        private const string s_MirrorX = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG1JREFUOE+lj9ENwCAIRB2IFdyRfRiuDSaXAF4MrR9P5eRhHGb2Gxp2oaEjIovTXSrAnPNx6hlgyCZ7o6omOdYOldGIZhAziEmOTSfigLV0RYAB9y9f/7kO8L3WUaQyhCgz0dmCL9CwCw172HgBeyG6oloC8fAAAAAASUVORK5CYII=";
        private const string s_MirrorY = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG9JREFUOE+djckNACEMAykoLdAjHbPyw1IOJ0L7mAejjFlm9hspyd77Kk+kBAjPOXcakJIh6QaKyOE0EB5dSPJAiUmOiL8PMVGxugsP/0OOib8vsY8yYwy6gRyC8CB5QIWgCMKBLgRSkikEUr5h6wOPWfMoCYILdgAAAABJRU5ErkJggg==";
        private const string s_MirrorXY = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABl0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC4yMfEgaZUAAAHkSURBVDhPrVJLSwJRFJ4cdXwjPlrVJly1kB62cpEguElXKgYKIpaC+EIEEfGxLqI/UES1KaJlEdGmRY9ltCsIWrUJatGm0eZO3xkHIsJdH3zce+ec75z5zr3cf2MMmLdYLA/BYFA2mUyPOPvwnR+GR4PXaDQLLpfrKpVKSb1eT6bV6XTeocAS4sIw7S804BzEZ4IgsGq1ykhcr9dlj8czwPdbxJdBMyX/As/zLiz74Ar2J9lsVulcKpUYut5DnEbsHFwEx8AhtFqtGViD6BOc1ul0B5lMRhGXy2Wm1+ufkBOE/2fsL1FsQpXCiCAcQiAlk0kJRZjf7+9TRxI3Gg0WCoW+IpGISHHERBS5UKUch8n2K5WK3O125VqtpqydTkdZie12W261WjIVo73b7RZVKccZDIZ1q9XaT6fTLB6PD9BFKhQKjITFYpGFw+FBNBpVOgcCARH516pUGZYZXk5R4B3efLBxDM9f1CkWi/WR3ICtGVh6Rd4NPE+p0iEgmkSRLRoMEjYhHpA4kUiIOO8iZRU8AmnadK2/QOOfhnjPZrO95fN5Zdq5XE5yOBwvuKoNxGfBkQ8FzXkPprnj9Xrfm82mDI8fsLON3x5H/Od+RwHdLfDds9vtn0aj8QoF6QH9JzjuG3acpxmu1RgPAAAAAElFTkSuQmCC";
        private const string s_Rotated = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAHdJREFUOE+djssNwCAMQxmIFdgx+2S4Vj4YxWlQgcOT8nuG5u5C732Sd3lfLlmPMR4QhXgrTQaimUlA3EtD+CJlBuQ7aUAUMjEAv9gWCQNEPhHJUkYfZ1kEpcxDzioRzGIlr0Qwi0r+Q5rTgM+AAVcygHgt7+HtBZs/2QVWP8ahAAAAAElFTkSuQmCC";
        private const string s_Fixed = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMjHxIGmVAAAA50lEQVQ4T51Ruw6CQBCkwBYKWkIgQAs9gfgCvgb4BML/qWBM9Bdo9QPIuVOQ3JIzosVkc7Mzty9NCPE3lORaKMm1YA/LsnTXdbdhGJ6iKHoVRTEi+r4/OI6zN01Tl/XM7HneLsuyW13XU9u2ous6gYh3kiR327YPsp6ZgyDom6aZYFqiqqqJ8mdZz8xoca64BHjkZT0zY0aVcQbysp6Z4zj+Vvkp65mZttxjOSozdkEzD7KemekcxzRNHxDOHSDiQ/DIy3pmpjtuSJBThStGKMtyRKSOLnSm3DCMz3f+FUpyLZTkOgjtDSWORSDbpbmNAAAAAElFTkSuQmCC";

        private static Texture2D[] s_Arrows;
        public static Texture2D[] arrows
        {
            get
            {
                if (s_Arrows == null)
                {
                    s_Arrows = new Texture2D[10];
                    s_Arrows[0] = Base64ToTexture(s_Arrow0);
                    s_Arrows[1] = Base64ToTexture(s_Arrow1);
                    s_Arrows[2] = Base64ToTexture(s_Arrow2);
                    s_Arrows[3] = Base64ToTexture(s_Arrow3);
                    s_Arrows[5] = Base64ToTexture(s_Arrow5);
                    s_Arrows[6] = Base64ToTexture(s_Arrow6);
                    s_Arrows[7] = Base64ToTexture(s_Arrow7);
                    s_Arrows[8] = Base64ToTexture(s_Arrow8);
                    s_Arrows[9] = Base64ToTexture(s_XIconString);
                }
                return s_Arrows;
            }
        }

        private static Texture2D[] s_AutoTransforms;
        public static Texture2D[] autoTransforms
        {
            get
            {
                if (s_AutoTransforms == null)
                {
                    s_AutoTransforms = new Texture2D[5];
                    s_AutoTransforms[0] = Base64ToTexture(s_Rotated);
                    s_AutoTransforms[1] = Base64ToTexture(s_MirrorX);
                    s_AutoTransforms[2] = Base64ToTexture(s_MirrorY);
                    s_AutoTransforms[3] = Base64ToTexture(s_Fixed);
                    s_AutoTransforms[4] = Base64ToTexture(s_MirrorXY);
                }
                return s_AutoTransforms;
            }
        }

        public DoodaRuleTile tile => target as DoodaRuleTile;
        public ReorderableList m_ReorderableList;
        public bool extendNeighbor;

        public PreviewRenderUtility m_PreviewUtility;
        public Grid m_PreviewGrid;
        public List<Tilemap> m_PreviewTilemaps;
        public List<TilemapRenderer> m_PreviewTilemapRenderers;

        public const float k_DefaultElementHeight = 48f;
        public const float k_PaddingBetweenRules = 26f;
        public const float k_SingleLineHeight = 16f;
        public const float k_LabelWidth = 80f;

        public virtual void OnEnable()
        {
            m_ReorderableList = new ReorderableList(tile.m_TilingRules, typeof(DoodaRuleTile.DoodaTilingRule), true, true, true, true);
            m_ReorderableList.drawHeaderCallback = OnDrawHeader;
            m_ReorderableList.drawElementCallback = OnDrawElement;
            m_ReorderableList.elementHeightCallback = GetElementHeight;
            m_ReorderableList.onChangedCallback = ListUpdated;
            m_ReorderableList.onAddCallback = OnAddElement;
        }

        public virtual void OnDisable()
        {
            DestroyPreview();
        }

        public virtual BoundsInt GetRuleGUIBounds(BoundsInt bounds, DoodaRuleTile.DoodaTilingRule rule)
        {
            if (extendNeighbor)
            {
                bounds.xMin--;
                bounds.yMin--;
                bounds.xMax++;
                bounds.yMax++;
            }
            bounds.xMin = Mathf.Min(bounds.xMin, -1);
            bounds.yMin = Mathf.Min(bounds.yMin, -1);
            bounds.xMax = Mathf.Max(bounds.xMax, 2);
            bounds.yMax = Mathf.Max(bounds.yMax, 2);
            return bounds;
        }

        public void ListUpdated(ReorderableList list)
        {
            HashSet<int> usedIdSet = new HashSet<int>();
            foreach (var rule in tile.m_TilingRules)
            {
                while (usedIdSet.Contains(rule.m_Id))
                    rule.m_Id++;
                usedIdSet.Add(rule.m_Id);
            }
        }

        public float GetElementHeight(int index)
        {
            DoodaRuleTile.DoodaTilingRule rule = tile.m_TilingRules[index];
            return GetElementHeight(rule);
        }

        public float GetElementHeight(DoodaRuleTile.DoodaTilingRule rule)
        {
            BoundsInt bounds = GetRuleGUIBounds(rule.GetBounds(), rule);

            float inspectorHeight = GetElementHeight(rule as DoodaRuleTile.DoodaTilingRuleOutput);
            float matrixHeight = GetMatrixSize(bounds).y + 10f;

            return Mathf.Max(inspectorHeight, matrixHeight);
        }

        public float GetElementHeight(DoodaRuleTile.DoodaTilingRuleOutput rule)
        {
            float inspectorHeight = k_DefaultElementHeight + k_PaddingBetweenRules;

            switch (rule.m_OutputType)
            {
                case DoodaRuleTile.DoodaTilingRule.OutputSprite.Random:
                    inspectorHeight = k_DefaultElementHeight + k_SingleLineHeight * (rule.m_Sprites.Length + 3) + k_PaddingBetweenRules;
                    break;
                case DoodaRuleTile.DoodaTilingRule.OutputSprite.Animation:
                    inspectorHeight = k_DefaultElementHeight + k_SingleLineHeight * (rule.m_Sprites.Length + 2) + k_PaddingBetweenRules;
                    break;
            }

            return inspectorHeight;
        }

        public virtual Vector2 GetMatrixSize(BoundsInt bounds)
        {
            return new Vector2(bounds.size.x * k_SingleLineHeight, bounds.size.y * k_SingleLineHeight);
        }

        public virtual void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            DoodaRuleTile.DoodaTilingRule rule = tile.m_TilingRules[index];
            BoundsInt bounds = GetRuleGUIBounds(rule.GetBounds(), rule);

            float yPos = rect.yMin + 2f;
            float height = rect.height - k_PaddingBetweenRules;
            Vector2 matrixSize = GetMatrixSize(bounds);

            Rect spriteRect = new Rect(rect.xMax - k_DefaultElementHeight - 5f, yPos, k_DefaultElementHeight, k_DefaultElementHeight);
            Rect matrixRect = new Rect(rect.xMax - matrixSize.x - spriteRect.width - 10f, yPos, matrixSize.x, matrixSize.y);
            Rect inspectorRect = new Rect(rect.xMin, yPos, rect.width - matrixSize.x - spriteRect.width - 20f, height);

            RuleInspectorOnGUI(inspectorRect, rule);
            RuleMatrixOnGUI(tile, matrixRect, bounds, rule);
            SpriteOnGUI(spriteRect, rule);
        }

        public void OnAddElement(ReorderableList list)
        {
            DoodaRuleTile.DoodaTilingRule rule = new DoodaRuleTile.DoodaTilingRule();
            rule.m_OutputType = DoodaRuleTile.DoodaTilingRule.OutputSprite.Single;
            rule.m_Sprites[0] = tile.m_DefaultSprite;
            rule.m_GameObject = tile.m_DefaultGameObject;
            rule.m_ColliderType = tile.m_DefaultColliderType;
            tile.m_TilingRules.Add(rule);
        }

        public void SaveTile()
        {
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }

        public void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, "Tiling Rules");

            Rect toggleRect = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);
            Rect toggleLabelRect = new Rect(rect.x, rect.y, rect.width - toggleRect.width - 5f, rect.height);

            extendNeighbor = EditorGUI.Toggle(toggleRect, extendNeighbor);
            EditorGUI.LabelField(toggleLabelRect, "Extend Neighbor", new GUIStyle()
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                fontSize = 10,
            });
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            tile.m_DefaultSprite = EditorGUILayout.ObjectField("Default Sprite", tile.m_DefaultSprite, typeof(Sprite), false) as Sprite;
            tile.m_DefaultGameObject = EditorGUILayout.ObjectField("Default Game Object", tile.m_DefaultGameObject, typeof(GameObject), false) as GameObject;
            tile.m_DefaultColliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Default Collider", tile.m_DefaultColliderType);

            DrawCustomFields(false);

            EditorGUILayout.Space();

            if (m_ReorderableList != null)
                m_ReorderableList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
                SaveTile();
        }

        public void DrawCustomFields(bool isOverrideInstance)
        {
            var customFields = tile.GetCustomFields(isOverrideInstance);

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            foreach (var field in customFields)
            {
                var property = serializedObject.FindProperty(field.Name);
                if (property != null)
                    EditorGUILayout.PropertyField(property, true);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        public virtual int GetArrowIndex(Vector3Int position)
        {
            if (Mathf.Abs(position.x) == Mathf.Abs(position.y))
            {
                if (position.x < 0 && position.y > 0)
                    return 0;
                else if (position.x > 0 && position.y > 0)
                    return 2;
                else if (position.x < 0 && position.y < 0)
                    return 6;
                else if (position.x > 0 && position.y < 0)
                    return 8;
            }
            else if (Mathf.Abs(position.x) > Mathf.Abs(position.y))
            {
                if (position.x > 0)
                    return 5;
                else
                    return 3;
            }
            else
            {
                if (position.y > 0)
                    return 1;
                else
                    return 7;
            }
            return -1;
        }

        public virtual void RuleOnGUI(Rect rect, Vector3Int position, int neighbor)
        {
            switch (neighbor)
            {
                case DoodaRuleTile.DoodaTilingRule.Neighbor.This:
                    GUI.DrawTexture(rect, arrows[GetArrowIndex(position)]);
                    break;
                case DoodaRuleTile.DoodaTilingRule.Neighbor.NotThis:
                    GUI.DrawTexture(rect, arrows[9]);
                    break;
                default:
                    var style = new GUIStyle();
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 10;
                    GUI.Label(rect, neighbor.ToString(), style);
                    break;
            }
        }

        public void RuleTooltipOnGUI(Rect rect, int neighbor)
        {
            var allConsts = tile.m_NeighborType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            foreach (var c in allConsts)
            {
                if ((int)c.GetValue(null) == neighbor)
                {
                    GUI.Label(rect, new GUIContent("", c.Name));
                    break;
                }
            }
        }

        public virtual void RuleTransformOnGUI(Rect rect, DoodaRuleTile.DoodaTilingRule.Transform ruleTransform)
        {
            switch (ruleTransform)
            {
                case DoodaRuleTile.DoodaTilingRule.Transform.Rotated:
                    GUI.DrawTexture(rect, autoTransforms[0]);
                    break;
                case DoodaRuleTile.DoodaTilingRule.Transform.MirrorX:
                    GUI.DrawTexture(rect, autoTransforms[1]);
                    break;
                case DoodaRuleTile.DoodaTilingRule.Transform.MirrorY:
                    GUI.DrawTexture(rect, autoTransforms[2]);
                    break;
                case DoodaRuleTile.DoodaTilingRule.Transform.Fixed:
                    GUI.DrawTexture(rect, autoTransforms[3]);
                    break;
                case DoodaRuleTile.DoodaTilingRule.Transform.MirrorXY:
                    GUI.DrawTexture(rect, autoTransforms[4]);
                    break;
            }
        }

        public void RuleNeighborUpdate(Rect rect, DoodaRuleTile.DoodaTilingRule DoodaTilingRule, Dictionary<Vector3Int, int> neighbors, Vector3Int position)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                var allConsts = tile.m_NeighborType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                var neighborConsts = allConsts.Select(c => (int)c.GetValue(null)).ToList();
                neighborConsts.Sort();

                if (neighbors.ContainsKey(position))
                {
                    int oldIndex = neighborConsts.IndexOf(neighbors[position]);
                    int newIndex = oldIndex + GetMouseChange();
                    if (newIndex >= 0 && newIndex < neighborConsts.Count)
                    {
                        newIndex = (int)Mathf.Repeat(newIndex, neighborConsts.Count);
                        neighbors[position] = neighborConsts[newIndex];
                    }
                    else
                    {
                        neighbors.Remove(position);
                    }
                }
                else
                {
                    neighbors.Add(position, neighborConsts[GetMouseChange() == 1 ? 0 : (neighborConsts.Count - 1)]);
                }
                DoodaTilingRule.ApplyNeighbors(neighbors);

                GUI.changed = true;
                Event.current.Use();
            }
        }

        public void RuleTransformUpdate(Rect rect, DoodaRuleTile.DoodaTilingRule DoodaTilingRule)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                DoodaTilingRule.m_RuleTransform = (DoodaRuleTile.DoodaTilingRule.Transform)(int)Mathf.Repeat((int)DoodaTilingRule.m_RuleTransform + GetMouseChange(), Enum.GetValues(typeof(DoodaRuleTile.DoodaTilingRule.Transform)).Length);
                GUI.changed = true;
                Event.current.Use();
            }
        }

        public virtual bool ContainsMousePosition(Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        public static int GetMouseChange()
        {
            return Event.current.button == 1 ? -1 : 1;
        }

        public virtual void RuleMatrixOnGUI(DoodaRuleTile tile, Rect rect, BoundsInt bounds, DoodaRuleTile.DoodaTilingRule DoodaTilingRule)
        {
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            float w = rect.width / bounds.size.x;
            float h = rect.height / bounds.size.y;

            for (int y = 0; y <= bounds.size.y; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }
            for (int x = 0; x <= bounds.size.x; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }
            Handles.color = Color.white;

            var neighbors = DoodaTilingRule.GetNeighbors();

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    Rect r = new Rect(rect.xMin + (x - bounds.xMin) * w, rect.yMin + (-y + bounds.yMax - 1) * h, w - 1, h - 1);
                    RuleMatrixIconOnGUI(DoodaTilingRule, neighbors, pos, r);
                }
            }
        }

        public void RuleMatrixIconOnGUI(DoodaRuleTile.DoodaTilingRule DoodaTilingRule, Dictionary<Vector3Int, int> neighbors, Vector3Int pos, Rect rect)
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (pos.x != 0 || pos.y != 0)
                {
                    if (neighbors.ContainsKey(pos))
                    {
                        RuleOnGUI(rect, pos, neighbors[pos]);
                        RuleTooltipOnGUI(rect, neighbors[pos]);
                    }
                    RuleNeighborUpdate(rect, DoodaTilingRule, neighbors, pos);
                }
                else
                {
                    RuleTransformOnGUI(rect, DoodaTilingRule.m_RuleTransform);
                    RuleTransformUpdate(rect, DoodaTilingRule);
                }
                if (check.changed)
                {
                    tile.UpdateNeighborPositions();
                }
            }
        }

        public virtual void SpriteOnGUI(Rect rect, DoodaRuleTile.DoodaTilingRuleOutput DoodaTilingRule)
        {
            DoodaTilingRule.m_Sprites[0] = EditorGUI.ObjectField(rect, DoodaTilingRule.m_Sprites[0], typeof(Sprite), false) as Sprite;
        }

        public void RuleInspectorOnGUI(Rect rect, DoodaRuleTile.DoodaTilingRuleOutput DoodaTilingRule)
        {
            float y = rect.yMin;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Game Object");
            DoodaTilingRule.m_GameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", DoodaTilingRule.m_GameObject, typeof(GameObject), false);
            y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Collider");
            DoodaTilingRule.m_ColliderType = (Tile.ColliderType)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_ColliderType);
            y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Output");
            DoodaTilingRule.m_OutputType = (DoodaRuleTile.DoodaTilingRule.OutputSprite)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_OutputType);
            y += k_SingleLineHeight;

            if (DoodaTilingRule.m_OutputType == DoodaRuleTile.DoodaTilingRule.OutputSprite.Animation)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Speed");
                DoodaTilingRule.m_AnimationSpeed = EditorGUI.FloatField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_AnimationSpeed);
                y += k_SingleLineHeight;
            }
            if (DoodaTilingRule.m_OutputType == DoodaRuleTile.DoodaTilingRule.OutputSprite.Random)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Noise");
                DoodaTilingRule.m_PerlinScale = EditorGUI.Slider(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_PerlinScale, 0.001f, 0.999f);
                y += k_SingleLineHeight;

                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Shuffle");
                DoodaTilingRule.m_RandomTransform = (DoodaRuleTile.DoodaTilingRule.Transform)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_RandomTransform);
                y += k_SingleLineHeight;
            }

            if (DoodaTilingRule.m_OutputType != DoodaRuleTile.DoodaTilingRule.OutputSprite.Single)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Size");
                EditorGUI.BeginChangeCheck();
                int newLength = EditorGUI.DelayedIntField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_Sprites.Length);
                if (EditorGUI.EndChangeCheck())
                    Array.Resize(ref DoodaTilingRule.m_Sprites, Math.Max(newLength, 1));
                y += k_SingleLineHeight;

                for (int i = 0; i < DoodaTilingRule.m_Sprites.Length; i++)
                {
                    DoodaTilingRule.m_Sprites[i] = EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), DoodaTilingRule.m_Sprites[i], typeof(Sprite), false) as Sprite;
                    y += k_SingleLineHeight;
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_PreviewUtility == null)
                CreatePreview();

            if (Event.current.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r, background);
            m_PreviewUtility.camera.orthographicSize = 2;
            if (r.height > r.width)
                m_PreviewUtility.camera.orthographicSize *= (float)r.height / r.width;
            m_PreviewUtility.camera.Render();
            m_PreviewUtility.EndAndDrawPreview(r);
        }

        public virtual void CreatePreview()
        {
            m_PreviewUtility = new PreviewRenderUtility(true);
            m_PreviewUtility.camera.orthographic = true;
            m_PreviewUtility.camera.orthographicSize = 2;
            m_PreviewUtility.camera.transform.position = new Vector3(0, 0, -10);

            var previewInstance = new GameObject();
            m_PreviewGrid = previewInstance.AddComponent<Grid>();
            m_PreviewUtility.AddSingleGO(previewInstance);

            m_PreviewTilemaps = new List<Tilemap>();
            m_PreviewTilemapRenderers = new List<TilemapRenderer>();

            for (int i = 0; i < 4; i++)
            {
                var previewTilemapGo = new GameObject();
                m_PreviewTilemaps.Add(previewTilemapGo.AddComponent<Tilemap>());
                m_PreviewTilemapRenderers.Add(previewTilemapGo.AddComponent<TilemapRenderer>());

                previewTilemapGo.transform.SetParent(previewInstance.transform, false);
            }

            for (int x = -2; x <= 0; x++)
                for (int y = -1; y <= 1; y++)
                    m_PreviewTilemaps[0].SetTile(new Vector3Int(x, y, 0), tile);

            for (int y = -1; y <= 1; y++)
                m_PreviewTilemaps[1].SetTile(new Vector3Int(1, y, 0), tile);

            for (int x = -2; x <= 0; x++)
                m_PreviewTilemaps[2].SetTile(new Vector3Int(x, -2, 0), tile);

            m_PreviewTilemaps[3].SetTile(new Vector3Int(1, -2, 0), tile);
        }

        public void DestroyPreview()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
                m_PreviewGrid = null;
                m_PreviewTilemaps = null;
                m_PreviewTilemapRenderers = null;
            }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (tile.m_DefaultSprite != null)
            {
                Type t = GetType("UnityEditor.SpriteUtility");
                if (t != null)
                {
                    MethodInfo method = t.GetMethod("RenderStaticPreview", new Type[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });
                    if (method != null)
                    {
                        object ret = method.Invoke("RenderStaticPreview", new object[] { tile.m_DefaultSprite, Color.white, width, height });
                        if (ret is Texture2D)
                            return ret as Texture2D;
                    }
                }
            }
            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        public static Type GetType(string TypeName)
        {
            var type = Type.GetType(TypeName);
            if (type != null)
                return type;

            if (TypeName.Contains("."))
            {
                var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));
                var assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                    return null;
                type = assembly.GetType(TypeName);
                if (type != null)
                    return type;
            }

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }
            }
            return null;
        }

        public static Texture2D Base64ToTexture(string base64)
        {
            Texture2D t = new Texture2D(1, 1);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.LoadImage(System.Convert.FromBase64String(base64));
            return t;
        }

        [Serializable]
        class DoodaRuleTileRuleWrapper
        {
            [SerializeField]
            public List<DoodaRuleTile.DoodaTilingRule> rules = new List<DoodaRuleTile.DoodaTilingRule>();
        }

        [MenuItem("CONTEXT/DoodaRuleTile/Copy All Rules")]
        public static void CopyAllRules(MenuCommand item)
        {
            DoodaRuleTile tile = item.context as DoodaRuleTile;
            if (tile == null)
                return;

            DoodaRuleTileRuleWrapper rulesWrapper = new DoodaRuleTileRuleWrapper();
            rulesWrapper.rules = tile.m_TilingRules;
            var rulesJson = EditorJsonUtility.ToJson(rulesWrapper);
            EditorGUIUtility.systemCopyBuffer = rulesJson;
        }

        [MenuItem("CONTEXT/DoodaRuleTile/Paste Rules")]
        public static void PasteRules(MenuCommand item)
        {
            DoodaRuleTile tile = item.context as DoodaRuleTile;
            if (tile == null)
                return;

            try
            {
                DoodaRuleTileRuleWrapper rulesWrapper = new DoodaRuleTileRuleWrapper();
                EditorJsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, rulesWrapper);
                tile.m_TilingRules.AddRange(rulesWrapper.rules);
            }
            catch (Exception)
            {
                Debug.LogError("Unable to paste rules from system copy buffer");
            }
        }
    }
}

