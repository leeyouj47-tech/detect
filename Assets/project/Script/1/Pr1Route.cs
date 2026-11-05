
using System.Collections.Generic;
using UnityEngine;

public class Pr1Route : MonoBehaviour
{
    [Header("전체 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    // Waypoint ID를 기준으로 검색
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
                "[Pr1Route] 등록된 Waypoint가 없습니다.");

            return;
        }

        foreach (WayPoint waypoint in allWaypoints)
        {
            if (waypoint == null)
            {
                Debug.LogWarning(
                    "[Pr1Route] null Waypoint가 포함되어 있습니다.");

                continue;
            }

            if (waypointDic.ContainsKey(waypoint.id))
            {
                Debug.LogWarning(
                    $"[Pr1Route] 중복된 Waypoint ID: {waypoint.id}");

                continue;
            }

            waypointDic.Add(waypoint.id, waypoint);
        }

        Debug.Log(
            $"[Pr1Route] 총 {waypointDic.Count}개의 Waypoint가 등록되었습니다.");
    }


    // ==========================================
    // 가장 가까운 Waypoint 검색
    // ==========================================

    public int GetNearestWaypointID(Vector3 position)
    {
        if (waypointDic.Count == 0)
        {
            Debug.LogError(
                "[Pr1Route] 등록된 Waypoint가 없습니다.");

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


        // 전체 Waypoint 초기화
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


            // 목적지 도착
            if (current == end)
                break;


            // 더 이상 이동할 수 없는 경우
            if (distance[current] == float.MaxValue)
                break;


            if (current.neighbors == null)
                continue;


            // 이웃 Waypoint 확인
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
                        neighbor.transform.position
                    );


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


        // 출발지부터 연결된 경로인지 확인
        if (path.Count == 0 || path[0] != start)
        {
            Debug.LogError(
                $"[Pr1Route] 경로를 찾을 수 없습니다. {start.id} -> {end.id}");

            return null;
        }


        Debug.Log(
            $"[Pr1Route] 경로 탐색 결과: {string.Join(" -> ", path.ConvertAll(w => w.id.ToString()))}");

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
    // AGV Controller에 경로 전달
    // ==========================================

    public bool GetRoutes(
        Pr1Controller controller,
        int startID,
        int endID)
    {
        if (controller == null)
        {
            Debug.LogError(
                "[Pr1Route] Pr1Controller가 연결되지 않았습니다.");

            return false;
        }

        if (waypointDic.Count == 0)
        {
            Debug.LogError(
                "[Pr1Route] 등록된 Waypoint가 없습니다.");

            return false;
        }


        // 출발지 검색
        if (!waypointDic.TryGetValue(startID, out WayPoint start))
        {
            Debug.LogError(
                $"[Pr1Route] 출발 Waypoint를 찾을 수 없습니다. ID: {startID}");

            return false;
        }


        // 목적지 검색
        if (!waypointDic.TryGetValue(endID, out WayPoint end))
        {
            Debug.LogError(
                $"[Pr1Route] 목적지 Waypoint를 찾을 수 없습니다. ID: {endID}");

            return false;
        }


        Debug.Log(
            $"[Pr1Route] 경로 탐색 시작: {startID} -> {endID}");


        // 최단 경로 탐색
        List<WayPoint> path = FindShortestPath(start, end);

        if (path == null || path.Count == 0)
        {
            Debug.LogError(
                "[Pr1Route] 경로 탐색에 실패했습니다.");

            return false;
        }


        // Controller에 경로 전달
        controller.SetRoute(path);

        Debug.Log(
            "[Pr1Route] 경로 탐색 및 전달 완료");

        return true;
    }
}
