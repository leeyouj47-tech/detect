using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class _Pr3Controller : MonoBehaviour
{
    [Header("Route 설정")]
    public _Pr3Route manager;

    public List<WayPoint> routes;

    private int routeIndex;

    [Header("AGV 이동 설정")]
    public float speed = 1f;
    public float rotateSpeed = 90f;
    public bool lockRotation = false;

    private bool isRotating = false;

    // 이동 명령을 받아야 움직일 수 있음
    private bool canMove = false;

    [Header("자동 이동 설정")]
    [Tooltip("자동으로 방문할 목적지 Waypoint ID 목록 (순서대로 이동)")]
    public int[] autoDestinationIDs;

    [Tooltip("시작 시 자동 이동을 바로 시작합니다.")]
    public bool autoStart = true;

    [Tooltip("마지막 목적지에 도착하면 처음 목적지부터 반복합니다.")]
    public bool loop = true;

    [Tooltip("각 목적지 도착 후 다음 목적지로 출발하기 전 대기 시간(초)")]
    public float arriveWaitTime = 0f;

    // 자동 이동 상태
    private bool isAutoRunning = false;
    private int autoIndex = 0;
    private bool isAutoWaiting = false;
    private float autoWaitTimer = 0f;

    [Header("정지 설정")]
    [Tooltip("이 ID의 Waypoint에 도착하면 정지하고 대기합니다.")]
    public int[] stopWaypointIDs;

    [Tooltip("운반 도구. 이 오브젝트를 제외한 자식이 모두 없어지면 다시 이동합니다.")]
    public Transform carrier;

    // 정지 지점에서 재개 조건을 기다리는 중
    private bool isWaiting = false;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();

    [Header("정지 지점 도착 이벤트")]
    public UnityEvent onArriveStop = new UnityEvent();

    [Header("자동 이동 전체 완료 이벤트 (loop가 꺼져 있을 때)")]
    public UnityEvent onCompleteAutoRun = new UnityEvent();


    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (manager == null)
        {
            Debug.LogError(
                "[Pr3Controller] Pr3Route가 연결되지 않았습니다.");
        }

        if (stopWaypointIDs != null &&
            stopWaypointIDs.Length > 0 &&
            carrier == null)
        {
            Debug.LogError(
                "[Pr3Controller] 정지 지점이 설정되어 있지만 운반 도구(carrier)가 연결되지 않았습니다.");
        }

        canMove = false;
        isRotating = false;
        isWaiting = false;
        routeIndex = 0;

        // Pr3Route는 Awake에서 초기화되므로 Start에서 바로 사용 가능
        if (autoStart)
            StartAutoRun();
    }


    // ==========================================
    // AGV 이동 및 회전
    // ==========================================

    private void FixedUpdate()
    {
        // 목적지 도착 후 다음 목적지 출발 전 대기
        if (isAutoRunning && isAutoWaiting)
        {
            autoWaitTimer -= Time.fixedDeltaTime;

            if (autoWaitTimer <= 0f)
            {
                isAutoWaiting = false;
                GoNextAuto();
            }

            return;
        }

        // 이동 명령을 받기 전에는 정지
        if (!canMove)
            return;

        if (routes == null || routes.Count == 0)
        {
            canMove = false;
            return;
        }


        // 정지 지점 대기 중: 운반 도구만 남으면 재개
        if (isWaiting)
        {
            if (IsOnlyCarrierLeft())
            {
                isWaiting = false;

                Debug.Log(
                    "[Pr3Controller] 재개 조건 충족, 이동을 다시 시작합니다.");
            }

            return;
        }

        if (routeIndex >= routes.Count)
            return;

        if (routes[routeIndex] == null)
        {
            Debug.LogError(
                $"[Pr3Controller] Waypoint가 null입니다. Index: {routeIndex}");

            StopMovement();
            StopAutoRun();
            return;
        }

        Vector3 targetPosition =
            routes[routeIndex].transform.position;

        Vector3 toward = targetPosition - transform.position;
        toward.y = 0f;


        // 1. 회전 제어
        if (isRotating)
        {
            if (toward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(toward);

                transform.rotation =
                    Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotateSpeed * Time.fixedDeltaTime
                    );

                if (Quaternion.Angle(
                    transform.rotation,
                    targetRotation) < 0.1f)
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


        // 2. 이동 제어
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.fixedDeltaTime
        );


        // 3. Waypoint 도착 확인
        if (Vector3.Distance(
            transform.position,
            targetPosition) < 0.001f)
        {
            transform.position = targetPosition;

            Debug.Log(
                $"[Pr3Controller] Waypoint {routes[routeIndex].id} 도착");

            int arrivedID = routes[routeIndex].id;
            bool isStartPoint = (routeIndex == 0);

            routeIndex++;

            isRotating = !lockRotation;


            // 4. 최종 목적지 도착
            if (routeIndex >= routes.Count)
            {
                StopMovement();

                Debug.Log(
                    $"[{gameObject.name}] 최종 목적지에 도착했습니다.");

                onCompleteMove?.Invoke();

                // 자동 이동 중이면 다음 목적지로 진행
                if (isAutoRunning)
                    OnAutoDestinationArrived();

                return;
            }


            // 5. 정지 지점 도착 (출발 Waypoint와 최종 목적지는 제외)
            if (!isStartPoint && IsStopWaypoint(arrivedID))
            {
                isWaiting = true;

                Debug.Log(
                    $"[Pr3Controller] 정지 지점 {arrivedID} 도착, 재개 조건을 기다립니다.");

                onArriveStop?.Invoke();
            }
        }
    }


    // ==========================================
    // 자동 이동
    // ==========================================

    /// <summary>
    /// 저장된 목적지 목록을 처음부터 자동으로 순회하기 시작합니다.
    /// </summary>
    public void StartAutoRun()
    {
        if (autoDestinationIDs == null || autoDestinationIDs.Length == 0)
        {
            Debug.LogWarning(
                "[Pr3Controller] 자동 이동 목적지 목록이 비어 있습니다.");

            return;
        }

        isAutoRunning = true;
        isAutoWaiting = false;
        autoIndex = 0;

        GoNextAuto();
    }

    /// <summary>
    /// 자동 이동을 중단합니다. (진행 중인 이동은 그대로 두지 않고 정지)
    /// </summary>
    public void StopAutoRun()
    {
        isAutoRunning = false;
        isAutoWaiting = false;
    }

    // 목적지 도착 시 호출: 대기 후 다음 목적지로
    private void OnAutoDestinationArrived()
    {
        if (arriveWaitTime > 0f)
        {
            isAutoWaiting = true;
            autoWaitTimer = arriveWaitTime;
        }
        else
        {
            GoNextAuto();
        }
    }

    // 다음 목적지로 이동 명령 (실패하면 건너뜀)
    private void GoNextAuto()
    {
        if (autoDestinationIDs == null || autoDestinationIDs.Length == 0)
        {
            StopAutoRun();
            return;
        }

        int tryCount = autoDestinationIDs.Length;

        for (int i = 0; i < tryCount; i++)
        {
            // 마지막까지 모두 방문한 경우
            if (autoIndex >= autoDestinationIDs.Length)
            {
                if (!loop)
                {
                    Debug.Log(
                        "[Pr3Controller] 자동 이동 목록을 모두 완료했습니다.");

                    StopAutoRun();
                    onCompleteAutoRun?.Invoke();
                    return;
                }

                autoIndex = 0;
            }

            int destinationID = autoDestinationIDs[autoIndex];
            autoIndex++;

            if (MoveTo(destinationID))
                return;

            Debug.LogWarning(
                $"[Pr3Controller] 자동 이동: 목적지 {destinationID} 이동 실패, 다음 목적지로 넘어갑니다.");
        }

        // 모든 목적지가 실패하면 무한 반복을 막기 위해 중단
        Debug.LogError(
            "[Pr3Controller] 자동 이동: 이동 가능한 목적지가 없어 중단합니다.");

        StopAutoRun();
    }


    // ==========================================
    // 정지 지점 확인
    // ==========================================

    private bool IsStopWaypoint(int waypointID)
    {
        if (stopWaypointIDs == null)
            return false;

        for (int i = 0; i < stopWaypointIDs.Length; i++)
        {
            if (stopWaypointIDs[i] == waypointID)
                return true;
        }

        return false;
    }


    // ==========================================
    // 재개 조건: 운반 도구를 제외한 자식이 없는지 확인
    // ==========================================

    private bool IsOnlyCarrierLeft()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i) != carrier)
                return false;
        }

        return true;
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
        isWaiting = false;
    }


    // ==========================================
    // 새로운 경로 설정
    // ==========================================

    public void SetRoute(List<WayPoint> newRoutes)
    {
        if (newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError(
                "[Pr3Controller] 전달받은 경로가 비어 있습니다.");

            StopMovement();
            return;
        }

        routes = new List<WayPoint>(newRoutes);
        routeIndex = 0;
        isRotating = !lockRotation;
        isWaiting = false;

        // 경로 설정이 완료된 경우에만 이동 허용
        canMove = true;

        Debug.Log(
            $"[Pr3Controller] 경로 설정 완료 / 경유 Waypoint 수: {routes.Count}");

        Debug.Log(
            $"[Pr3Controller] 첫 번째 이동 Waypoint: {routes[0].id}");

        Debug.Log(
            $"[Pr3Controller] 최종 목적지 Waypoint: {routes[routes.Count - 1].id}");
    }


    // ==========================================
    // 목적지 이동 명령
    // ==========================================

    /// <summary>
    /// 수동 이동 명령. 자동 이동 중이면 자동 이동을 중단하고 이 명령을 우선합니다.
    /// </summary>
    public bool Go(int destinationID)
    {
        StopAutoRun();

        return MoveTo(destinationID);
    }

    // 실제 이동 명령 (자동/수동 공통)
    private bool MoveTo(int destinationID)
    {
        Debug.Log(
            $"[Pr3Controller] 이동 명령 / 목적지 ID: {destinationID}");

        if (manager == null)
        {
            Debug.LogError(
                "[Pr3Controller] Pr3Route가 연결되지 않았습니다.");

            return false;
        }

        int startID =
            manager.GetNearestWaypointID(transform.position);

        if (startID < 0)
        {
            Debug.LogError(
                "[Pr3Controller] 출발 Waypoint를 찾지 못했습니다.");

            return false;
        }

        Debug.Log(
            $"[Pr3Controller] 출발지: {startID} / 목적지: {destinationID}");

        return manager.GetRoutes(this, startID, destinationID);
    }
}