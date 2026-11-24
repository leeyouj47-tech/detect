
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Pr2Route : MonoBehaviour
{
    [Header("전체 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float rotateSpeed = 180f;
    public float arrivalDistance = 0.05f;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();

    private Dictionary<int, WayPoint> waypointDic =
        new Dictionary<int, WayPoint>();

    private List<WayPoint> routes = new List<WayPoint>();

    private int routeIndex = 0;

    private bool canMove = false;
    private bool isRotating = false;

    // ==========================================
    // 초기화
    // ==========================================

    private void Awake()
    {
        InitializeWaypoints();
    }

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

    // ==========================================
    // 가장 가까운 Waypoint 찾기
    // ==========================================

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

    // ==========================================
    // 최단 경로 탐색 (다익스트라)
    // ==========================================

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
            if (waypoint == null)
                continue;

            distance[waypoint] = float.MaxValue;
            previous[waypoint] = null;
            unvisited.Add(waypoint);
        }

        if (!distance.ContainsKey(start) ||
            !distance.ContainsKey(end))
            return null;

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

    // ==========================================
    // 목적지 ID로 이동 명령
    // ==========================================

    public bool Go(int destinationID)
    {
        if (canMove)
        {
            Debug.LogWarning($"[{name}] 이미 이동 중입니다.");
            return false;
        }

        if (!waypointDic.ContainsKey(destinationID))
        {
            Debug.LogError(
                $"[{name}] 목적지 Waypoint ID가 없습니다: {destinationID}");
            return false;
        }

        int startID = GetNearestWaypointID(transform.position);

        if (startID == -1)
            return false;

        WayPoint start = waypointDic[startID];
        WayPoint end = waypointDic[destinationID];

        List<WayPoint> path = FindShortestPath(start, end);

        if (path == null || path.Count == 0)
            return false;

        SetRoute(path);

        Debug.Log(
            $"[{name}] 이동 시작: {startID} -> {destinationID}");

        return true;
    }

    // ==========================================
    // 지정된 경로 설정
    // ==========================================

    public void SetRoute(List<WayPoint> newRoutes)
    {
        if (newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError($"[{name}] 이동 경로가 비어 있습니다.");
            return;
        }

        routes.Clear();

        foreach (WayPoint waypoint in newRoutes)
        {
            if (waypoint != null)
                routes.Add(waypoint);
        }

        if (routes.Count == 0)
            return;

        // 현재 위치와 가장 가까운 시작 Waypoint는 건너뜀
        routeIndex = 1;

        if (routes.Count == 1)
        {
            CompleteMove();
            return;
        }

        canMove = true;
        isRotating = true;
    }

    // ==========================================
    // 경로 이동 처리
    // ==========================================

    private void FixedUpdate()
    {
        if (!canMove)
            return;

        if (routeIndex >= routes.Count)
        {
            CompleteMove();
            return;
        }

        WayPoint targetWaypoint = routes[routeIndex];

        if (targetWaypoint == null)
        {
            Debug.LogError($"[{name}] 이동할 Waypoint가 없습니다.");
            StopMovement();
            return;
        }

        Vector3 targetPosition =
            targetWaypoint.transform.position;

        Vector3 direction =
            targetPosition - transform.position;

        // 도착 여부 확인
        if (direction.sqrMagnitude <=
            arrivalDistance * arrivalDistance)
        {
            transform.position = targetPosition;

            routeIndex++;

            if (routeIndex >= routes.Count)
            {
                CompleteMove();
            }

            return;
        }

        // 이동 방향 회전
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction.normalized);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime);
        }

        // 목적지 방향으로 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime);
    }

    // ==========================================
    // 이동 완료
    // ==========================================

    private void CompleteMove()
    {
        canMove = false;
        isRotating = false;
        routeIndex = 0;

        routes.Clear();

        Debug.Log($"[{name}] 이동 완료");

        onCompleteMove.Invoke();
    }

    // ==========================================
    // 이동 중지
    // ==========================================

    public void StopMovement()
    {
        canMove = false;
        isRotating = false;
        routeIndex = 0;

        routes.Clear();

        Debug.Log($"[{name}] 이동 중지");
    }

    // ==========================================
    // 이동 상태 확인
    // ==========================================

    public bool IsMoving()
    {
        return canMove || isRotating;
    }

    // ==========================================
    // 시작 ID와 목적지 ID로 경로 설정
    // ==========================================

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

        SetRoute(path);

        Debug.Log($"[{name}] 경로 전달 및 이동 시작");

        return true;
    }
}
