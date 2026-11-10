
using System.Collections.Generic;
using UnityEngine;

public class PackRoute : MonoBehaviour
{
    [Header("배터리팩 공정 전체 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    private Dictionary<int, WayPoint> waypointDic =
        new Dictionary<int, WayPoint>();


    // ==========================================
    // Waypoint 초기화
    // ==========================================

    public void InitializeWaypoints()
    {
        waypointDic.Clear();

        if (allWaypoints == null || allWaypoints.Length == 0)
        {
            Debug.LogError(
                "[PackRoute] 등록된 Waypoint가 없습니다.");
            return;
        }

        foreach (WayPoint waypoint in allWaypoints)
        {
            if (waypoint == null)
            {
                Debug.LogWarning(
                    "[PackRoute] null Waypoint가 포함되어 있습니다.");
                continue;
            }

            if (waypointDic.ContainsKey(waypoint.id))
            {
                Debug.LogWarning(
                    $"[PackRoute] 중복된 Waypoint ID: {waypoint.id}");
                continue;
            }

            waypointDic.Add(waypoint.id, waypoint);
        }

        Debug.Log(
            $"[PackRoute] 총 {waypointDic.Count}개의 Waypoint 등록 완료");
    }


    // ==========================================
    // 가장 가까운 Waypoint 검색
    // ==========================================

    public int GetNearestWaypointID(Vector3 position)
    {
        if (waypointDic.Count == 0)
        {
            Debug.LogError(
                "[PackRoute] 등록된 Waypoint가 없습니다.");
            return -1;
        }

        int foundID = -1;
        float minDistance = float.MaxValue;

        foreach (var pair in waypointDic)
        {
            if (pair.Value == null)
                continue;

            float distanceSqr =
                (pair.Value.transform.position - position).sqrMagnitude;

            if (distanceSqr < minDistance)
            {
                minDistance = distanceSqr;
                foundID = pair.Key;
            }
        }

        return foundID;
    }


    // ==========================================
    // 최단 경로 탐색 - 다익스트라 알고리즘
    // ==========================================

    private List<WayPoint> FindShortestPath(
        WayPoint start,
        WayPoint end)
    {
        if (start == null || end == null)
            return null;

        Dictionary<WayPoint, float> distance =
            new Dictionary<WayPoint, float>();

        Dictionary<WayPoint, WayPoint> previousPoints =
            new Dictionary<WayPoint, WayPoint>();

        List<WayPoint> unvisited =
            new List<WayPoint>();


        foreach (WayPoint waypoint in waypointDic.Values)
        {
            distance[waypoint] = float.MaxValue;
            previousPoints[waypoint] = null;

            unvisited.Add(waypoint);
        }

        distance[start] = 0f;


        // 다익스트라 알고리즘
        while (unvisited.Count > 0)
        {
            unvisited.Sort(
                (a, b) => distance[a].CompareTo(distance[b]));

            WayPoint current = unvisited[0];

            unvisited.RemoveAt(0);

            if (current == end)
                break;

            if (distance[current] == float.MaxValue)
                break;

            if (current.neighbors == null)
                continue;

            foreach (WayPoint neighbor in current.neighbors)
            {
                if (neighbor == null)
                    continue;

                if (!distance.ContainsKey(neighbor))
                    continue;

                if (!unvisited.Contains(neighbor))
                    continue;

                float alt =
                    distance[current] +
                    Vector3.Distance(
                        current.transform.position,
                        neighbor.transform.position);

                if (alt < distance[neighbor])
                {
                    distance[neighbor] = alt;
                    previousPoints[neighbor] = current;
                }
            }
        }


        // 경로 역추적
        List<WayPoint> path = new List<WayPoint>();

        WayPoint currentPoint = end;

        while (currentPoint != null)
        {
            path.Insert(0, currentPoint);
            currentPoint = previousPoints[currentPoint];
        }


        if (path.Count == 0 || path[0] != start)
        {
            Debug.LogError(
                $"[PackRoute] 경로 탐색 실패: {start.id} -> {end.id}");

            return null;
        }

        Debug.Log(
            $"[PackRoute] 경로 탐색 결과: {string.Join(" -> ", path.ConvertAll(w => w.id.ToString()))}");

        return path;
    }


    // ==========================================
    // 초기 실행
    // ==========================================

    private void Awake()
    {
        InitializeWaypoints();
    }


    // ==========================================
    // PackController에 경로 전달
    // ==========================================

    public bool GetRoutes(
        PackController controller,
        int startID,
        int endID)
    {
        if (controller == null)
        {
            Debug.LogError(
                "[PackRoute] PackController가 연결되지 않았습니다.");
            return false;
        }

        if (waypointDic.Count == 0)
        {
            Debug.LogError(
                "[PackRoute] 등록된 Waypoint가 없습니다.");
            return false;
        }


        WayPoint start;
        WayPoint end;

        if (!waypointDic.TryGetValue(startID, out start))
        {
            Debug.LogError(
                $"[PackRoute] 출발 Waypoint를 찾을 수 없습니다. ID: {startID}");
            return false;
        }

        if (!waypointDic.TryGetValue(endID, out end))
        {
            Debug.LogError(
                $"[PackRoute] 목적지 Waypoint를 찾을 수 없습니다. ID: {endID}");
            return false;
        }

        Debug.Log(
            $"[PackRoute] 경로 탐색 시작: {startID} -> {endID}");


        List<WayPoint> path =
            FindShortestPath(start, end);

        if (path == null || path.Count == 0)
        {
            Debug.LogError(
                "[PackRoute] 경로 탐색에 실패했습니다.");
            return false;
        }

        controller.SetRoute(path);

        Debug.Log(
            "[PackRoute] 경로 탐색 및 전달 완료");

        return true;
    }
}
