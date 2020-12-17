using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

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
            int swapIndex = 0;

            if (childIndexLeft < m_CurretItemCount)
            {
                swapIndex = childIndexLeft;

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

public class AStarPathFindingScript : MonoBehaviour
{
    /// Inspector Variables
    [SerializeField] private Tilemap[] I_TileMapArray = null;
    [SerializeField] private Vector2Int I_NumberOfGrids;
    [SerializeField] private Vector2 I_Position = Vector2.zero;
    [SerializeField] private float I_NodeSize = 1;
    [SerializeField] private bool I_DisplayGridGizmos = false;
    [SerializeField] private int I_PeniltyForWalls = 1;
    [SerializeField] private int I_PeniltyMultiplyerForEndOfWall = 4;
    [SerializeField] private int I_PeniltyMultiplyerForBomb = 4;
    [SerializeField] private int I_BlurPenaltyScale = 2;

    ///Private Variables
    private AStarNodes[,] m_Nodes;

    int m_BlurredMin = int.MaxValue;
    int m_BlurredMax = int.MinValue;

    private void Awake()
    {
        //SetNodes();
    }
    public void SetNodes()
    { 
        m_Nodes = new AStarNodes[I_NumberOfGrids.x, I_NumberOfGrids.y];
        for (int y = 0; y < I_NumberOfGrids.y; y++)
        {
            for (int x = 0; x < I_NumberOfGrids.x; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                foreach (Tilemap t in I_TileMapArray)
                {
                    Vector3Int tilePos = t.LocalToCell(WorldSpaceFromGrid3(pos));
                    if (t.GetTile(tilePos) != null)
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
                            !t.GetTile(tilePos + Vector3Int.left) && !t.GetTile(tilePos + Vector3Int.up)||
                            !t.GetTile(tilePos + Vector3Int.left) && !t.GetTile(tilePos + Vector3Int.down))
                            penalty *= I_PeniltyMultiplyerForEndOfWall;

                        m_Nodes[x, y] = new AStarNodes(false, WorldSpaceFromGrid(pos), pos, penalty);
                    }
                }
                if (m_Nodes[x, y] == null)
                {
                    m_Nodes[x, y] = new AStarNodes(true, WorldSpaceFromGrid(pos), pos, 0);
                }
            }
        }

        //Mark bombs as a bad place to go
        foreach (Bomb b in GameObject.FindObjectsOfType<Bomb>())
        {
            AStarNodes on = GetNodeFromWorldPos(b.transform.position);

            int radius = (int) (2.0f / I_NodeSize);
            
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int pos = on.GridPosition + new Vector2Int(x, y);
                    if (pos.x >= 0 && pos.x < I_NumberOfGrids.x && pos.y >= 0 && pos.y < I_NumberOfGrids.y)
                        m_Nodes[pos.x, pos.y] = new AStarNodes(false, WorldSpaceFromGrid(pos), pos, I_PeniltyMultiplyerForBomb);
                }
            }
        }

        //Avoid the Tanks
        foreach (Tank b in GameObject.FindObjectsOfType<Tank>())
        {
            AStarNodes on = GetNodeFromWorldPos(b.GetComponentInChildren<Rigidbody2D>().position); 
            m_Nodes[on.GridPosition.x, on.GridPosition.y] = new AStarNodes(true, on.Position, on.GridPosition, I_PeniltyForWalls);
        }

        BlurPenaltyMap(I_BlurPenaltyScale);
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(I_Position, new Vector3 (I_NumberOfGrids.x * I_NodeSize, I_NumberOfGrids.y * I_NodeSize, 1));

        if (m_Nodes != null && I_DisplayGridGizmos)
        {
            foreach (AStarNodes n in m_Nodes)
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
        return m_Nodes[(int)((position.x + I_NumberOfGrids.x * I_NodeSize / 2) / I_NodeSize),
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
                    neighbours.Add(m_Nodes[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }
    private void BlurPenaltyMap(int blurSize)
    {
        int kernalSize = blurSize * 2 + 1;
        int kernelExtents = blurSize;

        int[,] penaltiesHorezontalPass = new int[I_NumberOfGrids.x, I_NumberOfGrids.y];
        int[,] penaltiesVerticalPass = new int[I_NumberOfGrids.x, I_NumberOfGrids.y];

        for (int y = 0; y < I_NumberOfGrids.y; y++)
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorezontalPass[0, y] += m_Nodes[sampleX, y].Penilty;
            }

            for (int x = 1; x < I_NumberOfGrids.x; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, I_NumberOfGrids.x);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, I_NumberOfGrids.x - 1);

                penaltiesHorezontalPass[x, y] = penaltiesHorezontalPass[x - 1, y] - m_Nodes[removeIndex, y].Penilty + m_Nodes[addIndex, y].Penilty;

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
            m_Nodes[x, 0].Penilty = blurredPenalty;

            for (int y = 1; y < I_NumberOfGrids.y; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, I_NumberOfGrids.y);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, I_NumberOfGrids.y - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y-1] - penaltiesHorezontalPass[x, removeIndex] + penaltiesHorezontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernalSize * kernalSize));
                m_Nodes[x, y].Penilty = blurredPenalty;

                if (blurredPenalty > m_BlurredMax)
                    m_BlurredMax = blurredPenalty;
                if (blurredPenalty < m_BlurredMin)
                    m_BlurredMin = blurredPenalty;
            }
        }
    }
}
