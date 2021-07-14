using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Tilemaps;

/// Uses weighting but not smooth wighting. 
/// Can be implimented at : https://www.youtube.com/watch?v=Tb-rM3wGwv4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=7

public class AStarPathFindingAlg : MonoBehaviour
{
    private AStarPathFindingScript m_Script;

    public void Awake()
    {
        m_Script = GetComponent<AStarPathFindingScript>();
        m_Script.Update();
        m_Script.SetNodesStatic();
    }
    public void FindPath(PathRequest request, Action<PathResult> callback)
    {
        m_Script.SetNodeDynamic();

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

        Vector3[] wayPoints = SimplifyPathWithControllCorrection(path);
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
    private Vector3[] SimplifyPathWithControllCorrection(List<AStarNodes> path)
    {
        ///Working Use check of intersecting lines
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

        List<Vector3> wayPoints2 = new List<Vector3>();
        int num = 5;
        for (int i = 1; i < path.Count; i++)
        {
            //Check if going between narrow gap
            Vector2 dir = m_Script.IsSurroundedByTwoWalls(path[i].Position);
            if (dir != Vector2.zero)
            {
                if (dir == Vector2.up)
                {
                    wayPoints2 = RemoveWayPoints(path[i].Position, 1.4f, wayPoints2);
                    wayPoints = RemoveWayPoints(path[i].Position, 1.4f, wayPoints);
                    Vector2 center = m_Script.GetCenterOfTile(path[i].Position);
                    for (int j = -num; j < num; j++)
                    {
                        if (i != 0) //Use Default to left
                        {
                            if (path[i - 1].Position.x - path[i].Position.x > 0)
                            {
                                //Right to Left
                                Vector2 waypoint = center + Vector2.left * j / 3;
                                if (CheckIfWayPointsAreBehind(waypoint))
                                    wayPoints2.Add(waypoint);
                            }
                            else
                            {
                                //Left to right
                                Vector2 waypoint = center + Vector2.right * j / 3;
                                if (CheckIfWayPointsAreBehind(waypoint))
                                    wayPoints2.Add(waypoint);
                            }
                        }
                    }
                }
                else // if (dir == Vector2.right)
                {
                    wayPoints2 = RemoveWayPoints(path[i].Position, 1.4f, wayPoints2);
                    wayPoints = RemoveWayPoints(path[i].Position, 1.4f, wayPoints);
                    Vector2 center = m_Script.GetCenterOfTile(path[i].Position);
                    for (int j = -num; j < num; j++)
                    {
                        if (i != 0) //Use Default to left
                        {
                            if (path[i - 1].Position.y - path[i].Position.y > 0)
                            {
                                //up to down
                                Vector2 waypoint = center + Vector2.down * j / 3;
                                if (CheckIfWayPointsAreBehind(waypoint))
                                    wayPoints2.Add(waypoint);
                            }
                            else
                            {
                                //down to up
                                Vector2 waypoint = center + Vector2.up * j / 3;
                                if (CheckIfWayPointsAreBehind(waypoint))
                                    wayPoints2.Add(waypoint);
                            }
                        }
                    }
                }
            }
            else
            {
                if (wayPoints.Contains(path[i].Position))
                {
                    wayPoints2.Add(path[i].Position);
                }
            }
        }

        return wayPoints2.ToArray();
    }
    private bool CheckIfWayPointsAreBehind(Vector2 waypoint)
    {
        Vector2 pos = GlobalVariables.GetPlayerTankBody().position;
        Vector2 vel = GlobalVariables.GetPlayerTankBody().velocity;
        if (Vector2.Distance(pos, waypoint) < 2.0f
            && (Tank.Dot(vel, waypoint - pos) < 0
            || vel.normalized == pos - waypoint))
            return false;
        return true;
    }
    private int GetDistance(AStarNodes nodeA, AStarNodes nodeB)
    {
        int distX = (int)Mathf.Abs(nodeA.Position.x - nodeB.Position.x);
        int distY = (int)Mathf.Abs(nodeA.Position.y - nodeB.Position.y);

        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        return 14 * distX + 10 * (distY - distX);
    }
    private List<Vector3> RemoveWayPoints(Vector2 position, float dist, List<Vector3> wayPoints)
    {
        for (int i = 0; i < wayPoints.Count; i++) 
        {
            if (Vector2.Distance(position, wayPoints[i]) < dist)
            {
                wayPoints.RemoveAt(i);
                i--;
            }
        }

        return wayPoints;
    }
}
