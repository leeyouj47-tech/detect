using System.Collections.Generic;
using UnityEngine;

public class RailRoute : MonoBehaviour
{
    [Header("AGV / Trolley 레일 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    private Dictionary<int, WayPoint> waypointDic =
        new Dictionary<int, WayPoint>();

    public void InitializeWaypoints()
    {{
        waypointDic.Clear();

        if (allWaypoints == null || allWaypoints.Length == 0)
        {{
            Debug.LogError("[RailRoute] 등록된 Waypoint가 없습니다.");
            return;
        }}

        foreach (WayPoint waypoint in allWaypoints)
        {{
            if (waypoint == null)
            {{
                Debug.LogWarning("[RailRoute] null Waypoint가 포함되어 있습니다.");
                continue;
            }}

            if (waypointDic.ContainsKey(waypoint.id))
            {{
                Debug.LogWarning("[RailRoute] 중복된 Waypoint ID: " + waypoint.id);
                continue;
            }}

            waypointDic.Add(waypoint.id, waypoint);
        }}

        Debug.Log("[RailRoute] 총 " + waypointDic.Count + "개의 Waypoint 등록 완료");
    }}

    public int GetNearestWaypointID(Vector3 position)
    {{
        if (waypointDic.Count == 0)
        {{
            Debug.LogError("[RailRoute] 등록된 Waypoint가 없습니다.");
            return -1;
        }}

        int foundID = -1;
        float minDistance = float.MaxValue;

        foreach (KeyValuePair<int, WayPoint> pair in waypointDic)
        {{
            if (pair.Value == null)
                continue;

            float distanceSqr =
                (pair.Value.transform.position - position).sqrMagnitude;

            if (distanceSqr < minDistance)
            {{
                minDistance = distanceSqr;
                foundID = pair.Key;
            }}
        }}

        return foundID;
    }}

    private List<WayPoint> FindShortestPath(WayPoint start, WayPoint end)
    {{
        if (start == null || end == null)
            return null;

        Dictionary<WayPoint, float> distance =
            new Dictionary<WayPoint, float>();

        Dictionary<WayPoint, WayPoint> previousPoints =
            new Dictionary<WayPoint, WayPoint>();

        List<WayPoint> unvisited = new List<WayPoint>();

        foreach (WayPoint waypoint in waypointDic.Values)
        {{
            distance[waypoint] = float.MaxValue;
            previousPoints[waypoint] = null;
            unvisited.Add(waypoint);
        }}

        distance[start] = 0f;

        while (unvisited.Count > 0)
        {{
            unvisited.Sort(
                delegate(WayPoint a, WayPoint b)
                {{
                    return distance[a].CompareTo(distance[b]);
                }});

            WayPoint current = unvisited[0];
            unvisited.RemoveAt(0);

            if (current == end)
                break;

            if (distance[current] == float.MaxValue)
                break;

            if (current.neighbors == null)
                continue;

            foreach (WayPoint neighbor in current.neighbors)
            {{
                if (neighbor == null ||
                    !distance.ContainsKey(neighbor) ||
                    !unvisited.Contains(neighbor))
                    continue;

                float alt = distance[current] +
                    Vector3.Distance(
                        current.transform.position,
                        neighbor.transform.position);

                if (alt < distance[neighbor])
                {{
                    distance[neighbor] = alt;
                    previousPoints[neighbor] = current;
                }}
            }}
        }}

        List<WayPoint> path = new List<WayPoint>();
        WayPoint currentPoint = end;

        while (currentPoint != null)
        {{
            path.Insert(0, currentPoint);
            currentPoint = previousPoints[currentPoint];
        }}

        if (path.Count == 0 || path[0] != start)
        {{
            Debug.LogError("[RailRoute] 경로 탐색 실패: " + start.id + " -> " + end.id);
            return null;
        }}

        Debug.Log("[RailRoute] 경로: " +
            string.Join(" -> ", path.ConvertAll(delegate(WayPoint w) {{ return w.id.ToString(); }})));

        return path;
    }}

    private void Awake()
    {{
        InitializeWaypoints();
    }}

    public bool GetRoutes(
        Pr2Controller controller,
        Pr2Vehicle vehicle,
        int startID,
        int endID)
    {
        if (controller == null)
        {
            Debug.LogError("[RailRoute] Pr2Controller가 연결되지 않았습니다.");
            return false;
        }

        if (vehicle != Pr2Vehicle.AGV && vehicle != Pr2Vehicle.Trolley)
        {
            Debug.LogError("[RailRoute] AGV 또는 Trolley만 사용할 수 있습니다.");
            return false;
        }

        return BuildAndSendRoute(controller, vehicle, startID, endID);
    }

    private bool BuildAndSendRoute(
        Pr2Controller controller,
        Pr2Vehicle vehicle,
        int startID,
        int endID)
    {
        WayPoint start;
        WayPoint end;

        if (!waypointDic.TryGetValue(startID, out start))
        {
            Debug.LogError("[RailRoute] 출발 Waypoint를 찾을 수 없습니다: " + startID);
            return false;
        }

        if (!waypointDic.TryGetValue(endID, out end))
        {
            Debug.LogError("[RailRoute] 목적지 Waypoint를 찾을 수 없습니다: " + endID);
            return false;
        }

        List<WayPoint> path = FindShortestPath(start, end);
        if (path == null || path.Count == 0)
            return false;

        controller.SetRoute(vehicle, path);
        return true;
    }
}