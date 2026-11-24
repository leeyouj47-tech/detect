
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 공통 Waypoint 이동 클래스
// ==========================================
public abstract class Pr2RouteBase : MonoBehaviour
{
    [Header("전체 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    [Header("이동 설정")]
    public float speed = 1f;
    public float rotateSpeed = 90f;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();

    private Dictionary<int, WayPoint> waypointDic =
        new Dictionary<int, WayPoint>();

    private List<WayPoint> routes;
    private int routeIndex;
    private bool canMove;
    private bool isRotating;

    protected virtual void Awake()
    {
        InitializeWaypoints();
    }

    private void InitializeWaypoints()
    {
        waypointDic.Clear();

        if (allWaypoints == null || allWaypoints.Length == 0)
        {
            Debug.LogError($"[{name}] Waypoint가 등록되지 않았습니다.");
            return;
        }

        foreach (WayPoint waypoint in allWaypoints)
        {
            if (waypoint == null)
                continue;

            if (waypointDic.ContainsKey(waypoint.id))
            {
                Debug.LogWarning(
                    $"[{name}] 중복된 Waypoint ID: {waypoint.id}");
                continue;
            }

            waypointDic.Add(waypoint.id, waypoint);
        }

        Debug.Log($"[{name}] Waypoint 등록 완료: {waypointDic.Count}개");
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

        return path;
    }

    public bool Go(int destinationID)
    {
        if (!waypointDic.TryGetValue(
            destinationID, out WayPoint destination))
        {
            Debug.LogError(
                $"[{name}] 목적지 Waypoint를 찾을 수 없습니다: {destinationID}");
            return false;
        }

        int startID = GetNearestWaypointID(transform.position);

        if (!waypointDic.TryGetValue(startID, out WayPoint start))
        {
            Debug.LogError($"[{name}] 출발 Waypoint를 찾을 수 없습니다.");
            return false;
        }

        List<WayPoint> path = FindShortestPath(start, destination);

        if (path == null || path.Count == 0)
            return false;

        SetRoute(path);
        return true;
    }

    public void SetRoute(List<WayPoint> newRoutes)
    {
        if (newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError($"[{name}] 경로가 비어 있습니다.");
            StopMovement();
            return;
        }

        routes = new List<WayPoint>(newRoutes);
        routeIndex = 0;
        isRotating = true;
        canMove = true;

        Debug.Log(
            $"[{name}] 이동 시작: {routes[0].id} -> " +
            $"{routes[routes.Count - 1].id}");
    }

    private void FixedUpdate()
    {
        if (!canMove || routes == null || routes.Count == 0)
            return;

        if (routeIndex >= routes.Count)
            return;

        WayPoint targetWaypoint = routes[routeIndex];

        if (targetWaypoint == null)
        {
            Debug.LogError($"[{name}] Waypoint가 null입니다.");
            StopMovement();
            return;
        }

        Vector3 targetPosition =
            targetWaypoint.transform.position;

        Vector3 toward = targetPosition - transform.position;
        toward.y = 0f;

        if (isRotating)
        {
            if (toward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(toward);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.fixedDeltaTime);

                if (Quaternion.Angle(
                    transform.rotation, targetRotation) < 0.1f)
                {
                    transform.rotation = targetRotation;
                    isRotating = false;
                }
            }
            else
            {
                isRotating = false;
            }

            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.fixedDeltaTime);

        if (Vector3.Distance(
            transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;

            Debug.Log(
                $"[{name}] Waypoint {targetWaypoint.id} 도착");

            routeIndex++;
            isRotating = true;

            if (routeIndex >= routes.Count)
            {
                StopMovement();

                Debug.Log($"[{name}] 목적지 도착 완료");
                onCompleteMove?.Invoke();
            }
        }
    }

    public void StopMovement()
    {
        canMove = false;
        routes = null;
        routeIndex = 0;
        isRotating = false;
    }

    public bool IsMoving()
    {
        return canMove;
    }
}

// ==========================================
// Project 2 전체 컨트롤러
// ==========================================
public class Pr2Controller : MonoBehaviour
{
    [Header("장치별 Waypoint Controller")]
    public AGVRoute agv;
    public LiftRoute lift;
    public RailRoute rail;
    public ICCURoute iccu;

    [Header("Unity 작업 이벤트")]
    public UnityEvent onBatteryCoupling;
    public UnityEvent onAGVLeave;
    public UnityEvent onRobotArm;
    public UnityEvent onICCUInspection;
    public UnityEvent onChassisInspection;

    [Header("전체 정지 이벤트")]
    public UnityEvent onProcessStop;

    public bool MoveAGV(int waypointID)
    {
        return agv != null && agv.Go(waypointID);
    }

    public bool MoveLift(int waypointID)
    {
        return lift != null && lift.Go(waypointID);
    }

    public bool MoveRail(int waypointID)
    {
        return rail != null && rail.Go(waypointID);
    }

    public bool MoveICCU(int waypointID)
    {
        return iccu != null && iccu.Go(waypointID);
    }

    public void PlayBatteryCoupling()
    {
        onBatteryCoupling?.Invoke();
    }

    public void PlayAGVLeave()
    {
        onAGVLeave?.Invoke();
    }

    public void RunRobotArm()
    {
        onRobotArm?.Invoke();
    }

    public void StartICCUInspection()
    {
        onICCUInspection?.Invoke();
    }

    public void StartChassisInspection()
    {
        onChassisInspection?.Invoke();
    }

    public void StopAllDevices()
    {
        if (agv != null) agv.StopMovement();
        if (lift != null) lift.StopMovement();
        if (rail != null) rail.StopMovement();
        if (iccu != null) iccu.StopMovement();

        onProcessStop?.Invoke();

        Debug.LogWarning("[Pr2Controller] 전체 장치 정지");
    }
}
