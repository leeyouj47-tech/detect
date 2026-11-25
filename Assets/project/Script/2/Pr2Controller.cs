using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// ==========================================
// Project 2 전체 컨트롤러
// 실제 장비 동작(이동/애니메이션/로봇/검사)을 담당
// PLC 신호 처리는 Pr2Connector가 담당
// ==========================================
public class Pr2Controller : MonoBehaviour
{
    [Header("장치별 Route")]
    public AGVRoute agv;
    public LiftRoute lift;
    public RailRoute rail;
    public ICCURoute iccu;

    [Header("차체 이동 (레일 -> 리프트)")]
    [Tooltip("차체. 처음에는 레일(트롤리) 자식으로 두면 레일과 같이 이동")]
    public Transform carBody;
    [Tooltip("차체를 넘겨받을 리프트 캐리어 (비우면 리프트 Route의 Move Target)")]
    public Transform liftCarrier;
    [Tooltip("리프트가 이 Waypoint에 도착하면 차체를 리프트로 옮김")]
    public int liftReceiveWaypointID = 0;

    [Header("배터리 결합 (M831)")]
    [Tooltip("배터리 결합 연출 시간(초). 끝나면 M751 완료 신호")]
    public float batteryCouplingTime = 1f;
    [Tooltip("차체에 보일 배터리. 비우면 AGV 배터리팩을 복제해 차체에 붙임")]
    public GameObject carBodyBattery;
    public UnityEvent onBatteryCoupling;

    [Header("AGV 빠지기 (M832)")]
    [Tooltip("빠져나갈 AGV 본체")]
    public Transform agvObject;
    [Tooltip("AGV 빠지기 시작 시 숨길 배터리팩")]
    public GameObject agvBatteryPack;
    [Tooltip("Y축 회전 각도 (음수 = 왼쪽)")]
    public float agvTurnAngle = -90f;
    [Tooltip("회전에 걸리는 시간(초)")]
    public float agvTurnDuration = 1f;
    [Tooltip("회전 후 앞으로 빠지는 속도(m/s)")]
    public float agvExitSpeed = 1f;
    [Tooltip("빠지는 시간(초). 이 시간 후 정지")]
    public float agvExitDuration = 5f;
    [Tooltip("AGV 빠지기 후 리프트가 이동할 Waypoint ID (-1이면 이동 안 함)")]
    public int liftWaypointAfterAGVLeave = 4;
    [Tooltip("AGV 빠지기 후 AGV Waypoint 명령(WP2~4)은 차체만 이동")]
    public bool moveCarBodyOnAGVRoute = true;
    public UnityEvent onAGVLeave;

    [Header("스키드 (차체 도착 후 같이 이동)")]
    [Tooltip("차체가 이 AGV Waypoint에 도착하면 스키드에 올리고, 이후 스키드+차체가 같이 이동")]
    public Transform skid;
    public int skidJoinWaypointID = 2;

    [Header("로봇팔 (M833)")]
    public OfflineProgram robotProgram;
    public int robotProgramNumber = 1;
    [Tooltip("로봇 동작 제한 시간(초). 0이면 무제한")]
    public float robotTimeout = 60f;
    public UnityEvent onRobotArm;

    [Header("불량검사 (M834 ICCU / M835 차체)")]
    public float inspectionTime = 5f;
    [Tooltip("체크하면 ICCU 검사 결과 = 불량")]
    public bool iccuDefect;
    [Tooltip("체크하면 차체 검사 결과 = 불량")]
    public bool chassisDefect;
    public UnityEvent onICCUInspection;
    public UnityEvent onChassisInspection;

    [Header("전체 정지 이벤트")]
    public UnityEvent onProcessStop;

    // ==========================================
    // 차체 이동: 리프트 도착 시 차체를 리프트 캐리어로 옮김
    // ==========================================

    private void Start()
    {
        if (lift != null)
            lift.Arrived += OnLiftArrived;

        if (agv != null)
            agv.Arrived += OnAGVRouteArrived;
    }

    private void OnDestroy()
    {
        if (lift != null)
            lift.Arrived -= OnLiftArrived;

        if (agv != null)
            agv.Arrived -= OnAGVRouteArrived;
    }

