
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PackController : MonoBehaviour
{
    [Header("Route 설정")]
    public PackRoute manager;

    public List<WayPoint> routes;

    private int currentRouteIndex = 0;

    [Header("AGV 이동 설정")]
    public float speed = 1f;
    public float rotateSpeed = 90f;

    private bool canMove = false;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();


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
        currentRouteIndex = 0;
    }


    // ==========================================
    // AGV 이동 처리
    // ==========================================

    private void FixedUpdate()
    {
        // 이동 명령이 없으면 정지
        if (!canMove)
            return;

        if (routes == null || routes.Count == 0)
        {
            StopMovement();
            return;
        }

        // 경로 범위 확인
        if (currentRouteIndex >= routes.Count)
        {
            CompleteMovement();
            return;
        }

        WayPoint target = routes[currentRouteIndex];

        if (target == null)
        {
            Debug.LogError(
                "[PackController] 이동할 Waypoint가 null입니다.");

            StopMovement();
            return;
        }

        Vector3 targetPosition =
            target.transform.position;

        // Y축 높이는 현재 위치 유지
        targetPosition.y = transform.position.y;

        // 목적지 방향으로 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.fixedDeltaTime
        );

        // 도착 확인
        if (Vector3.Distance(
            transform.position,
            targetPosition) < 0.05f)
        {
            transform.position = targetPosition;

            Debug.Log(
                $"[PackController] Waypoint {target.id} 도착");

            currentRouteIndex++;

            if (currentRouteIndex >= routes.Count)
            {
                CompleteMovement();
            }
        }
    }


    // ==========================================
    // 이동 정지
    // ==========================================

    private void StopMovement()
    {
        canMove = false;
        currentRouteIndex = 0;

        if (routes != null)
            routes.Clear();
    }


    // ==========================================
    // 이동 완료
    // ==========================================

    private void CompleteMovement()
    {
        Debug.Log("[PackController] 최종 목적지 도착");
        Debug.Log("[PackController] onCompleteMove 이벤트 발생");

        canMove = false;

        if (routes != null)
            routes.Clear();

        currentRouteIndex = 0;

        onCompleteMove?.Invoke();

        Debug.Log("[PackController] 도착 이벤트 호출 완료");
    }

    // ==========================================
    // 새로운 경로 설정
    // ==========================================

    public void SetRoute(List<WayPoint> newRoutes)
    {
        Debug.Log("[PackController] 경로 설정 호출");

        if (newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError(
                "[PackController] 전달받은 경로가 비어 있습니다.");

            StopMovement();
            return;
        }

        // 출발지와 목적지가 같은 경로인지 확인
        if (newRoutes.Count == 1 ||
            (newRoutes[0] != null &&
             newRoutes[newRoutes.Count - 1] != null &&
             newRoutes[0].id == newRoutes[newRoutes.Count - 1].id))
        {
            Debug.LogWarning(
                "[PackController] 출발지와 목적지가 같습니다. 이동 및 완료 이벤트를 무시합니다.");

            canMove = false;
            currentRouteIndex = 0;

            if (routes != null)
                routes.Clear();

            return;
        }

        // 이전 경로 초기화
        canMove = false;

        routes = new List<WayPoint>(newRoutes);

        // 출발 Waypoint는 이미 현재 위치이므로 건너뜀
        currentRouteIndex = 1;

        Debug.Log(
            $"[PackController] 출발 Waypoint: {routes[0].id}");

        Debug.Log(
            $"[PackController] 최종 목적지: {routes[routes.Count - 1].id}");

        Debug.Log(
            $"[PackController] 실제 첫 이동 목적지: " +
            (currentRouteIndex < routes.Count
                ? routes[currentRouteIndex].id.ToString()
                : "없음"));

        canMove = true;

        Debug.Log("[PackController] 이동 시작");
    }
    // ==========================================
    // 목적지 이동 명령
    // ==========================================

    public bool Go(int destinationID)
    {
        Debug.Log(
            $"[PackController] Go 호출 / 목적지: {destinationID}");

        if (manager == null)
        {
            Debug.LogError(
                "[PackController] PackRoute가 연결되지 않았습니다.");

            return false;
        }

        // 이동 중에는 새로운 경로 요청 방지
        if (canMove)
        {
            Debug.LogWarning(
                "[PackController] 현재 이동 중입니다.");

            return false;
        }

        // 현재 Pack 위치에서 가장 가까운 Waypoint 검색
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

        // 이미 목적지에 도착해 있다면 이동 명령 무시
        if (startID == destinationID)
        {
            Debug.LogWarning(
                $"[PackController] 이미 Waypoint {destinationID}에 있습니다. 이동 명령 무시");

            return false;
        }

        return manager.GetRoutes(
            this,
            startID,
            destinationID);
    }
}
