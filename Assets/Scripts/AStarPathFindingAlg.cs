using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// Uses weighting but not smooth wighting. 
/// Can be implimented at : https://www.youtube.com/watch?v=Tb-rM3wGwv4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=7

public class AStarPathFindingAlg : MonoBehaviour
{
    private AStarPathFindingScript m_Script;

    private void Awake()
    {
        m_Script = GetComponent<AStarPathFindingScript>();
    }
    public void FindPath(PathRequest request, Action<PathResult> callback)
    {
        m_Script.SetNodes();

        Vector3[] wayPoints = new Vector3[0];
        bool pathSuccess = false;

        AStarNodes startingNode = m_Script.GetNodeFromWorldPos(request.PathStart);
        AStarNodes targetNode = m_Script.GetNodeFromWorldPos(request.PathEnd);
        startingNode.Parent = startingNode;

        if (startingNode.Passable && targetNode.Passable)
        {
            Heap<AStarNodes> openSet = new Heap<AStarNodes>(m_Script.MaxSize);
            HashSet<AStarNodes> closedSet = new HashSet<AStarNodes>();
            openSet.Add(startingNode);

            while (openSet.Count > 0)
            {
                AStarNodes CurrentNode = openSet.RemoveFirstItem();
                closedSet.Add(CurrentNode);

                if (CurrentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                foreach (AStarNodes neighbour in m_Script.GetNeighbours(CurrentNode))
                {
                    if (!neighbour.Passable || closedSet.Contains(neighbour))
                        continue;

                    int newMovementCost = CurrentNode.GCost + GetDistance(CurrentNode, neighbour) + neighbour.Penilty;

                    if (newMovementCost < neighbour.GCost || !openSet.Contains(neighbour))
                    {
                        neighbour.GCost = newMovementCost;
                        neighbour.HCost = GetDistance(neighbour, targetNode);
                        neighbour.Parent = CurrentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);

                    }
                }
            }
        }
        if (pathSuccess)
        {
            wayPoints = RetracePath(startingNode, targetNode);
            pathSuccess = wayPoints.Length > 0;
        }
        callback(new PathResult(wayPoints, pathSuccess, request.CallBack));
    }
    Vector3[] RetracePath(AStarNodes startNode, AStarNodes endNode)
    {
        List<AStarNodes> path = new List<AStarNodes>();
        AStarNodes currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        Vector3[] wayPoints = SimplifyPath(path);
        Array.Reverse(wayPoints);
        return wayPoints;
    }
    private Vector3[] SimplifyPath(List<AStarNodes> path)
    {
        List<Vector3> wayPoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].GridPosition.x - path[i].GridPosition.x,
                path[i - 1].GridPosition.y - path[i].GridPosition.y);
            if (directionNew != directionOld)
            {
                wayPoints.Add(path[i].Position);
            }
            directionOld = directionNew;
        }

        return wayPoints.ToArray();
    }
    int GetDistance(AStarNodes nodeA, AStarNodes nodeB)
    {
        int distX = (int)Mathf.Abs(nodeA.Position.x - nodeB.Position.x);
        int distY = (int)Mathf.Abs(nodeA.Position.y - nodeB.Position.y);

        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        return 14 * distX + 10 * (distY - distX);
    }
}