    // 차체가 스키드 위치에 도착하면 스키드에 올리고, 이후 이동 대상을 스키드로 변경
    private void OnAGVRouteArrived(int waypointID)
    {
        if (skid == null || carBody == null || waypointID != skidJoinWaypointID)
            return;

        if (agv.moveTarget != carBody)
            return;

        carBody.SetParent(skid, true);
        agv.SetMoveTarget(skid);

        Debug.Log($"[Pr2Controller] 차체를 스키드에 올림. 이후 AGV Waypoint 이동 대상: {skid.name}");
    }

    private void OnLiftArrived(int waypointID)
    {
        if (carBody == null || waypointID != liftReceiveWaypointID)
            return;

        Transform carrier = liftCarrier != null
            ? liftCarrier
            : (lift.moveTarget != null ? lift.moveTarget : lift.transform);

        if (carBody.parent == carrier)
            return;

        carBody.SetParent(carrier, true);
        Debug.Log($"[Pr2Controller] 차체를 리프트로 옮김: {carBody.name} -> {carrier.name}");
    }

    // ==========================================
    // 장치 이동
    // ==========================================

    public bool MoveAGV(int waypointID) => agv != null && agv.Go(waypointID);
    public bool MoveLift(int waypointID) => lift != null && lift.Go(waypointID);
    public bool MoveRail(int waypointID) => rail != null && rail.Go(waypointID);
    public bool MoveICCU(int waypointID) => iccu != null && iccu.Go(waypointID);

    // ==========================================
    // 불량 여부 설정 (UI Toggle에서 Dynamic bool로 연결 가능)
    // ==========================================

    public void SetICCUDefect(bool defect) => iccuDefect = defect;
    public void SetChassisDefect(bool defect) => chassisDefect = defect;

    // ==========================================
    // 배터리 결합
    // ==========================================

    public void RunBatteryCoupling(Action onDone)
    {
        StartCoroutine(BatteryCouplingRoutine(onDone));
    }

    private IEnumerator BatteryCouplingRoutine(Action onDone)
    {
        onBatteryCoupling?.Invoke();

        AttachBatteryToCarBody();

        yield return new WaitForSeconds(batteryCouplingTime);

        onDone?.Invoke();
    }

    // AGV 배터리는 숨기고, 같은 위치에 차체 배터리를 보이게 함 (이후 차체와 같이 이동)
    private void AttachBatteryToCarBody()
    {
        if (carBodyBattery == null && agvBatteryPack != null && carBody != null)
        {
            Transform source = agvBatteryPack.transform;
            carBodyBattery = Instantiate(agvBatteryPack, source.position, source.rotation, carBody);
            carBodyBattery.transform.localScale = Vector3.Scale(
                source.lossyScale,
                new Vector3(1f / carBody.lossyScale.x, 1f / carBody.lossyScale.y, 1f / carBody.lossyScale.z));
            carBodyBattery.name = agvBatteryPack.name + " (CarBody)";
        }

        if (carBodyBattery != null)
            carBodyBattery.SetActive(true);
        else
            Debug.LogWarning("[Pr2Controller] 차체 배터리를 만들 수 없습니다. Agv Battery Pack / Car Body 연결을 확인하세요.");

        if (agvBatteryPack != null)
            agvBatteryPack.SetActive(false);

        Debug.Log("[Pr2Controller] 배터리 결합: AGV 배터리 숨김, 차체 배터리 표시");
    }

    // ==========================================
    // AGV 빠지기 -> 리프트 WP 이동
    // ==========================================

    public bool CanRunAGVLeave()
    {
        return lift == null || !lift.IsMoving();
    }

    public void RunAGVLeave(Action onDone)
    {
        StartCoroutine(AGVLeaveRoutine(onDone));
    }

