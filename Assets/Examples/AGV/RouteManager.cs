using System.Collections.Generic;
using UnityEngine;

public class RouteManager : MonoBehaviour
{
    public WayPoint[] allWaypoints;

    //딕셔너리 - key, value
    private Dictionary<int, WayPoint> waypointDic = new Dictionary<int, WayPoint>();

    /* public void InitializeWaypoints()
     {
         waypointDic.Clear();
         foreach(WayPoint w in allWaypoints)
         {
             if (waypointDic.ContainsKey(w.id))
             {
                 continue;
             }
             waypointDic.Add(w.id, w);
         }
         Debug.Log($"[RouteManager]에 총 {waypointDic.Count}개의 웨이포인트가 로드되었습니다.");
     }*/
    //26.11.24 LYJ: 수정중

    public void InitializeWaypoints()
    {
        waypointDic.Clear();

        if (allWaypoints == null || allWaypoints.Length == 0)
        {
            Debug.LogError(
                $"[{name}] All Waypoints가 설정되지 않았습니다.");
            return;
        }

        foreach (WayPoint w in allWaypoints)
        {
            if (w == null)
            {
                Debug.LogWarning(
                    $"[{name}] 비어 있는 Waypoint 항목이 있습니다.");
                continue;
            }

            if (waypointDic.ContainsKey(w.id))
            {
                Debug.LogWarning(
                    $"[{name}] 중복 Waypoint ID: {w.id}");
                continue;
            }

            waypointDic.Add(w.id, w);
        }

        Debug.Log(
            $"[RouteManager] 총 {waypointDic.Count}개의 웨이포인트가 로드되었습니다.");
    }


    /// <summary>
    /// 해당 위치 기준 가장 가까운 웨이포인트의 ID를 검색해서 반환
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public int GetNearesstWaypointID(Vector3 position)
    {
        if(waypointDic.Count == 0)
        {
            Debug.LogError($"[RouteManager]에 등록된 웨이포인트가 존재하지 않습니다. 새로운 웨이포인트를 추가해주세요.");
            return 0;
        }

        int foundID = 0;
        float minDistance = float.MaxValue;
        foreach(var pair in waypointDic)
        {
            float distanceSqr = (pair.Value.transform.position - position).sqrMagnitude;
            if(minDistance > distanceSqr)
            {
                minDistance = distanceSqr;
                foundID = pair.Key;
            }
        }
        return foundID;
    }

    /// <summary>
    /// 목적지까지의 최단 경로 탐색. 다익스트라 알고리즘 사용
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    private List<WayPoint> FindShortestPath(WayPoint start, WayPoint end)
    {
        Dictionary<WayPoint, float> distance = new Dictionary<WayPoint, float>();
        Dictionary<WayPoint, WayPoint> previousPoints = new Dictionary<WayPoint, WayPoint>();
        List<WayPoint> unvisited = new List<WayPoint>();

        //딕셔너리에 저장된 모든 등록 웨이포인트 순회
        foreach(var wp in waypointDic.Values)
        {
            distance[wp] = float.MaxValue;
            previousPoints[wp] = null;
            unvisited.Add(wp);
        }
        distance[start] = 0;

        while(unvisited.Count > 0)
        {
            unvisited.Sort((a, b) => distance[a].CompareTo(distance[b]));
            WayPoint current = unvisited[0];
            unvisited.RemoveAt(0);

            if (current == end) 
                break;
            if (distance[current] == float.MaxValue) 
                break;

            foreach(var neighbor in current.neighbors)
            {
                if (!unvisited.Contains(neighbor)) continue;

                float alt = distance[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if(alt < distance[neighbor])
                {
                    distance[neighbor] = alt;
                    previousPoints[neighbor] = current;
                }
            }
        }
        List<WayPoint> path = new List<WayPoint>();
        WayPoint curr = end;

        while(curr != null)
        {
            path.Insert(0, curr);
            curr = previousPoints[curr];
        }

        Debug.Log(path);
        if (path.Count == 0 || path[0] != start)
            return null;

        Debug.Log(path.Count);
        return path;
    }

    private void Awake()
    {
        InitializeWaypoints();
    }

    /// <summary>
    /// AGV 컨트롤러 쪽에서 최단 경로를 알아봐달라고 요청하는 함수
    /// </summary>
    /// <param name="controller">요청한 컨트롤러</param>
    /// <param name="startID">가장 가까운 웨이포인트 ID</param>
    /// <param name="endID">도착해야 할 웨이포인트 ID</param>
    /// <returns>경로 검색 성공 여부</returns>
    public void GetRoutes(AGVController controller, int startID, int endID)
    {
        if (waypointDic.Count == 0)
            return;

        if(!waypointDic.TryGetValue(startID, out WayPoint start))
        {
            return;
        }

        if(!waypointDic.TryGetValue(endID, out WayPoint end))
        {
            return;
        }

        Debug.Log("경로 탐색 시작");
        List<WayPoint> path = FindShortestPath(start, end);
        if (path == null || path.Count == 0)
            return;

        Debug.Log("경로 탐색 완료");
        //컨트롤러에게 최단 경로 가져다 주기
        controller.SetRoute(path);
    }
}
