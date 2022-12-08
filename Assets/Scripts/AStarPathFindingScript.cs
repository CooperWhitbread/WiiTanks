using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.UIElements;

public class AStarPathFindingScript : MonoBehaviour
{
    /// Inspector Variables
    [SerializeField] private Vector2Int I_NumberOfGrids;
    [SerializeField] private Vector2 I_Position = Vector2.zero;
    [SerializeField] private float I_NodeSize = 1;
    [SerializeField] private bool I_DisplayGridGizmos = false;
    [SerializeField] private int I_PeniltyForWalls = 1;
    [SerializeField] private int I_PeniltyMultiplyerForEndOfWall = 20;
    [SerializeField] private int I_PeniltyMultiplyerForBomb = 20;
    [SerializeField] private int I_BlurPenaltyScale = 2;

    ///Private Variables
    private AStarNodes[,] m_NodesStatic;
    private AStarNodes[,] m_NodeDynamic;

    private Tilemap[] m_TileMapArray = new Tilemap[0];
    int m_BlurredMin = int.MaxValue;
    int m_BlurredMax = int.MinValue;

    public void SetNodeDynamic()
    {
        m_NodeDynamic = m_NodesStatic;

        //Mark bombs as a bad place to go
        foreach (Bomb b in FindObjectsOfType<Bomb>())
        {
            AStarNodes on = GetNodeFromWorldPos(b.transform.position);

            int radius = (int)(2.0f / I_NodeSize);

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int pos = on.GridPosition + new Vector2Int(x, y);
                    Vector2 worldPos = WorldSpaceFromGrid(pos);
                    double test = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
                    if (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) <= 2)
                    {
                        if (pos.x >= 0 && pos.x < I_NumberOfGrids.x && pos.y >= 0 && pos.y < I_NumberOfGrids.y)
                            m_NodeDynamic[pos.x, pos.y] = new AStarNodes(false, worldPos, pos, I_PeniltyMultiplyerForBomb);
                    }
                }
            }
        }

        //Avoid the Tanks
        foreach (Tank b in FindObjectsOfType<BrownTank>())
        {
            AStarNodes on = GetNodeFromWorldPos(b.GetComponentInChildren<Rigidbody2D>().position);
            m_NodeDynamic[on.GridPosition.x, on.GridPosition.y] = new AStarNodes(true, on.Position, on.GridPosition, I_PeniltyForWalls);
        }
    }
    public void SetNodesStatic()
    {
        //GetTiles
        m_TileMapArray = new Tilemap[2];
        int interval = 0;
        foreach (Tilemap tm in FindObjectsOfType<Tilemap>())
        {
            if (tm.name == GlobalVariables.TilemapWallName || tm.name == GlobalVariables.TilemapHoleName)
            {
                if (interval < 2)
                    m_TileMapArray[interval++] = tm;
            }
        }

        m_NodesStatic = new AStarNodes[I_NumberOfGrids.x, I_NumberOfGrids.y];
        for (int y = 0; y < I_NumberOfGrids.y; y++)
        {
            for (int x = 0; x < I_NumberOfGrids.x; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (m_TileMapArray.Length != 0)
                {
                    if (m_TileMapArray[0])
                    {
                        foreach (Tilemap t in m_TileMapArray)
                        {
                            Vector3Int tilePos = t.LocalToCell(WorldSpaceFromGrid3(pos));
                            if (t.GetTile(tilePos))
                            {
                                //End multiply
                                int penalty = I_PeniltyForWalls;
                                int neighbours = 0;
                                if (t.GetTile(tilePos + Vector3Int.right))
                                    neighbours++;
                                if (t.GetTile(tilePos + Vector3Int.left))
                                    neighbours++;
                                if (t.GetTile(tilePos + Vector3Int.up))
                                    neighbours++;
                                if (t.GetTile(tilePos + Vector3Int.down))
                                    neighbours++;

                                if (neighbours <= 1)
                                    penalty *= I_PeniltyMultiplyerForEndOfWall;

                                //Corner multiply
                                if (!t.GetTile(tilePos + Vector3Int.right) && !t.GetTile(tilePos + Vector3Int.up) ||
                                    !t.GetTile(tilePos + Vector3Int.right) && !t.GetTile(tilePos + Vector3Int.down) ||
                                    !t.GetTile(tilePos + Vector3Int.left) && !t.GetTile(tilePos + Vector3Int.up) ||
                                    !t.GetTile(tilePos + Vector3Int.left) && !t.GetTile(tilePos + Vector3Int.down))
                                    penalty *= I_PeniltyMultiplyerForEndOfWall;

                                m_NodesStatic[x, y] = new AStarNodes(false, WorldSpaceFromGrid(pos), pos, penalty);
                            }
                        }
                    }
                }
                if (m_NodesStatic[x, y] == null)
                {
                    m_NodesStatic[x, y] = new AStarNodes(true, WorldSpaceFromGrid(pos), pos, 0);
                }
            }
        }

        BlurPenaltyMap(I_BlurPenaltyScale, ref m_NodesStatic);

        //Correct to avoid corners
        {
            for (int y = 0; y < I_NumberOfGrids.y; y++)
            {
                for (int x = 0; x < I_NumberOfGrids.x; x++)
                {
                    if (x > 0 && x < I_NumberOfGrids.x - 1 && y > 0 && y < I_NumberOfGrids.y - 1)
                    {
                        Vector2Int pos = new Vector2Int(x, y);

                        if (m_TileMapArray.Length != 0)
                        {
                            if (m_TileMapArray[0])
                            {
                                foreach (Tilemap t in m_TileMapArray)
                                {
                                    Vector3Int tilePos = t.LocalToCell(WorldSpaceFromGrid3(pos));
                                    if (t.GetTile(tilePos))
                                    {
                                        //End multiply
                                        int neighbours = 0;
                                        if (t.GetTile(tilePos + Vector3Int.right))
                                            neighbours++;
                                        if (t.GetTile(tilePos + Vector3Int.left))
                                            neighbours++;
                                        if (t.GetTile(tilePos + Vector3Int.up))
                                            neighbours++;
                                        if (t.GetTile(tilePos + Vector3Int.down))
                                            neighbours++;

                                        if (neighbours == 1)
                                        {
                                            int penaltyMultiplyer = 2;
                                            //Get the two nodes on the end
                                            if (t.GetTile(tilePos + Vector3Int.right))
                                            {
                                                if (m_NodesStatic[x - 1, y].Passable && m_NodesStatic[x, y - 1].Passable)
                                                    m_NodesStatic[x - 1, y - 1].Penilty *= penaltyMultiplyer;
                                                if (m_NodesStatic[x - 1, y].Passable && m_NodesStatic[x, y + 1].Passable)
                                                    m_NodesStatic[x - 1, y + 1].Penilty *= penaltyMultiplyer;
                                            }
                                            if (t.GetTile(tilePos + Vector3Int.left))
                                            {
                                                if (m_NodesStatic[x + 1, y].Passable && m_NodesStatic[x, y - 1].Passable)
                                                    m_NodesStatic[x + 1, y - 1].Penilty *= penaltyMultiplyer;
                                                if (m_NodesStatic[x + 1, y].Passable && m_NodesStatic[x, y + 1].Passable)
                                                    m_NodesStatic[x + 1, y + 1].Penilty *= penaltyMultiplyer;
                                            }
                                            if (t.GetTile(tilePos + Vector3Int.up))
                                            {
                                                if (m_NodesStatic[x - 1, y].Passable && m_NodesStatic[x, y - 1].Passable)
                                                    m_NodesStatic[x - 1, y - 1].Penilty *= penaltyMultiplyer;
                                                if (m_NodesStatic[x + 1, y].Passable && m_NodesStatic[x, y - 1].Passable)
                                                    m_NodesStatic[x + 1, y - 1].Penilty *= penaltyMultiplyer;
                                            }
                                            if (t.GetTile(tilePos + Vector3Int.down))
                                            {
                                                if (m_NodesStatic[x - 1, y].Passable && m_NodesStatic[x, y + 1].Passable)
                                                    m_NodesStatic[x - 1, y + 1].Penilty *= penaltyMultiplyer;
                                                if (m_NodesStatic[x + 1, y].Passable && m_NodesStatic[x, y + 1].Passable)
                                                    m_NodesStatic[x + 1, y + 1].Penilty *= penaltyMultiplyer;
                                            }
                                        }

                                        //Make path through center of two
                                        if (!t.GetTile(tilePos + Vector3Int.right) && t.GetTile(tilePos + Vector3Int.right * 2))
                                        {
                                            int penaltyMultiplyer = 10;
                                            if (m_NodesStatic[x + 1, y].Passable)
                                            {
                                                m_NodesStatic[x + 1, y].Penilty /= penaltyMultiplyer;
                                                if (m_NodesStatic[x + 2, y + 2].Passable)
                                                {
                                                    m_NodesStatic[x + 2, y + 1].Penilty /= penaltyMultiplyer;
                                                    m_NodesStatic[x + 2, y + 2].Penilty /= penaltyMultiplyer;
                                                }
                                                if (m_NodesStatic[x + 2, y - 2].Passable)
                                                {
                                                    m_NodesStatic[x + 2, y - 1].Penilty /= penaltyMultiplyer;
                                                    m_NodesStatic[x + 2, y - 2].Penilty /= penaltyMultiplyer;
                                                }
                                            }
                                        }
                                        if (!t.GetTile(tilePos + Vector3Int.up) && t.GetTile(tilePos + Vector3Int.up * 2))
                                        {
                                            int penaltyMultiplyer = 10;
                                            if (m_NodesStatic[x, y + 1].Passable)
                                            {
                                                m_NodesStatic[x, y + 2].Penilty /= penaltyMultiplyer;
                                                if (m_NodesStatic[x + 2, y + 2].Passable)
                                                {
                                                    m_NodesStatic[x + 1, y + 2].Penilty /= penaltyMultiplyer;
                                                    m_NodesStatic[x + 2, y + 2].Penilty /= penaltyMultiplyer;
                                                }
                                                if (m_NodesStatic[x - 2, y + 2].Passable)
                                                {
                                                    m_NodesStatic[x - 1, y + 2].Penilty /= penaltyMultiplyer;
                                                    m_NodesStatic[x - 2, y + 2].Penilty /= penaltyMultiplyer;
                                                }
                                            }
                                        }

                                        //Corners avoid
                                        if (neighbours == 2)
                                        {
                                            int penaltyMultiplyer = 3;
                                            if (t.GetTile(tilePos + Vector3Int.right))
                                            {
                                                if (t.GetTile(tilePos + Vector3Int.up))
                                                {
                                                    if (m_NodesStatic[x - 1, y - 1].Passable)
                                                        m_NodesStatic[x - 1, y - 1].Penilty *= penaltyMultiplyer;
                                                }
                                                if (t.GetTile(tilePos + Vector3Int.down))
                                                {
                                                    if (m_NodesStatic[x - 1, y + 1].Passable)
                                                        m_NodesStatic[x - 1, y + 1].Penilty *= penaltyMultiplyer;
                                                }
                                            }
                                            if (t.GetTile(tilePos + Vector3Int.left))
                                            {
                                                if (t.GetTile(tilePos + Vector3Int.up))
                                                {
                                                    if (m_NodesStatic[x + 1, y - 1].Passable)
                                                        m_NodesStatic[x + 1, y - 1].Penilty *= penaltyMultiplyer;
                                                }
                                                if (t.GetTile(tilePos + Vector3Int.down))
                                                {
                                                    if (m_NodesStatic[x + 1, y + 1].Passable)
                                                        m_NodesStatic[x + 1, y + 1].Penilty *= penaltyMultiplyer;
                                                }
                                            }
                                        }
                                    }

                                    else //no tile
                                    {
                                        int neighbours = 0;
                                        if (t.GetTile(t.LocalToCell(WorldSpaceFromGrid3(pos + Vector2Int.right))))
                                            neighbours++;
                                        if (t.GetTile(t.LocalToCell(WorldSpaceFromGrid3(pos + Vector2Int.left))))
                                            neighbours++;
                                        if (t.GetTile(t.LocalToCell(WorldSpaceFromGrid3(pos + Vector2Int.up))))
                                            neighbours++;
                                        if (t.GetTile(t.LocalToCell(WorldSpaceFromGrid3(pos + Vector2Int.down))))
                                            neighbours++;

                                        if (neighbours >= 1)
                                        {
                                            m_NodesStatic[x, y] = new AStarNodes(false, WorldSpaceFromGrid(pos), pos, m_NodesStatic[x, y].Penilty);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        
        }
    }
    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(I_Position, new Vector3 (I_NumberOfGrids.x * I_NodeSize, I_NumberOfGrids.y * I_NodeSize, 1));

        if (m_NodesStatic != null && I_DisplayGridGizmos)
        {
            foreach (AStarNodes n in m_NodesStatic)
            {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(m_BlurredMin, m_BlurredMax, n.Penilty));

                Gizmos.color = (n.Passable) ? Gizmos.color : Color.red;
                Gizmos.DrawCube(n.Position, Vector3.one * I_NodeSize);
            }
        }
    }
    public int MaxSize
    {
        get { return I_NumberOfGrids.x * I_NumberOfGrids.y; }
    }

    public Vector2 WorldSpaceFromGrid(Vector2Int gridCoord)
    {
        return new Vector2((gridCoord.x - I_NumberOfGrids.x / 2) * I_NodeSize + I_Position.x + I_NodeSize / 2, 
            (gridCoord.y - I_NumberOfGrids.y / 2) * I_NodeSize + I_Position.y + I_NodeSize / 2);
    }
    public Vector3 WorldSpaceFromGrid3(Vector2Int gridCoord)
    {
        return new Vector3(WorldSpaceFromGrid(gridCoord).x, WorldSpaceFromGrid(gridCoord).y, 1);
    }
    public AStarNodes GetNodeFromWorldPos(Vector2 position)
    {
        return m_NodeDynamic[(int)((position.x + I_NumberOfGrids.x * I_NodeSize / 2) / I_NodeSize),
            (int)((position.y + I_NumberOfGrids.y * I_NodeSize / 2) / I_NodeSize)];
    }
    public List<AStarNodes> GetNeighbours(AStarNodes node)
    {
        List<AStarNodes> neighbours = new List<AStarNodes>();

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.GridPosition.x + x;
                int checkY = node.GridPosition.y + y;

                if (checkX >= 0 && checkX < I_NumberOfGrids.x && checkY >= 0 && checkY < I_NumberOfGrids.y)
                {
                    neighbours.Add(m_NodeDynamic[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }
    private void BlurPenaltyMap(int blurSize, ref AStarNodes[,] nodes)
    {
        int kernalSize = blurSize * 2 + 1;
        int kernelExtents = blurSize;

        int[,] penaltiesHorezontalPass = new int[I_NumberOfGrids.x, I_NumberOfGrids.y];
        int[,] penaltiesVerticalPass = new int[I_NumberOfGrids.x, I_NumberOfGrids.y];

        //Basic blur
        for (int y = 0; y < I_NumberOfGrids.y; y++)
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorezontalPass[0, y] += nodes[sampleX, y].Penilty;
            }

            for (int x = 1; x < I_NumberOfGrids.x; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, I_NumberOfGrids.x);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, I_NumberOfGrids.x - 1);

                penaltiesHorezontalPass[x, y] = penaltiesHorezontalPass[x - 1, y] - nodes[removeIndex, y].Penilty + nodes[addIndex, y].Penilty;

            }
        }

        for (int x = 0; x < I_NumberOfGrids.x; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorezontalPass[x, sampleY];
            }


            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernalSize * kernalSize));
            nodes[x, 0].Penilty = blurredPenalty;

            for (int y = 1; y < I_NumberOfGrids.y; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, I_NumberOfGrids.y);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, I_NumberOfGrids.y - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y-1] - penaltiesHorezontalPass[x, removeIndex] + penaltiesHorezontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernalSize * kernalSize));
                nodes[x, y].Penilty = blurredPenalty;

                if (blurredPenalty > m_BlurredMax)
                    m_BlurredMax = blurredPenalty;
                if (blurredPenalty < m_BlurredMin)
                    m_BlurredMin = blurredPenalty;
            }
        }
    }
    public Vector2 IsSurroundedByTwoWalls(Vector2 worldPos)
    {
        foreach (Tilemap t in m_TileMapArray)
        {
            Vector3Int tilePos = t.LocalToCell(worldPos);
            if (t.GetTile(tilePos + Vector3Int.right) && t.GetTile(tilePos + Vector3Int.left))
                return Vector2.right;
            if (t.GetTile(tilePos + Vector3Int.up) && t.GetTile(tilePos + Vector3Int.down))
                return Vector2.up;
        }

        return Vector2.zero;
    }
    public Vector2 GetCenterOfTile(Vector2 worldSpace)
    {
        return m_TileMapArray[0].GetCellCenterWorld(m_TileMapArray[0].LocalToCell(worldSpace));
    }
    public bool CheckTileMap()
    {
        return m_TileMapArray[0];
    }
}