    private IEnumerator AGVLeaveRoutine(Action onDone)
    {
        onAGVLeave?.Invoke();

        // 1. 배터리팩 숨김
        if (agvBatteryPack != null)
            agvBatteryPack.SetActive(false);

        Transform agvTransform = agvObject != null
            ? agvObject
            : (agv != null && agv.moveTarget != null ? agv.moveTarget : null);

        if (agvTransform != null)
        {
            // 2. Y축 회전
            Quaternion startRotation = agvTransform.rotation;
            Quaternion endRotation = Quaternion.AngleAxis(agvTurnAngle, Vector3.up) * startRotation;

            for (float t = 0f; t < agvTurnDuration; t += Time.deltaTime)
            {
                agvTransform.rotation = Quaternion.Slerp(startRotation, endRotation, t / agvTurnDuration);
                yield return null;
            }

            agvTransform.rotation = endRotation;

            // 3. 회전한 방향(앞)으로 일정 시간 빠진 뒤 정지
            Vector3 exitDirection = agvTransform.forward;

            for (float t = 0f; t < agvExitDuration; t += Time.deltaTime)
            {
                agvTransform.position += exitDirection * agvExitSpeed * Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("[Pr2Controller] AGV Object가 연결되지 않아 AGV 빠지기 동작을 건너뜁니다.");
        }

        // 4. 리프트 이동
        if (lift != null && liftWaypointAfterAGVLeave >= 0)
        {
            yield return new WaitUntil(() => !lift.IsMoving());

            if (lift.Go(liftWaypointAfterAGVLeave))
                yield return new WaitUntil(() => !lift.IsMoving());
        }

        // 5. 이후 AGV Waypoint 명령은 차체만 이동
        if (moveCarBodyOnAGVRoute && agv != null && carBody != null)
        {
            carBody.SetParent(null, true);
            agv.SetMoveTarget(carBody);
            Debug.Log($"[Pr2Controller] 이후 AGV Waypoint 이동 대상: {carBody.name}");
        }

        onDone?.Invoke();
    }

    // ==========================================
    // 로봇팔
    // ==========================================

    /// <summary>로봇이 다른 작업 중이면 false (나중에 다시 시도)</summary>
    public bool RunRobotArm(Action onDone)
    {
        if (robotProgram == null)
        {
            Debug.LogWarning("[Pr2Controller] Robot Program이 연결되지 않아 바로 완료 처리합니다.");
            onRobotArm?.Invoke();
            onDone?.Invoke();
            return true;
        }

        if (robotProgram.currentState != TaskState.Wait &&
            robotProgram.currentState != TaskState.Done)
            return false;

        StartCoroutine(RobotArmRoutine(onDone));
        return true;
    }

    private IEnumerator RobotArmRoutine(Action onDone)
    {
        onRobotArm?.Invoke();

        bool completed = false;
        UnityAction handler = () => completed = true;

        robotProgram.onProgramCompleted.AddListener(handler);
        robotProgram.RunProgram(robotProgramNumber);

        float elapsed = 0f;

        while (!completed && (robotTimeout <= 0f || elapsed < robotTimeout))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        robotProgram.onProgramCompleted.RemoveListener(handler);

        if (!completed)
        {
            Debug.LogError("[Pr2Controller] 로봇팔 동작 시간 초과. 완료 신호를 보내지 않습니다.");
            yield break;
        }

        onDone?.Invoke();
    }

    // ==========================================
    // 불량검사 (inspectionTime 동안 실행 후 결과 전달)
    // ==========================================

    public void RunICCUInspection(Action<bool> onDone)
    {
        StartCoroutine(InspectionRoutine(onICCUInspection, () => !iccuDefect, onDone));
    }

    public void RunChassisInspection(Action<bool> onDone)
    {
        StartCoroutine(InspectionRoutine(onChassisInspection, () => !chassisDefect, onDone));
    }

    private IEnumerator InspectionRoutine(
        UnityEvent startEvent, Func<bool> isGood, Action<bool> onDone)
    {
        startEvent?.Invoke();

        yield return new WaitForSeconds(inspectionTime);

        onDone?.Invoke(isGood());
    }

    // ==========================================
    // 전체 정지
    // ==========================================

    public void StopAllDevices()
    {
        StopAllCoroutines();

        if (agv != null) agv.StopMovement();
        if (lift != null) lift.StopMovement();
        if (rail != null) rail.StopMovement();
        if (iccu != null) iccu.StopMovement();

        onProcessStop?.Invoke();

        Debug.LogWarning("[Pr2Controller] 전체 장치 정지");
    }
}
