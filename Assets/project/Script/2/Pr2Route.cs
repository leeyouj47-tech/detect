using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ==========================================
// Waypoint별 도착 이벤트 (부착/분리 등 연출용)
// ==========================================
[Serializable]
public class WaypointArrivalEvent
{
    public int waypointID;
    public UnityEvent onArrived = new UnityEvent();
}

// ==========================================
// 2공정 공통 Waypoint 이동
// AGVRoute / LiftRoute / RailRoute / ICCURoute 가 상속
// ==========================================
public class Pr2Route : MonoBehaviour
{
    [Header("이동시킬 장비 (비우면 이 오브젝트)")]
    [Tooltip("실제로 움직일 장비. Waypoint들의 부모를 움직이면 Waypoint도 같이 움직여서 도착하지 못하므로 반드시 장비를 지정하세요.")]
    public Transform moveTarget;

    [Header("전체 Waypoint 등록")]
    public WayPoint[] allWaypoints;

    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float rotateSpeed = 180f;
    public float arrivalDistance = 0.05f;

    [Tooltip("true: 이동 방향(수평)으로 회전 / false: 회전 없이 평행이동")]
    public bool rotateToDirection = false;

    [Tooltip("true: neighbors 연결을 따라 최단경로 이동 / false: 목적지로 바로 직선 이동")]
    public bool useWaypointGraph = false;

    [Tooltip("시작 시 장비와 가장 가까운 Waypoint 사이의 위치 차이를 유지한 채 이동")]
    public bool keepStartOffset = true;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();

    [Header("Waypoint별 도착 이벤트")]
    public WaypointArrivalEvent[] arrivalEvents;

    /// <summary>최종 목적지 도착 시 (목적지 ID)</summary>
    public event Action<int> Arrived;

    public int CurrentWaypointID { get; private set; } = -1;

    private readonly Dictionary<int, WayPoint> waypointDic =
        new Dictionary<int, WayPoint>();

    private readonly List<WayPoint> routes = new List<WayPoint>();

    private int routeIndex;
    private int destinationID = -1;
    private bool canMove;
    private Vector3 offset;

    private Transform Target => moveTarget != null ? moveTarget : transform;

    // ==========================================
    // 초기화
    // ==========================================

    private void Awake()
    {
        InitializeWaypoints();

        int nearestID = GetNearestWaypointID(Target.position);

        if (nearestID >= 0)
        {
            CurrentWaypointID = nearestID;

            if (keepStartOffset)
                offset = Target.position - waypointDic[nearestID].transform.position;
        }
    }

    // 비활성 컴포넌트는 Start가 호출되지 않으므로 실제 사용하는 Route만 검사
    private void Start()
    {
        if (moveTarget != null)
            return;

        foreach (WayPoint waypoint in waypointDic.Values)
        {
            if (waypoint.transform.IsChildOf(transform))
            {
                Debug.LogError(
                    $"[{name}] Move Target이 비어 있고 Waypoint가 이 오브젝트의 자식입니다. " +
                    "이대로는 Waypoint가 같이 움직여 도착하지 못합니다. Move Target에 장비를 지정하세요.");
                break;
            }
        }
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
                Debug.LogWarning($"[{name}] 중복 ID: {waypoint.id}");
                continue;
            }

