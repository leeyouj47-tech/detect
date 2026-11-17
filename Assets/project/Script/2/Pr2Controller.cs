using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum Pr2Vehicle
{
    AGV,
    Trolley,
    ICCU,
    CarBody
}

public class Pr2Controller : MonoBehaviour
{
    [System.Serializable]
    private class VehicleMotion
    {
        public Transform target;
        public float speed = 1f;
        public float rotateSpeed = 90f;
        public UnityEvent onCompleteMove = new UnityEvent();

        [HideInInspector] public List<WayPoint> routes;
        [HideInInspector] public int routeIndex;
        [HideInInspector] public bool canMove;
        [HideInInspector] public bool isRotating;
        [HideInInspector] public bool isBusy;
    }

    [Header("이동 오브젝트")]
    public Transform agv;
    public Transform trolley;
    public Transform iccu;
    public Transform carBody;

    [Header("개별 이동 속도")]
    public float agvSpeed = 1f;
    public float trolleySpeed = 1f;
    public float iccuSpeed = 1f;
    public float carBodySpeed = 1f;

    [Header("개별 회전 속도")]
    public float agvRotateSpeed = 90f;
    public float trolleyRotateSpeed = 90f;
    public float iccuRotateSpeed = 90f;
    public float carBodyRotateSpeed = 90f;

    [Header("경로 관리자")]
    public RailRoute agvRailRoute;
    public RailRoute trolleyRailRoute;
    public ICCURoute iccuRoute;
    public CarRoute carRoute;

    [Header("이동 완료 이벤트")]
    public UnityEvent onAGVCompleteMove = new UnityEvent();
    public UnityEvent onTrolleyCompleteMove = new UnityEvent();
    public UnityEvent onICCUCompleteMove = new UnityEvent();
    public UnityEvent onCarBodyCompleteMove = new UnityEvent();

    private VehicleMotion agvMotion;
    private VehicleMotion trolleyMotion;
    private VehicleMotion iccuMotion;
    private VehicleMotion carBodyMotion;

    private void Awake()
    {
        agvMotion = CreateMotion(agv, agvSpeed, agvRotateSpeed, onAGVCompleteMove);
        trolleyMotion = CreateMotion(trolley, trolleySpeed, trolleyRotateSpeed, onTrolleyCompleteMove);
        iccuMotion = CreateMotion(iccu, iccuSpeed, iccuRotateSpeed, onICCUCompleteMove);
        carBodyMotion = CreateMotion(carBody, carBodySpeed, carBodyRotateSpeed, onCarBodyCompleteMove);
    }

    private VehicleMotion CreateMotion(
        Transform target,
        float moveSpeed,
        float turnSpeed,
        UnityEvent completeEvent)
    {
        VehicleMotion motion = new VehicleMotion();
        motion.target = target;
        motion.speed = moveSpeed;
        motion.rotateSpeed = turnSpeed;
        motion.onCompleteMove = completeEvent;
        motion.canMove = false;
        motion.isRotating = false;
        motion.isBusy = false;
        motion.routeIndex = 0;
        return motion;
    }

    private void FixedUpdate()
    {
        UpdateMotion(agvMotion, "AGV");
        UpdateMotion(trolleyMotion, "Trolley");
        UpdateMotion(iccuMotion, "ICCU");
        UpdateMotion(carBodyMotion, "CarBody");
    }

    private void UpdateMotion(VehicleMotion motion, string objectName)
    {
        if (motion == null || !motion.canMove)
            return;

        if (motion.target == null)
        {
            Debug.LogError("[Pr2Controller] " + objectName + " Transform이 연결되지 않았습니다.");
            StopMotion(motion);
            return;
        }

        if (motion.routes == null || motion.routes.Count == 0)
        {
            StopMotion(motion);
            return;
        }

        if (motion.routeIndex >= motion.routes.Count)
            return;

        WayPoint waypoint = motion.routes[motion.routeIndex];

        if (waypoint == null)
        {
            Debug.LogError("[Pr2Controller] " + objectName + " 경로에 null Waypoint가 있습니다.");
            StopMotion(motion);
            return;
        }

        Vector3 targetPosition = waypoint.transform.position;
        Vector3 toward = targetPosition - motion.target.position;
        toward.y = 0f;

        // 1. 목표 방향으로 회전
        if (motion.isRotating)
        {
            if (toward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toward);

                motion.target.rotation = Quaternion.RotateTowards(
                    motion.target.rotation,
                    targetRotation,
                    motion.rotateSpeed * Time.fixedDeltaTime);

                if (Quaternion.Angle(motion.target.rotation, targetRotation) < 0.1f)
                {
                    motion.target.rotation = targetRotation;
                    motion.isRotating = false;
                }
            }
            else
            {
                motion.isRotating = false;
            }

            return;
        }

        // 2. 목표 위치로 이동 (현재 Y 높이 유지)
        targetPosition.y = motion.target.position.y;

