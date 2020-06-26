using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace UnityEngine
{
    public class DoodaRuleTile<T> : DoodaRuleTile
    { 
        public sealed override Type m_NeighborType => typeof(T);
    }

    [Serializable]
    [CreateAssetMenu(fileName = "DoodaRuleTile", menuName = "DoodaAssets", order = 1)]

    public class DoodaRuleTile : TileBase
    {
        //Defualt Neighbouring Rule Class Type
        public virtual Type m_NeighborType => typeof(DoodaTilingRule.Neighbor);
        public Sprite m_DefaultSprite;
        public GameObject m_DefaultGameObject;
        public Tile.ColliderType m_DefaultColliderType = Tile.ColliderType.Sprite;

        public virtual int m_RotationAngle => 90;
        public int m_RotationCount => 360 / m_RotationAngle;

        //CLASSES//
        
        //The Data Structure for tile information
        [Serializable]
        public class DoodaTilingRuleOutput
        {
            //DataClass
            public int m_Id;
            public Sprite[] m_Sprites = new Sprite[1];
            public GameObject m_GameObject;
            public float m_AnimationSpeed = 1f;
            public float m_PerlinScale = 0.5f; 
            public OutputSprite m_OutputType = OutputSprite.Single;
            public Tile.ColliderType m_ColliderType = Tile.ColliderType.Sprite;
            public Transform m_RandomTransform;

            public class Neighbor
            {
                public const int This = 1;
                public const int NotThis = 2;
            }

            //How the Tile can be transformed
            public enum Transform
            {
                Fixed,
                Rotated,
                MirrorX,
                MirrorY,
                MirrorXY
            }

            //What Kind of sprite it is
            public enum OutputSprite
            {
                Single,
                Random,
                Animation
            }
        }

        [Serializable]
        public class DoodaTilingRule : DoodaTilingRuleOutput
        {
            public List<int> m_Neighbors = new List<int>();

            //Generates the positions of the tiles around it
            public List<Vector3Int> m_NeighborPositions = new List<Vector3Int>()
        {
            new Vector3Int(-1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, -1, 0),
        };

            public Transform m_RuleTransform;

            //Creates a dictionary of all the neighbours
            public Dictionary<Vector3Int, int> GetNeighbors()
            {
                Dictionary<Vector3Int, int> dict = new Dictionary<Vector3Int, int>();

                for (int i = 0; i < m_Neighbors.Count && i < m_NeighborPositions.Count; i++)
                    dict.Add(m_NeighborPositions[i], m_Neighbors[i]);

                return dict;
            }

            //Updates neigbours
            public void ApplyNeighbors(Dictionary<Vector3Int, int> dict)
            {
                m_NeighborPositions = dict.Keys.ToList();
                m_Neighbors = dict.Values.ToList();
            }

            //Get the bounds of the object
            public BoundsInt GetBounds()
            {
                BoundsInt bounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);
                foreach (var neighbor in GetNeighbors())
                {
                    bounds.xMin = Mathf.Min(bounds.xMin, neighbor.Key.x);
                    bounds.yMin = Mathf.Min(bounds.yMin, neighbor.Key.y);
                    bounds.xMax = Mathf.Max(bounds.xMax, neighbor.Key.x + 1);
                    bounds.yMax = Mathf.Max(bounds.yMax, neighbor.Key.y + 1);
                }
                return bounds;
            }
        }

        public class DontOverride : Attribute { }

        //Layout
        [HideInInspector] public List<DoodaTilingRule> m_TilingRules = new List<DoodaRuleTile.DoodaTilingRule>();

        public HashSet<Vector3Int> neighborPositions
        {
            get
            {
                if (m_NeighborPositions.Count == 0)
                    UpdateNeighborPositions();

                return m_NeighborPositions;
            }
        }

        public void UpdateNeighborPositions()
        {
            m_CacheTilemapsNeighborPositions.Clear();

            HashSet<Vector3Int> positions = m_NeighborPositions;
            positions.Clear();

            foreach (DoodaTilingRule rule in m_TilingRules)
            {
                foreach (var neighbor in rule.GetNeighbors())
                {
                    Vector3Int position = neighbor.Key;
                    positions.Add(position);

                    // Check rule against rotations of 0, 90, 180, 270
                    if (rule.m_RuleTransform == DoodaTilingRule.Transform.Rotated)
                    {
                        for (int angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
                        {
                            positions.Add(GetRotatedPosition(position, angle));
                        }
                    }
                    // Check rule against x-axis, y-axis mirror
                    else if (rule.m_RuleTransform == DoodaTilingRule.Transform.MirrorXY)
                    {
                        positions.Add(GetMirroredPosition(position, true, true));
                        positions.Add(GetMirroredPosition(position, true, false));
                        positions.Add(GetMirroredPosition(position, false, true));
                    }
                    // Check rule against x-axis mirror
                    else if (rule.m_RuleTransform == DoodaTilingRule.Transform.MirrorX)
                    {
                        positions.Add(GetMirroredPosition(position, true, false));
                    }
                    // Check rule against y-axis mirror
                    else if (rule.m_RuleTransform == DoodaTilingRule.Transform.MirrorY)
                    {
                        positions.Add(GetMirroredPosition(position, false, true));
                    }
                }
            }
        }

        private HashSet<Vector3Int> m_NeighborPositions = new HashSet<Vector3Int>();

        //Called On the First Frame Running
        /// <param name="location">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="instantiatedGameObject">The GameObject instantiated for the Tile.</param>
        /// <returns>Whether StartUp was successful</returns>
        public override bool StartUp(Vector3Int location, ITilemap tilemap, GameObject instantiatedGameObject)
        {
            if (instantiatedGameObject != null)
            {
                Tilemap tmpMap = tilemap.GetComponent<Tilemap>();
                Matrix4x4 orientMatrix = tmpMap.orientationMatrix;

                var iden = Matrix4x4.identity;
                Vector3 gameObjectTranslation = new Vector3();
                Quaternion gameObjectRotation = new Quaternion();
                Vector3 gameObjectScale = new Vector3();

                bool ruleMatched = false;
                foreach (DoodaTilingRule rule in m_TilingRules)
                {
                    Matrix4x4 transform = iden;
                    if (RuleMatches(rule, location, tilemap, ref transform))
                    {
                        transform = orientMatrix * transform;

                        // Converts the tile's translation, rotation, & scale matrix to values to be used by the instantiated Game Object
                        gameObjectTranslation = new Vector3(transform.m03, transform.m13, transform.m23);
                        gameObjectRotation = Quaternion.LookRotation(new Vector3(transform.m02, transform.m12, transform.m22), new Vector3(transform.m01, transform.m11, transform.m21));
                        gameObjectScale = transform.lossyScale;

                        ruleMatched = true;
                        break;
                    }
                }
                if (!ruleMatched)
                {
                    // Fallback to just using the orientMatrix for the translation, rotation, & scale values.
                    gameObjectTranslation = new Vector3(orientMatrix.m03, orientMatrix.m13, orientMatrix.m23);
                    gameObjectRotation = Quaternion.LookRotation(new Vector3(orientMatrix.m02, orientMatrix.m12, orientMatrix.m22), new Vector3(orientMatrix.m01, orientMatrix.m11, orientMatrix.m21));
                    gameObjectScale = orientMatrix.lossyScale;
                }

                instantiatedGameObject.transform.localPosition = gameObjectTranslation + tmpMap.CellToLocalInterpolated(location + tmpMap.tileAnchor);
                instantiatedGameObject.transform.localRotation = gameObjectRotation;
                instantiatedGameObject.transform.localScale = gameObjectScale;
            }

            return true;
        }

        //Renderering Information
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="tileData">Data to render the tile.</param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            var iden = Matrix4x4.identity;

            tileData.sprite = m_DefaultSprite;
            tileData.gameObject = m_DefaultGameObject;
            tileData.colliderType = m_DefaultColliderType;
            tileData.flags = TileFlags.LockTransform;
            tileData.transform = iden;

            foreach (DoodaTilingRule rule in m_TilingRules)
            {
                Matrix4x4 transform = iden;
                if (RuleMatches(rule, position, tilemap, ref transform))
                {
                    switch (rule.m_OutputType)
                    {
                        case DoodaTilingRule.OutputSprite.Single:
                        case DoodaTilingRule.OutputSprite.Animation:
                            tileData.sprite = rule.m_Sprites[0];
                            break;
                        case DoodaTilingRule.OutputSprite.Random:
                            int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, rule.m_PerlinScale, 100000f) * rule.m_Sprites.Length), 0, rule.m_Sprites.Length - 1);
                            tileData.sprite = rule.m_Sprites[index];
                            if (rule.m_RandomTransform != DoodaTilingRule.Transform.Fixed)
                                transform = ApplyRandomTransform(rule.m_RandomTransform, transform, rule.m_PerlinScale, position);
                            break;
                    }
                    tileData.transform = transform;
                    tileData.gameObject = rule.m_GameObject;
                    tileData.colliderType = rule.m_ColliderType;
                    break;
                }
            }
        }

        //Creates Perlin Noise
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="scale">The Perlin Scale factor of the Tile.</param>
        /// <param name="offset">Offset of the Tile on the Tilemap.</param>
        public static float GetPerlinValue(Vector3Int position, float scale, float offset)
        {
            return Mathf.PerlinNoise((position.x + offset) * scale, (position.y + offset) * scale);
        }

        //Static cache based functions and variables
        static Dictionary<Tilemap, KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>>> m_CacheTilemapsNeighborPositions
            = new Dictionary<Tilemap, KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>>>();
        static TileBase[] m_AllocatedUsedTileArr = new TileBase[0];

        static bool IsTilemapUsedTilesChange(Tilemap tilemap)
        {
            if (!m_CacheTilemapsNeighborPositions.ContainsKey(tilemap))
                return true;

            var oldUsedTiles = m_CacheTilemapsNeighborPositions[tilemap].Key;
            int newUsedTilesCount = tilemap.GetUsedTilesCount();

            if (newUsedTilesCount != oldUsedTiles.Count)
                return true;

            if (m_AllocatedUsedTileArr.Length < newUsedTilesCount)
                Array.Resize(ref m_AllocatedUsedTileArr, newUsedTilesCount);

            tilemap.GetUsedTilesNonAlloc(m_AllocatedUsedTileArr);

            for (int i = 0; i < newUsedTilesCount; i++)
            {
                TileBase newUsedTile = m_AllocatedUsedTileArr[i];
                if (!oldUsedTiles.Contains(newUsedTile))
                    return true;
            }

            return false;
        }
        static void CachingTilemapNeighborPositions(Tilemap tilemap)
        {
            int usedTileCount = tilemap.GetUsedTilesCount();
            HashSet<TileBase> usedTiles = new HashSet<TileBase>();
            HashSet<Vector3Int> neighborPositions = new HashSet<Vector3Int>();

            if (m_AllocatedUsedTileArr.Length < usedTileCount)
                Array.Resize(ref m_AllocatedUsedTileArr, usedTileCount);

            tilemap.GetUsedTilesNonAlloc(m_AllocatedUsedTileArr);

            for (int i = 0; i < usedTileCount; i++)
            {
                TileBase tile = m_AllocatedUsedTileArr[i];
                usedTiles.Add(tile);
                DoodaRuleTile DoodaRuleTile = null;

                if (tile is DoodaRuleTile)
                    DoodaRuleTile = tile as DoodaRuleTile;

                if (DoodaRuleTile)
                    foreach (Vector3Int neighborPosition in DoodaRuleTile.neighborPositions)
                        neighborPositions.Add(neighborPosition);
            }

            m_CacheTilemapsNeighborPositions[tilemap] = new KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>>(usedTiles, neighborPositions);
        }
        static void ReleaseDestroyedTilemapCacheData()
        {
            m_CacheTilemapsNeighborPositions = m_CacheTilemapsNeighborPositions
                .Where(data => data.Key != null)
                .ToDictionary(data => data.Key, data => data.Value);
        }

        //Retrieves tile animated data
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="tileAnimationData">Data to run an animation on the tile.</param>
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            var iden = Matrix4x4.identity;
            foreach (DoodaTilingRule rule in m_TilingRules)
            {
                if (rule.m_OutputType == DoodaTilingRule.OutputSprite.Animation)
                {
                    Matrix4x4 transform = iden;
                    if (RuleMatches(rule, position, tilemap, ref transform))
                    {
                        tileAnimationData.animatedSprites = rule.m_Sprites;
                        tileAnimationData.animationSpeed = rule.m_AnimationSpeed;
                        return true;
                    }
                }
            }
            return false;
        }

        //Called When Tile Is Refreshed
        /// <param name="location">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        public override void RefreshTile(Vector3Int location, ITilemap tilemap)
        {
            base.RefreshTile(location, tilemap);

            Tilemap tilemap_2 = tilemap.GetComponent<Tilemap>();

            ReleaseDestroyedTilemapCacheData(); // Prevent memory leak

            if (IsTilemapUsedTilesChange(tilemap_2))
                CachingTilemapNeighborPositions(tilemap_2);

            HashSet<Vector3Int> neighborPositions = m_CacheTilemapsNeighborPositions[tilemap_2].Value;
            foreach (Vector3Int offset in neighborPositions)
            {
                Vector3Int position = GetOffsetPositionReverse(location, offset);
                TileBase tile = tilemap_2.GetTile(position);
                DoodaRuleTile DoodaRuleTile = null;

                if (tile is DoodaRuleTile)
                    DoodaRuleTile = tile as DoodaRuleTile;

                if (DoodaRuleTile)
                    if (DoodaRuleTile.neighborPositions.Contains(offset))
                        base.RefreshTile(position, tilemap);
            }
        }

        //TEST RULE MATHCES//
        
        /// <param name="neighbor">Neighbor matching rule.</param>
        /// <param name="other">Tile to match.</param>
        public virtual bool RuleMatch(int neighbor, TileBase other)
        {
            switch (neighbor)
            {
                case DoodaTilingRule.Neighbor.This: return other == this;
                case DoodaTilingRule.Neighbor.NotThis: return other != this;
            }
            return true;
        }

        /// <param name="rule">The Tiling Rule to match with.</param>
        /// <param name="tilemap">The tilemap to match with.</param>
        /// <param name="transform">A transform matrix which will match the Rule.</param>
        public virtual bool RuleMatches(DoodaTilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
        {
            if (RuleMatches(rule, position, tilemap, 0))
            {
                transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 0f), Vector3.one);
                return true;
            }

            // Check rule against rotations of 0, 90, 180, 270
            if (rule.m_RuleTransform == DoodaTilingRule.Transform.Rotated)
            {
                for (int angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
                {
                    if (RuleMatches(rule, position, tilemap, angle))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                        return true;
                    }
                }
            }
            // Check rule against x-axis, y-axis mirror
            else if (rule.m_RuleTransform == DoodaTilingRule.Transform.MirrorXY)
            {
                if (RuleMatches(rule, position, tilemap, true, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, -1f, 1f));
                    return true;
                }
                if (RuleMatches(rule, position, tilemap, true, false))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
                    return true;
                }
                if (RuleMatches(rule, position, tilemap, false, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
                    return true;
                }
            }
            // Check rule against x-axis mirror
            else if (rule.m_RuleTransform == DoodaTilingRule.Transform.MirrorX)
            {
                if (RuleMatches(rule, position, tilemap, true, false))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
                    return true;
                }
            }
            // Check rule against y-axis mirror
            else if (rule.m_RuleTransform == DoodaTilingRule.Transform.MirrorY)
            {
                if (RuleMatches(rule, position, tilemap, false, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
                    return true;
                }
            }

            return false;
        }

        /// <param name="rule">Neighbor matching rule.</param>
        /// <param name="tilemap">Tilemap to match.</param>
        /// <param name="angle">Rotation angle for matching.</param>
        public bool RuleMatches(DoodaTilingRule rule, Vector3Int position, ITilemap tilemap, int angle)
        {
            for (int i = 0; i < rule.m_Neighbors.Count && i < rule.m_NeighborPositions.Count; i++)
            {
                int neighbor = rule.m_Neighbors[i];
                Vector3Int positionOffset = GetRotatedPosition(rule.m_NeighborPositions[i], angle);
                TileBase other = tilemap.GetTile(GetOffsetPosition(position, positionOffset));
                if (!RuleMatch(neighbor, other))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <param name="rule">Neighbor matching rule.</param>
        /// <param name="tilemap">Tilemap to match.</param>
        /// <param name="mirrorX">Mirror X Axis for matching.</param>
        /// <param name="mirrorY">Mirror Y Axis for matching.</param>
        public bool RuleMatches(DoodaTilingRule rule, Vector3Int position, ITilemap tilemap, bool mirrorX, bool mirrorY)
        {
            for (int i = 0; i < rule.m_Neighbors.Count && i < rule.m_NeighborPositions.Count; i++)
            {
                int neighbor = rule.m_Neighbors[i];
                Vector3Int positionOffset = GetMirroredPosition(rule.m_NeighborPositions[i], mirrorX, mirrorY);
                TileBase other = tilemap.GetTile(GetOffsetPosition(position, positionOffset));
                if (!RuleMatch(neighbor, other))
                {
                    return false;
                }
            }
            return true;
        }

        //Creates a rondom transform matrix
        /// <param name="type">Random transform rule.</param>
        /// <param name="original">The original transform matrix.</param>
        /// <param name="perlinScale">The Perlin Scale factor of the Tile.</param>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        public virtual Matrix4x4 ApplyRandomTransform(DoodaTilingRule.Transform type, Matrix4x4 original, float perlinScale, Vector3Int position)
        {
            float perlin = GetPerlinValue(position, perlinScale, 200000f);
            switch (type)
            {
                case DoodaTilingRule.Transform.MirrorXY:
                    return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Math.Abs(perlin - 0.5) > 0.25 ? 1f : -1f, perlin < 0.5 ? 1f : -1f, 1f));
                case DoodaTilingRule.Transform.MirrorX:
                    return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(perlin < 0.5 ? 1f : -1f, 1f, 1f));
                case DoodaTilingRule.Transform.MirrorY:
                    return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, perlin < 0.5 ? 1f : -1f, 1f));
                case DoodaTilingRule.Transform.Rotated:
                    int angle = Mathf.Clamp(Mathf.FloorToInt(perlin * m_RotationCount), 0, m_RotationCount - 1) * m_RotationAngle;
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
            }
            return original;
        }

        public FieldInfo[] GetCustomFields(bool isOverrideInstance)
        {
            return this.GetType().GetFields()
                .Where(field => typeof(DoodaRuleTile).GetField(field.Name) == null)
                .Where(field => !field.IsDefined(typeof(HideInInspector)))
                .Where(field => !isOverrideInstance || !field.IsDefined(typeof(DoodaRuleTile.DontOverride)))
                .ToArray();
        }

        //MATH Calculations//

        /// <param name="position">Original position of Tile.</param>
        /// <param name="rotation">Rotation in degrees.</param>
        public virtual Vector3Int GetRotatedPosition(Vector3Int position, int rotation)
        {
            switch (rotation)
            {
                case 0:
                    return position;
                case 90:
                    return new Vector3Int(position.y, -position.x, 0);
                case 180:
                    return new Vector3Int(-position.x, -position.y, 0);
                case 270:
                    return new Vector3Int(-position.y, position.x, 0);
            }
            return position;
        }

        /// <param name="position">Original position of Tile.</param>
        /// <param name="mirrorX">Mirror in the X Axis.</param>
        /// <param name="mirrorY">Mirror in the Y Axis.</param>
        public virtual Vector3Int GetMirroredPosition(Vector3Int position, bool mirrorX, bool mirrorY)
        {
            if (mirrorX)
                position.x *= -1;
            if (mirrorY)
                position.y *= -1;
            return position;
        }

        /// <param name="location">The location being offset.</param>
        /// <param name="offset">Offset distance.</param>
        public virtual Vector3Int GetOffsetPosition(Vector3Int location, Vector3Int offset)
        {
            return location + offset;
        }

        /// <param name="location">The location being offset.</param>
        /// <param name="offset">Offset distance.</param>
        public virtual Vector3Int GetOffsetPositionReverse(Vector3Int position, Vector3Int offset)
        {
            return position - offset;
        }
    }
}
