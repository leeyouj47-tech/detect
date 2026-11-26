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

    [Header("배터리 결합 연출")]
    [Tooltip("배터리가 아래에서 밀려 올라오는 거리(m)")]
    public float batteryRiseDistance = 0.3f;
    [Tooltip("결합 연출 중 상승 구간 비율 (나머지는 볼트 체결)")]
    [Range(0.1f, 0.9f)] public float batteryRiseRatio = 0.5f;
    [Tooltip("볼트 체결 표시 색")]
    public Color boltColor = new Color(1f, 0.75f, 0.1f);
    [Tooltip("결합 중 이 카메라로 자동 전환 (비우면 전환 안 함)")]
    public CameraSwitcher cameraSwitcher;
    public int couplingCameraIndex = 5;
    [Tooltip("결합이 끝난 뒤 원래 카메라로 돌아가기 전 대기 시간(초)")]
    public float couplingCameraHold = 1.5f;

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

    [Header("ICCU 표시 (레일 위에는 움직이는 ICCU만 표시)")]
    [Tooltip("레일(컨베이어)을 따라 움직이는 ICCU")]
    public GameObject iccuOnConveyor;
    [Tooltip("로봇이 집어서 차체에 결합하는 ICCU (도착 전까지 숨김)")]
    public GameObject iccuForRobot;
    [Tooltip("움직이는 ICCU가 이 Waypoint에 도착하면 로봇용 ICCU로 교체")]
    public int iccuHandoverWaypointID = 2;
    [Tooltip("로봇이 내려놓은 ICCU를 차체에 고정해 계속 보이게 함")]
    public bool attachICCUToCarBody = true;

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

    [Header("불량검사 조명 연출 (초록불 스캔, 불량 시 빨강)")]
    public InspectionLight iccuInspectionLight;
    [Tooltip("ICCU 검사 대상 (비우면 ICCU Route의 이동 대상)")]
    public Transform iccuInspectionTarget;
    public InspectionLight chassisInspectionLight;
    [Tooltip("차체 검사 대상 (비우면 Car Body)")]
    public Transform chassisInspectionTarget;

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

        if (iccu != null)
            iccu.Arrived += OnICCUArrived;

        // 레일 위에는 움직이는 ICCU만 보이게: 로봇용 ICCU는 도착 전까지 숨김
        if (iccuOnConveyor != null && iccuForRobot != null && iccuOnConveyor != iccuForRobot)
            iccuForRobot.SetActive(false);
    }

    // 움직이는 ICCU가 로봇 집는 위치에 도착하면 로봇용 ICCU와 교체
    private void OnICCUArrived(int waypointID)
    {
        if (waypointID != iccuHandoverWaypointID || iccuForRobot == null)
            return;

        iccuForRobot.SetActive(true);

        if (iccuOnConveyor != null && iccuOnConveyor != iccuForRobot)
            iccuOnConveyor.SetActive(false);

        Debug.Log("[Pr2Controller] ICCU 집는 위치 도착: 로봇용 ICCU로 교체");
    }

    private void OnDestroy()
    {
        if (lift != null)
            lift.Arrived -= OnLiftArrived;

        if (agv != null)
            agv.Arrived -= OnAGVRouteArrived;

        if (iccu != null)
            iccu.Arrived -= OnICCUArrived;
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

        // 결합 장면 카메라로 전환
        int previousCamera = -1;
        if (cameraSwitcher != null && couplingCameraIndex > 0 &&
            cameraSwitcher.cameras != null && couplingCameraIndex < cameraSwitcher.cameras.Length)
        {
            previousCamera = cameraSwitcher.CurrentIndex;
            if (previousCamera != couplingCameraIndex)
                cameraSwitcher.SwitchTo(couplingCameraIndex);
        }

        AttachBatteryToCarBody();

        if (carBodyBattery != null)
            yield return StartCoroutine(BatteryCouplingAnimation(carBodyBattery.transform, batteryCouplingTime));
        else
            yield return new WaitForSeconds(batteryCouplingTime);

        onDone?.Invoke();

        // 잠시 결합 상태를 보여준 뒤 원래 카메라로 복귀
        if (previousCamera >= 0 && previousCamera != couplingCameraIndex)
        {
            yield return new WaitForSeconds(couplingCameraHold);
            if (cameraSwitcher.CurrentIndex == couplingCameraIndex)
                cameraSwitcher.SwitchTo(previousCamera);
        }
    }

    // 배터리가 아래에서 밀려 올라와 차체에 닿고, 볼트 4개가 차례로 조여지는 연출
    // 최종 위치는 결합 전 AGV 배터리 위치와 같음
    private IEnumerator BatteryCouplingAnimation(Transform battery, float duration)
    {
        duration = Mathf.Max(0.5f, duration);
        Vector3 finalPos = battery.position;
        Vector3 startPos = finalPos - Vector3.up * batteryRiseDistance;

        float riseTime = duration * batteryRiseRatio;
        float boltTime = duration - riseTime;

        // 1. 상승 (가감속)
        for (float t = 0f; t < riseTime; t += Time.deltaTime)
        {
            battery.position = Vector3.Lerp(startPos, finalPos, Mathf.SmoothStep(0f, 1f, t / riseTime));
            yield return null;
        }
        battery.position = finalPos;

        // 2. 볼트 체결: 모서리 4곳 볼트가 차례로 회전하며 조여지고, 살짝 진동
        GameObject[] bolts = CreateBolts(battery);
        for (float t = 0f; t < boltTime; t += Time.deltaTime)
        {
            float k = t / boltTime;
            for (int i = 0; i < bolts.Length; i++)
            {
                float local = Mathf.Clamp01(k * bolts.Length - i);
                Transform b = bolts[i].transform;
                if (local > 0f && local < 1f)
                    b.Rotate(Vector3.up, 900f * Time.deltaTime, Space.World);
                b.localScale = Vector3.Lerp(BoltStartScale, BoltEndScale, local);
            }

            float shake = Mathf.Sin(t * 60f) * 0.003f * (1f - k);
            battery.position = finalPos + new Vector3(shake, 0f, -shake);
            yield return null;
        }
        battery.position = finalPos;

        foreach (GameObject b in bolts)
            Destroy(b);
    }

    private static readonly Vector3 BoltStartScale = new Vector3(0.07f, 0.06f, 0.07f);
    private static readonly Vector3 BoltEndScale = new Vector3(0.09f, 0.01f, 0.09f);

    private GameObject[] CreateBolts(Transform battery)
    {
        Bounds bounds = new Bounds(battery.position, Vector3.zero);
        bool found = false;
        foreach (Renderer r in battery.GetComponentsInChildren<Renderer>())
        {
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        Color c = boltColor * 2f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);

        Vector3 e = bounds.extents * 0.8f;
        Vector3[] corners =
        {
            new Vector3(-e.x, 0f, -e.z), new Vector3(e.x, 0f, -e.z),
            new Vector3(e.x, 0f, e.z), new Vector3(-e.x, 0f, e.z)
        };

        GameObject[] bolts = new GameObject[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bolt.name = "Coupling Bolt " + (i + 1);
            Destroy(bolt.GetComponent<Collider>());
            bolt.GetComponent<Renderer>().sharedMaterial = mat;
            bolt.transform.SetParent(null);
            bolt.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.02f, bounds.center.z) + corners[i];
            bolt.transform.localScale = BoltStartScale;
            bolts[i] = bolt;
        }
        return bolts;
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

        if (attachICCUToCarBody && iccuForRobot != null && carBody != null)
            StartCoroutine(WatchICCURelease(() => completed));

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

    // 로봇이 ICCU를 집었다가 놓는 순간 차체에 고정 (떨어지거나 남지 않고 차체와 같이 이동)
    private IEnumerator WatchICCURelease(Func<bool> robotDone)
    {
        Transform part = iccuForRobot.transform;
        Transform startParent = part.parent;

        // 집을 때까지 대기 (부모가 그리퍼로 바뀜)
        while (part.parent == startParent && !robotDone())
            yield return null;

        // 놓을 때까지 대기 (그리퍼가 부모를 해제)
        while (part.parent != null && part.parent != startParent && !robotDone())
            yield return null;

        if (part.parent == startParent)
        {
            Debug.LogWarning("[Pr2Controller] 로봇이 ICCU를 집지 않았습니다. 차체 고정을 건너뜁니다.");
            yield break;
        }

        Rigidbody body = part.GetComponent<Rigidbody>();
        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
        }

        part.SetParent(carBody, true);
        Debug.Log("[Pr2Controller] ICCU를 차체에 결합: 이후 차체와 같이 이동");
    }

    // ==========================================
    // 불량검사 (inspectionTime 동안 실행 후 결과 전달)
    // ==========================================

    public void RunICCUInspection(Action<bool> onDone)
    {
        Transform target = iccuInspectionTarget != null ? iccuInspectionTarget
            : (iccu != null && iccu.moveTarget != null ? iccu.moveTarget : null);
        StartCoroutine(InspectionRoutine(onICCUInspection, () => !iccuDefect, onDone,
            iccuInspectionLight, target));
    }

    public void RunChassisInspection(Action<bool> onDone)
    {
        Transform target = chassisInspectionTarget != null ? chassisInspectionTarget : carBody;
        StartCoroutine(InspectionRoutine(onChassisInspection, () => !chassisDefect, onDone,
            chassisInspectionLight, target));
    }

    private IEnumerator InspectionRoutine(
        UnityEvent startEvent, Func<bool> isGood, Action<bool> onDone,
        InspectionLight inspectionLight, Transform inspectionTarget)
    {
        startEvent?.Invoke();

        if (inspectionLight != null)
            inspectionLight.BeginManual(inspectionTarget, inspectionTime);

        yield return new WaitForSeconds(inspectionTime);

        bool good = isGood();

        if (inspectionLight != null)
            inspectionLight.ShowResult(good);

        onDone?.Invoke(good);
    }

    // ==========================================
    // 전체 정지
    // ==========================================

    public void StopAllDevices()
    {
        StopAllCoroutines();

        if (iccuInspectionLight != null) iccuInspectionLight.StopManual();
        if (chassisInspectionLight != null) chassisInspectionLight.StopManual();

        if (agv != null) agv.StopMovement();
        if (lift != null) lift.StopMovement();
        if (rail != null) rail.StopMovement();
        if (iccu != null) iccu.StopMovement();

        onProcessStop?.Invoke();

        Debug.LogWarning("[Pr2Controller] 전체 장치 정지");
    }
}