        motion.target.position = Vector3.MoveTowards(
            motion.target.position,
            targetPosition,
            motion.speed * Time.fixedDeltaTime);

        // 3. Waypoint 도착 확인
        if (Vector3.Distance(motion.target.position, targetPosition) < 0.001f)
        {
            motion.target.position = targetPosition;

            Debug.Log("[Pr2Controller] " + objectName + " Waypoint " + waypoint.id + " 도착");

            motion.routeIndex++;
            motion.isRotating = true;

            // 4. 최종 목적지 도착
            if (motion.routeIndex >= motion.routes.Count)
            {
                UnityEvent completed = motion.onCompleteMove;
                StopMotion(motion);

                Debug.Log("[Pr2Controller] " + objectName + " 최종 목적지 도착");
                if (completed != null)
                    completed.Invoke();
            }
        }
    }

    private void StopMotion(VehicleMotion motion)
    {
        if (motion == null)
            return;

        motion.canMove = false;
        motion.isBusy = false;
        motion.routes = null;
        motion.routeIndex = 0;
        motion.isRotating = false;
    }

    public bool IsBusy(Pr2Vehicle vehicle)
    {
        VehicleMotion motion = GetMotion(vehicle);
        return motion != null && motion.isBusy;
    }

    public bool Go(Pr2Vehicle vehicle, int destinationID)
    {
        VehicleMotion motion = GetMotion(vehicle);

        if (motion == null || motion.target == null)
        {
            Debug.LogError("[Pr2Controller] " + vehicle + " 오브젝트가 연결되지 않았습니다.");
            return false;
        }

        if (motion.isBusy)
        {
            Debug.LogWarning("[Pr2Controller] " + vehicle + " 이미 이동 중입니다.");
            return false;
        }

        int startID = GetNearestWaypointID(vehicle, motion.target.position);
        if (startID < 0)
        {
            Debug.LogError("[Pr2Controller] " + vehicle + " 출발 Waypoint를 찾지 못했습니다.");
            return false;
        }

        bool result = false;

        if (vehicle == Pr2Vehicle.AGV && agvRailRoute != null)
            result = agvRailRoute.GetRoutes(this, vehicle, startID, destinationID);
        else if (vehicle == Pr2Vehicle.Trolley && trolleyRailRoute != null)
            result = trolleyRailRoute.GetRoutes(this, vehicle, startID, destinationID);
        else if (vehicle == Pr2Vehicle.ICCU && iccuRoute != null)
            result = iccuRoute.GetRoutes(this, startID, destinationID);
        else if (vehicle == Pr2Vehicle.CarBody && carRoute != null)
            result = carRoute.GetRoutes(this, startID, destinationID);

        if (!result)
            Debug.LogError("[Pr2Controller] " + vehicle + " 경로 생성 실패: " + startID + " -> " + destinationID);

        return result;
    }

    public int GetNearestWaypointID(Pr2Vehicle vehicle, Vector3 position)
    {
        if (vehicle == Pr2Vehicle.AGV && agvRailRoute != null)
            return agvRailRoute.GetNearestWaypointID(position);
        if (vehicle == Pr2Vehicle.Trolley && trolleyRailRoute != null)
            return trolleyRailRoute.GetNearestWaypointID(position);
        if (vehicle == Pr2Vehicle.ICCU && iccuRoute != null)
            return iccuRoute.GetNearestWaypointID(position);
        if (vehicle == Pr2Vehicle.CarBody && carRoute != null)
            return carRoute.GetNearestWaypointID(position);

        return -1;
    }

    public void SetRoute(Pr2Vehicle vehicle, List<WayPoint> newRoutes)
    {
        VehicleMotion motion = GetMotion(vehicle);

        if (motion == null || newRoutes == null || newRoutes.Count == 0)
        {
            Debug.LogError("[Pr2Controller] " + vehicle + " 전달받은 경로가 비어 있습니다.");
            return;
        }

        if (motion.isBusy)
        {
            Debug.LogWarning("[Pr2Controller] " + vehicle + " 이동 중 경로 변경을 거부했습니다.");
            return;
        }

        motion.routes = new List<WayPoint>(newRoutes);
        motion.routeIndex = 0;
        motion.isRotating = true;
        motion.canMove = true;
        motion.isBusy = true;

        Debug.Log("[Pr2Controller] " + vehicle + " 경로 설정 완료 / Waypoint 수: " + newRoutes.Count);
        Debug.Log("[Pr2Controller] " + vehicle + " 출발: " + newRoutes[0].id +
                  " / 목적지: " + newRoutes[newRoutes.Count - 1].id);
    }

    private VehicleMotion GetMotion(Pr2Vehicle vehicle)
    {
        if (vehicle == Pr2Vehicle.AGV) return agvMotion;
        if (vehicle == Pr2Vehicle.Trolley) return trolleyMotion;
        if (vehicle == Pr2Vehicle.ICCU) return iccuMotion;
        if (vehicle == Pr2Vehicle.CarBody) return carBodyMotion;
        return null;
    }
}
