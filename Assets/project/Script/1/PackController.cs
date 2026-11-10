
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PackController : MonoBehaviour
{
    [Header("Route 설정")]
    public PackRoute manager;

    public List<WayPoint> routes;

    private int routeIndex;

    [Header("AGV 이동 설정")]
    public float speed = 1f;
    public float rotateSpeed = 90f;

    private bool isRotating = false;
    private bool canMove = false;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();
    private int currentRouteIndex;


    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (manager == null)
        {
            Debug.LogError(
                "[PackController] PackRoute가 연결되지 않았습니다.");
        }

        canMove = false;
        isRotating = false;
        routeIndex = 0;
    }


    // ==========================================
    // AGV 이동 및 회전
    // ==========================================

    private void FixedUpdate()
    {
        if (routes == null || routes.Count == 0)
            return;

        WayPoint target = routes[currentRouteIndex];

        Vector3 targetPosition = target.transform.position;

        // Y축 높이는 현재 위치 유지
        targetPosition.y = transform.position.y;

        // 회전 없이 목표 Waypoint 방향으로 직선 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.fixedDeltaTime
        );

        // 도착 확인
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            currentRouteIndex++;

            if (currentRouteIndex >= routes.Count)
            {
                routes.Clear();
                currentRouteIndex = 0;

                onCompleteMove?.Invoke();
            }
        }
    }

    // ==========================================
    // 이동 정지
    // ==========================================

    private void StopMovement()
    {
        canMove = false;
        routes = null;
        routeIndex = 0;
        isRotating = false;
    }


    // ==========================================
    // 새로운 경로 설정
    // ==========================================

    public void SetRoute(List<WayPoint> newRoutes)
    {
        Debug.Log("[PackController] 호출됨 경로개수: " + (newRoutes == null ? 0 : newRoutes.Count));
        if (newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError(
                "[PackController] 전달받은 경로가 비어 있습니다.");

            StopMovement();
            return;
        }

        routes = new List<WayPoint>(newRoutes);

        routeIndex = 0;
        isRotating = true;

        canMove = true;

        Debug.Log(
            $"[PackController] 경로 설정 완료 / 경유 Waypoint 수: {routes.Count}");

        Debug.Log(
            $"[PackController] 첫 번째 이동 Waypoint: {routes[0].id}");

        Debug.Log(
            $"[PackController] 최종 목적지 Waypoint: {routes[routes.Count - 1].id}");
    }


    // ==========================================
    // 목적지 이동 명령
    // ==========================================

    public bool Go(int destinationID)
    {
        Debug.Log("[PackController] Go 호출 목적지" + destinationID);
        Debug.Log( $"[PackController] 이동 명령 / 목적지 ID: {destinationID}");

        if (manager == null)
        {
            Debug.LogError(
                "[PackController] PackRoute가 연결되지 않았습니다.");

            return false;
        }

        int startID =
            manager.GetNearestWaypointID(transform.position);

        if (startID < 0)
        {
            Debug.LogError(
                "[PackController] 출발 Waypoint를 찾지 못했습니다.");

            return false;
        }

        Debug.Log(
            $"[PackController] 출발지: {startID} / 목적지: {destinationID}");

        return manager.GetRoutes(this, startID, destinationID);
    }
}
