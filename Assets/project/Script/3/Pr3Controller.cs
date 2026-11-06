
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Pr3Controller : MonoBehaviour
{
    [Header("Route 설정")]
    public List<WayPoint> allWaypoints = new List<WayPoint>();

    private List<WayPoint> routes;
    private int routeIndex;

    [Header("AGV 이동 설정")]
    public float speed = 1f;
    public float rotateSpeed = 90f;

    private bool isRotating;
    private bool canMove;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();

    private void Start()
    {
        // 씬 실행만으로 AGV가 움직이지 않도록 초기 정지
        canMove = false;
        isRotating = false;
        routeIndex = 0;
        routes = null;
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
            Debug.LogError("[Pr3Controller] Waypoint가 null입니다.");
            StopMovement();
            return;
        }

        Vector3 targetPosition = targetWaypoint.transform.position;
        Vector3 toward = targetPosition - transform.position;
        toward.y = 0f;

        // 1. 회전
        if (isRotating)
        {
            if (toward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toward);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.fixedDeltaTime
                );

                if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
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

        // 2. 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.fixedDeltaTime
        );

        // 3. 도착 확인
        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;

            Debug.Log(
                $"[Pr3Controller] Waypoint {targetWaypoint.id} 도착"
            );

            routeIndex++;
            isRotating = true;

            // 4. 최종 목적지 도착
            if (routeIndex >= routes.Count)
            {
                StopMovement();

                Debug.Log("[Pr3Controller] 최종 목적지 도착");

                onCompleteMove?.Invoke();
            }
        }
    }

    public bool Go(int destinationID)
    {
        if (canMove)
        {
            Debug.LogWarning("[Pr3Controller] 이미 이동 중입니다.");
            return false;
        }

        WayPoint startWaypoint = GetNearestWaypoint();

        if (startWaypoint == null)
        {
            Debug.LogError("[Pr3Controller] 출발 Waypoint를 찾지 못했습니다.");
            return false;
        }

        WayPoint destination = allWaypoints.Find(
            wp => wp != null && wp.id == destinationID
        );

        if (destination == null)
        {
            Debug.LogError(
                $"[Pr3Controller] 목적지 Waypoint {destinationID}를 찾지 못했습니다."
            );
            return false;
        }

        List<WayPoint> newRoute = FindRoute(startWaypoint, destination);

        if (newRoute == null || newRoute.Count == 0)
        {
            Debug.LogError(
                $"[Pr3Controller] 경로 탐색 실패: {startWaypoint.id} -> {destinationID}"
            );
            return false;
        }

        routes = newRoute;
        routeIndex = 0;
        isRotating = true;
        canMove = true;

        Debug.Log(
            $"[Pr3Controller] 이동 시작: {startWaypoint.id} -> {destinationID}"
        );

        return true;
    }

    private WayPoint GetNearestWaypoint()
    {
        WayPoint nearest = null;
        float minDistance = float.MaxValue;

        foreach (WayPoint waypoint in allWaypoints)
        {
            if (waypoint == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                waypoint.transform.position
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = waypoint;
            }
        }

        return nearest;
    }

    private List<WayPoint> FindRoute(WayPoint start, WayPoint destination)
    {
        Dictionary<WayPoint, float> distances =
            new Dictionary<WayPoint, float>();

        Dictionary<WayPoint, WayPoint> previous =
            new Dictionary<WayPoint, WayPoint>();

        List<WayPoint> unvisited = new List<WayPoint>();

        foreach (WayPoint waypoint in allWaypoints)
        {
            if (waypoint == null)
                continue;

            distances[waypoint] = float.MaxValue;
            unvisited.Add(waypoint);
        }

        if (!distances.ContainsKey(start) ||
            !distances.ContainsKey(destination))
            return null;

        distances[start] = 0f;

        while (unvisited.Count > 0)
        {
            WayPoint current = null;
            float minDistance = float.MaxValue;

            foreach (WayPoint waypoint in unvisited)
            {
                if (distances[waypoint] < minDistance)
                {
                    minDistance = distances[waypoint];
                    current = waypoint;
                }
            }

            if (current == null || minDistance == float.MaxValue)
                break;

            unvisited.Remove(current);

            if (current == destination)
                break;

            if (current.neighbors == null)
                continue;

            foreach (WayPoint neighbor in current.neighbors)
            {
                if (neighbor == null || !distances.ContainsKey(neighbor))
                    continue;

                float cost = Vector3.Distance(
                    current.transform.position,
                    neighbor.transform.position
                );

                float newDistance = distances[current] + cost;

                if (newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                    previous[neighbor] = current;
                }
            }
        }

        if (start == destination)
            return new List<WayPoint> { destination };

        if (!previous.ContainsKey(destination))
            return null;

        List<WayPoint> path = new List<WayPoint>();
        WayPoint node = destination;

        while (node != start)
        {
            path.Add(node);

            if (!previous.ContainsKey(node))
                return null;

            node = previous[node];
        }

        path.Reverse();
        return path;
    }

    private void StopMovement()
    {
        canMove = false;
        isRotating = false;
        routeIndex = 0;
        routes = null;
    }
}