public class Heap<T> where T : IHeapItem<T>
{
    private T[] m_Items;
    private int m_CurretItemCount;

    public Heap (int maxHeapSize)
    {
        m_Items = new T[maxHeapSize];
    }
    public void Add(T item)
    {
        item.HeapIndex = m_CurretItemCount;
        m_Items[m_CurretItemCount] = item;
        SortUp(item);
        m_CurretItemCount++;
    }
    public T RemoveFirstItem()
    {
        T firstItem = m_Items[0];
        m_CurretItemCount--;

        m_Items[0] = m_Items[m_CurretItemCount];
        m_Items[0].HeapIndex = 0;
        SortDown(m_Items[0]);
        return firstItem;
    }
    private void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (true)
        {
            T parentItem = m_Items[parentIndex];
            if (item.CompareTo(parentItem) > 0)
                Swap(item, parentItem);
            else break;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }
    public bool Contains(T item)
    {
        return Equals(m_Items[item.HeapIndex], item);
    }
    public int Count
    {
        get { return m_CurretItemCount; }
    }
    public void UpdateItem(T item)
    {
        SortUp(item);
    }
    public void SortDown(T item)
    {
        while (true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;

            if (childIndexLeft < m_CurretItemCount)
            {
                int swapIndex = childIndexLeft;

                if (childIndexLeft < m_CurretItemCount)
                {
                    if (m_Items[childIndexLeft].CompareTo(m_Items[childIndexRight]) < 0)
                        swapIndex = childIndexRight;
                }
                if (item.CompareTo(m_Items[swapIndex]) < 0)
                    Swap(item, m_Items[swapIndex]);
                else return;
            }
            else return;
        }
    }
    private void Swap(T itemA, T itemB)
    {
        m_Items[itemA.HeapIndex] = itemB;
        m_Items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}

public class AStarNodes : IHeapItem<AStarNodes>
{
    public bool Passable;
    public Vector2 Position;
    public Vector2Int GridPosition;
    public AStarNodes Parent;
    private int m_HeapIndex;
    public int Penilty;
    
    public int GCost;
    public int HCost;

    public int FCost { get { return GCost + HCost; } }
    public AStarNodes(bool passable, Vector2 pos, Vector2Int gridPos, int penilty)
    {
        Passable = passable;
        Position = pos;
        GridPosition = gridPos;
        Penilty = penilty;
    }
    public int HeapIndex
    {
        get { return m_HeapIndex; }
        set { m_HeapIndex = value; }
    }
    public int CompareTo(AStarNodes nodeToCompare)
    {
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if (compare == 0)
            compare = HCost.CompareTo(nodeToCompare.HCost);

        return -compare;
    }
}
