
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class RobotArmConnector : MXObject
{
    [Header("로봇팔 프로그램")]
    public TEST robotArm;

    [Header("PLC -> Unity")]
    public DeviceAddress startAddress = new DeviceAddress("M504");

    [Header("Unity -> PLC")]
    public DeviceAddress completedAddress = new DeviceAddress("M524");

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    //26.11.20 LYJ: 배터리팩 표시제어 추가
    [Header("배터리팩 표시 제어")]
    public BatteryPackVisibility agvBatteryVisibility;
    public BatteryPackVisibility robotBatteryVisibility;

    [Tooltip("조립 완료 후 로봇이 내려놓은 커버(콜라이더 포함)를 통째로 숨김")]
    public bool hideReleasedCoverBody = true;

    //26.11.20 LYJ: 변수 추가
    private bool commandReceived = false;
    private bool isRunning = false;
    private bool completionFeedbackRunning = false;

    // PLC M504 명령으로 실행했는지 확인
    private bool startedByPLC = false;

    // M504를 받았지만 로봇이 아직 준비되지 않아 대기 중
    private bool pendingStart = false;

    [Header("배터리팩 위치 유지")]
    [Tooltip("AGV 위 배터리팩 오브젝트 이름 (조립 후 처음 위치로 되돌림)")]
    public string agvBatteryPackName = "Battery pack";
    private Vector3 batteryPackLocalPosition;
    private Quaternion batteryPackLocalRotation;
    private bool batteryPackPoseSaved;
    // ==========================================
    // 초기화
    // ==========================================

    private void Start()
    {
        if (robotArm == null)
        {
            Debug.LogError(
                "[RobotArmConnector] TEST 연결 안 됨");
            return;
        }

        // 완료 이벤트 생성
        if (robotArm.onProgramCompleted == null)
            robotArm.onProgramCompleted = new UnityEvent();

        // 로봇팔 완료 이벤트 등록
        robotArm.onProgramCompleted.AddListener(
            OnRobotArmCompleted
        );

        // M504 PLC 입력 등록
        MXRequester.Get.AddDeviceAddress(
            startAddress.address,
            OnStartCommand
        );

        SaveBatteryPackPose();

        Debug.Log("[RobotArmConnector] 로봇팔 PLC 연결 완료");
    }

    // ==========================================
    // M504 : 로봇팔 실행
    // ==========================================

    private void OnStartCommand(short value)
    {
        Debug.Log($"[PLC 수신] M504 = {value}");

        if (value == 0)
        {
            commandReceived = false;
            pendingStart = false;
            return;
        }

        if (commandReceived || isRunning)
            return;

        if (robotArm == null)
            return;

        // Keep the request and start once the robot is ready (see Update).
        // Before: the command was dropped when the robot was not powered (state None),
        // and M504 stays ON so it never came again -> robot never moved.
        commandReceived = true;
        pendingStart = true;
        TryStartProgram();
    }

    private void Update()
    {
        if (pendingStart)
            TryStartProgram();
    }

    private void TryStartProgram()
    {
        if (!pendingStart || isRunning || robotArm == null)
            return;

        // Robot power off -> power on automatically (runs Home first, then Program1)
        if (!robotArm.IsOn)
        {
            Debug.Log("[RobotArmConnector] 로봇 전원이 꺼져 있어 자동으로 켭니다. (Home 후 Program1 실행)");
            robotArm.IsOn = true;
            return;
        }

        if (robotArm.currentState == TaskState.None)
            robotArm.currentState = TaskState.Wait;

        // 로봇팔 실행 상태 확인 (Home 동작 중이면 끝날 때까지 대기)
        if (robotArm.currentState != TaskState.Wait &&
            robotArm.currentState != TaskState.Done)
            return;

        pendingStart = false;
        isRunning = true;
        startedByPLC = true;

        Debug.Log("[RobotArmConnector] 로봇팔 동작 시작");

        Debug.Log(
            $"[RobotArm] 상태: {robotArm.currentState}, " +
            $"Scheduler: {(robotArm.scheduler != null ? "연결됨" : "없음")}, " +
            $"Waypoint: {(robotArm.waypoints != null ? robotArm.waypoints.Length : 0)}개"
        );

        // Program1 실행
        robotArm.RunProgram(1);

        // RunProgram refused (e.g. state changed) -> retry next frame
        if (robotArm.currentState != TaskState.Run)
        {
            isRunning = false;
            startedByPLC = false;
            pendingStart = true;
        }
    }

    // ==========================================
    // 로봇팔 동작 완료
    // =========================================＝
    private void OnRobotArmCompleted()
    {
        Debug.Log("[RobotArmConnector] 로봇팔 완료 이벤트 발생");

        // PLC 명령으로 실행한 경우에만 처리
        if (!startedByPLC)
        {
            Debug.Log("[RobotArmConnector] 수동 실행이므로 배터리팩 전환 생략");
            return;
        }

        if (completionFeedbackRunning)
            return;

        // PLC 실행 상태 초기화
        startedByPLC = false;
        isRunning = false;

        Debug.Log("[RobotArmConnector] PLC 명령 조립 완료");

        // AGV 배터리팩 표시
        BatteryPackVisibility agvCover = FindAGVCoverToShow();
        if (agvCover != null)
            agvCover.SetM524State(true);

        // 커버가 떨어지며 밀린 배터리팩을 처음 위치로 복원
        if (agvCover != null)
            RestoreBatteryPackPose(agvCover.transform.parent);

        // 로봇팔 기존 배터리팩 숨김
        if (robotBatteryVisibility != null)
        {
            robotBatteryVisibility.SetM524State(true);

            // Hide the released cover body too, so its invisible collider does not
            // stay on the AGV and push the battery pack.
            if (hideReleasedCoverBody && robotBatteryVisibility.gameObject != agvCover?.gameObject)
                robotBatteryVisibility.gameObject.SetActive(false);
        }

        // PLC에 M524 완료 신호 전송
        StartCoroutine(SendAssemblyCompleteSignal());
    }
    // ==========================================
    // 배터리팩 처음 위치 저장 / 복원
    // ==========================================

    private Transform FindBatteryPack(Transform agvRoot)
    {
        if (agvRoot == null || string.IsNullOrEmpty(agvBatteryPackName))
            return null;

        return agvRoot.Find(agvBatteryPackName);
    }

    private void SaveBatteryPackPose()
    {
        if (agvBatteryVisibility == null)
            return;

        Transform pack = FindBatteryPack(agvBatteryVisibility.transform.parent);
        if (pack == null)
            return;

        batteryPackLocalPosition = pack.localPosition;
        batteryPackLocalRotation = pack.localRotation;
        batteryPackPoseSaved = true;
    }

    private void RestoreBatteryPackPose(Transform agvRoot)
    {
        if (!batteryPackPoseSaved)
            return;

        Transform pack = FindBatteryPack(agvRoot);
        if (pack == null)
            return;

        pack.localPosition = batteryPackLocalPosition;
        pack.localRotation = batteryPackLocalRotation;

        Rigidbody body = pack.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Debug.Log("[RobotArmConnector] 배터리팩 위치를 처음 위치로 복원");
    }

    // Pick the hidden AGV cover closest to the assembled cover (robot side).
    // AGVs spawned by AGVFiller are clones, so the fixed reference may point to a different AGV.
    private BatteryPackVisibility FindAGVCoverToShow()
    {
        if (agvBatteryVisibility == null || agvBatteryVisibility.targetObject == null)
            return agvBatteryVisibility;

        Vector3 origin = robotBatteryVisibility != null && robotBatteryVisibility.targetObject != null
            ? robotBatteryVisibility.targetObject.transform.position
            : robotArm.transform.position;

        string coverName = agvBatteryVisibility.targetObject.name;
        BatteryPackVisibility best = null;
        float bestDistance = float.MaxValue;

        foreach (BatteryPackVisibility candidate in FindObjectsByType<BatteryPackVisibility>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.targetObject == null ||
                candidate == robotBatteryVisibility ||
                candidate.targetObject.activeSelf ||
                candidate.targetObject.name != coverName ||
                !candidate.gameObject.scene.IsValid())
                continue;

            float distance = (candidate.targetObject.transform.position - origin).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best != null ? best : agvBatteryVisibility;
    }

    // ==========================================
    // M524 완료 신호 전송
    // ==========================================

    private IEnumerator SendAssemblyCompleteSignal()
    {
        if (completionFeedbackRunning)
            yield break;

        completionFeedbackRunning = true;

        // M524 ON
        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address,
            1
        );

        Debug.Log("[RobotArmConnector] M524 ON 전송");

        yield return new WaitForSeconds(feedbackTime);

        // M524 OFF
        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address,
            0
        );

        Debug.Log("[RobotArmConnector] M524 OFF 전송");

        completionFeedbackRunning = false;
    }

    // ==========================================
    // 종료 시 이벤트 해제
    // ==========================================

    private void OnDestroy()
    {
        if (robotArm != null &&
            robotArm.onProgramCompleted != null)
        {
            robotArm.onProgramCompleted.RemoveListener(
                OnRobotArmCompleted
            );
        }
    }
}