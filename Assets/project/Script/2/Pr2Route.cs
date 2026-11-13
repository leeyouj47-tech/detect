
using System.Collections.Generic;
using UnityEngine;

public class Pr2Route : MonoBehaviour
{
    [Header("전체 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    private Dictionary<int, WayPoint> waypointDic =
        new Dictionary<int, WayPoint>();

    public void InitializeWaypoints()
    {
        waypointDic.Clear();

        if (allWaypoints == null || allWaypoints.Length == 0)
        {
            Debug.LogError($"[{name}] Waypoint가 없습니다.");
            return;
        }

        foreach (WayPoint waypoint in allWaypoints)
        {
            if (waypoint == null)
                continue;

            if (waypointDic.ContainsKey(waypoint.id))
            {
                Debug.LogWarning(
                    $"[{name}] 중복 ID: {waypoint.id}");
                continue;
            }

            waypointDic.Add(waypoint.id, waypoint);
        }

        Debug.Log(
            $"[{name}] 총 {waypointDic.Count}개 Waypoint 등록 완료");
    }

    public int GetNearestWaypointID(Vector3 position)
    {
        if (waypointDic.Count == 0)
            return -1;

        int foundID = -1;
        float minDistance = float.MaxValue;

        foreach (var pair in waypointDic)
        {
            if (pair.Value == null)
                continue;

            float distance =
                (pair.Value.transform.position - position).sqrMagnitude;

            if (distance < minDistance)
            {
                minDistance = distance;
                foundID = pair.Key;
            }
        }

        return foundID;
    }

    private List<WayPoint> FindShortestPath(
        WayPoint start, WayPoint end)
    {
        if (start == null || end == null)
            return null;

        Dictionary<WayPoint, float> distance =
            new Dictionary<WayPoint, float>();

        Dictionary<WayPoint, WayPoint> previous =
            new Dictionary<WayPoint, WayPoint>();

        List<WayPoint> unvisited =
            new List<WayPoint>();

        foreach (WayPoint waypoint in waypointDic.Values)
        {
            distance[waypoint] = float.MaxValue;
            previous[waypoint] = null;
            unvisited.Add(waypoint);
        }

        distance[start] = 0f;

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
                if (neighbor == null ||
                    !distance.ContainsKey(neighbor) ||
                    !unvisited.Contains(neighbor))
                    continue;

                float alt = distance[current] +
                    Vector3.Distance(
                        current.transform.position,
                        neighbor.transform.position);

                if (alt < distance[neighbor])
                {
                    distance[neighbor] = alt;
                    previous[neighbor] = current;
                }
            }
        }

        List<WayPoint> path = new List<WayPoint>();
        WayPoint point = end;

        while (point != null)
        {
            path.Insert(0, point);
            point = previous[point];
        }

        if (path.Count == 0 || path[0] != start)
        {
            Debug.LogError(
                $"[{name}] 경로 탐색 실패: {start.id} -> {end.id}");
            return null;
        }

        Debug.Log(
            $"[{name}] 경로: " +
            string.Join(" -> ",
                path.ConvertAll(w => w.id.ToString())));

        return path;
    }

    private void Awake()
    {
        InitializeWaypoints();
    }

    public bool GetRoutes(
        Pr2Controller controller,
        int startID,
        int endID)
    {
        if (controller == null || waypointDic.Count == 0)
            return false;

        if (!waypointDic.TryGetValue(startID, out WayPoint start))
            return false;

        if (!waypointDic.TryGetValue(endID, out WayPoint end))
            return false;

        List<WayPoint> path =
            FindShortestPath(start, end);

        if (path == null || path.Count == 0)
            return false;

        controller.SetRoute(path);

        Debug.Log($"[{name}] 경로 전달 완료");

        return true;
    }
}
