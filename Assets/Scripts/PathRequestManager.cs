using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class PathRequestManager : MonoBehaviour
{
    Queue<PathResult> m_Results = new Queue<PathResult>();
    private AStarPathFindingAlg m_Pathfinding;

    static PathRequestManager S_Instance;

    private void Start()
    {
        S_Instance = this;
        m_Pathfinding = GetComponent<AStarPathFindingAlg>();
        ResetWallsPathFinding();
    }
    private void Update()
    {
        if (m_Results.Count > 0)
        {
            int itemsInQueue = m_Results.Count;
            lock (m_Results)
            {
                for (int i = 0; i < itemsInQueue; i++)
                {
                    PathResult result = m_Results.Dequeue();
                    result.Callback(result.Path, result.Success);
                }
            }
        }
    }

    public static void ResetWallsPathFinding() 
    {
        if (S_Instance)
            S_Instance.m_Pathfinding.SetStaticNodes(); 
    }
    public static void RequestPath(PathRequest request)
    {
        if (S_Instance)
        {
            ThreadStart threadStart = delegate
            {
                S_Instance.m_Pathfinding.FindPath(request, S_Instance.FinishedProccessingPath);
            };

            threadStart.Invoke();
        }
        else
        {
            Debug.LogError("S_Instance in Path Request not set");
            request.CallBack(new Vector3[0], false);
        }
    }
    public void FinishedProccessingPath(PathResult result)
    { 
        lock (m_Results)
        {
            m_Results.Enqueue(result);
        }
    }

}

public struct PathRequest
{
    public Vector3 PathStart;
    public Vector3 PathEnd;
    public Action<Vector3[], bool> CallBack;

    public PathRequest(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callBack)
    {
        PathStart = pathStart;
        PathEnd = pathEnd;
        CallBack = callBack;
    }
}

public struct PathResult
{
    public Vector3[] Path;
    public bool Success;
    public Action<Vector3[], bool> Callback;

    public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback)
    {
        Path = path;
        Success = success;
        Callback = callback;
    }
}