            waypointDic.Add(waypoint.id, waypoint);
        }

        Debug.Log($"[{name}] 총 {waypointDic.Count}개 Waypoint 등록 완료");
    }

    // ==========================================
    // 가장 가까운 Waypoint 찾기
    // ==========================================

    public int GetNearestWaypointID(Vector3 position)
    {
        int foundID = -1;
        float minDistance = float.MaxValue;

        foreach (var pair in waypointDic)
        {
            if (pair.Value == null)
                continue;

            float distance =
                (pair.Value.transform.position + offset - position).sqrMagnitude;

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

    private List<WayPoint> FindShortestPath(WayPoint start, WayPoint end)
    {
        if (start == null || end == null)
            return null;

        var distance = new Dictionary<WayPoint, float>();
        var previous = new Dictionary<WayPoint, WayPoint>();
        var unvisited = new List<WayPoint>();

        foreach (WayPoint waypoint in waypointDic.Values)
        {
            if (waypoint == null)
                continue;

            distance[waypoint] = float.MaxValue;
            previous[waypoint] = null;
            unvisited.Add(waypoint);
        }

        if (!distance.ContainsKey(start) || !distance.ContainsKey(end))
            return null;

        distance[start] = 0f;

        while (unvisited.Count > 0)
        {
            unvisited.Sort((a, b) => distance[a].CompareTo(distance[b]));

            WayPoint current = unvisited[0];
            unvisited.RemoveAt(0);

            if (current == end || distance[current] == float.MaxValue)
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
                    Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (alt < distance[neighbor])
                {
                    distance[neighbor] = alt;
                    previous[neighbor] = current;
                }
            }
        }

        var path = new List<WayPoint>();

        for (WayPoint point = end; point != null; point = previous[point])
            path.Insert(0, point);

        if (path.Count == 0 || path[0] != start)
            return null;

        return path;
    }

    // ==========================================
    // 목적지 ID로 이동 시작
    // 도착 처리는 항상 FixedUpdate에서 일어남 (호출 직후 동기 완료 없음)
    // ==========================================

    public bool Go(int destinationID)
    {
        if (canMove)
        {
            Debug.LogWarning($"[{name}] 이미 이동 중입니다.");
            return false;
        }

        if (!waypointDic.TryGetValue(destinationID, out WayPoint end))
        {
            Debug.LogError($"[{name}] 목적지 Waypoint ID가 없습니다: {destinationID}");
            return false;
        }

        var path = new List<WayPoint>();

        if (useWaypointGraph)
        {
            int startID = GetNearestWaypointID(Target.position);

            if (startID >= 0)
                path = FindShortestPath(waypointDic[startID], end);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning(
                    $"[{name}] 경로 탐색 실패({startID} -> {destinationID}). 목적지로 직선 이동합니다.");
                path = new List<WayPoint>();
            }
        }

        if (path.Count == 0)
            path.Add(end);

        SetRoute(path, destinationID);

        Debug.Log($"[{name}] 이동 시작 -> WP{destinationID} " +
                  $"({string.Join(" -> ", path.ConvertAll(w => w.id.ToString()))})");

        return true;
    }

    private void SetRoute(List<WayPoint> newRoutes, int destination)
    {
        routes.Clear();

        foreach (WayPoint waypoint in newRoutes)
        {
            if (waypoint != null)
                routes.Add(waypoint);
        }

        routeIndex = 0;
        destinationID = destination;
        canMove = routes.Count > 0;
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

        Transform target = Target;
        Vector3 targetPosition = targetWaypoint.transform.position + offset;
        Vector3 direction = targetPosition - target.position;

        // 도착 확인
        if (direction.sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            target.position = targetPosition;
            CurrentWaypointID = targetWaypoint.id;
            InvokeArrivalEvent(targetWaypoint.id);

            routeIndex++;

            if (routeIndex >= routes.Count)
                CompleteMove();

            return;
        }

        // 이동 방향 회전 (수평 방향만)
        if (rotateToDirection)
        {
            Vector3 flat = new Vector3(direction.x, 0f, direction.z);

            if (flat.sqrMagnitude > 0.0001f)
            {
                target.rotation = Quaternion.RotateTowards(
                    target.rotation,
                    Quaternion.LookRotation(flat.normalized),
                    rotateSpeed * Time.fixedDeltaTime);
            }
        }

        target.position = Vector3.MoveTowards(
            target.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime);
    }

    private void InvokeArrivalEvent(int waypointID)
    {
        if (arrivalEvents == null)
            return;

        foreach (WaypointArrivalEvent arrivalEvent in arrivalEvents)
        {
            if (arrivalEvent != null && arrivalEvent.waypointID == waypointID)
                arrivalEvent.onArrived?.Invoke();
        }
    }

    // ==========================================
    // 이동 완료
    // ==========================================

    private void CompleteMove()
    {
        int arrivedID = destinationID;

        canMove = false;
        routeIndex = 0;
        destinationID = -1;
        routes.Clear();

        Debug.Log($"[{name}] 이동 완료: WP{arrivedID}");

        onCompleteMove?.Invoke();
        Arrived?.Invoke(arrivedID);
    }

    // ==========================================
    // 이동 정지
    // ==========================================

    public void StopMovement()
    {
        if (!canMove)
            return;

        canMove = false;
        routeIndex = 0;
        destinationID = -1;
        routes.Clear();

        Debug.Log($"[{name}] 이동 정지");
    }

    public bool IsMoving()
    {
        return canMove;
    }

    // ==========================================
    // 이동 대상 교체 (예: AGV 빠진 뒤 차체만 이동)
    // 새 대상과 가장 가까운 Waypoint 사이의 위치 차이를 유지
    // ==========================================

    public void SetMoveTarget(Transform newTarget)
    {
        if (newTarget == null)
            return;

        StopMovement();
        moveTarget = newTarget;

        offset = Vector3.zero;
        int nearestID = GetNearestWaypointID(newTarget.position);

        if (nearestID >= 0)
        {
            CurrentWaypointID = nearestID;

            if (keepStartOffset)
                offset = newTarget.position - waypointDic[nearestID].transform.position;
        }
    }
}
