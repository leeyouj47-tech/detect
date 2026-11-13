
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Pr2Controller : MonoBehaviour
{
    [Header("Route 설정")]
    public ICCURoute manager;
    public List<WayPoint> routes;

    private int routeIndex;
    private bool canMove = false;
    private bool isRotating = false;

    [Header("이동 설정")]
    public float speed = 1f;
    public float rotateSpeed = 90f;

    [Header("이동 완료 이벤트")]
    public UnityEvent onCompleteMove = new UnityEvent();

    private void Start()
    {
        canMove = false;
        isRotating = false;
        routeIndex = 0;

        if (manager == null)
            Debug.LogError($"[{name}] Process2Route가 연결되지 않았습니다.");
    }

    private void FixedUpdate()
    {
        if (!canMove)
            return;

        if (routes == null || routes.Count == 0)
        {
            StopMovement();
            return;
        }

        if (routeIndex >= routes.Count)
            return;

        if (routes[routeIndex] == null)
        {
            Debug.LogError($"[{name}] Waypoint가 null입니다.");
            StopMovement();
            return;
        }

        Vector3 targetPosition =
            routes[routeIndex].transform.position;

        Vector3 toward = targetPosition - transform.position;
        toward.y = 0f;

        // 회전
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

        // 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.fixedDeltaTime
        );

        // 도착 확인
        if (Vector3.Distance(
            transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;

            Debug.Log(
                $"[{name}] Waypoint {routes[routeIndex].id} 도착");

            routeIndex++;

            isRotating = true;

            if (routeIndex >= routes.Count)
            {
                StopMovement();

                Debug.Log($"[{name}] 최종 목적지 도착");

                onCompleteMove?.Invoke();
            }
        }
    }

    private void StopMovement()
    {
        canMove = false;
        routes = null;
        routeIndex = 0;
        isRotating = false;
    }

    public void SetRoute(List<WayPoint> newRoutes)
    {
        if (newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError($"[{name}] 전달받은 경로가 없습니다.");
            StopMovement();
            return;
        }

        routes = new List<WayPoint>(newRoutes);
        routeIndex = 0;
        isRotating = true;
        canMove = true;

        Debug.Log(
            $"[{name}] 경로 설정 완료 / " +
            $"시작 WP: {routes[0].id} / " +
            $"목적지 WP: {routes[routes.Count - 1].id}");
    }

    public bool Go(int destinationID)
    {
        if (manager == null)
        {
            Debug.LogError($"[{name}] Route가 연결되지 않았습니다.");
            return false;
        }

        int startID =
            manager.GetNearestWaypointID(transform.position);

        if (startID < 0)
            return false;

        Debug.Log(
            $"[{name}] 이동 요청: {startID} -> {destinationID}");

        return manager.GetRoutes(this, startID, destinationID);
    }
}
