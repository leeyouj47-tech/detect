
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

    //26.11.20 LYJ: 변수 추가
    private bool commandReceived = false;
    private bool isRunning = false;
    private bool completionFeedbackRunning = false;

    // PLC M504 명령으로 실행했는지 확인
    private bool startedByPLC = false;
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
            return;
        }

        if (commandReceived || isRunning)
            return;

        if (robotArm == null)
            return;

        // 로봇팔 실행 상태 확인
        if (robotArm.currentState != TaskState.Wait &&
            robotArm.currentState != TaskState.Done)
        {
            Debug.LogWarning(
                "[RobotArmConnector] 로봇팔이 아직 실행 가능한 상태가 아닙니다.");
            return;
        }

        commandReceived = true;
        isRunning = true;

        Debug.Log("[RobotArmConnector] 로봇팔 동작 시작");

        Debug.Log(
            $"[RobotArm] 상태: {robotArm.currentState}, " +
            $"Scheduler: {(robotArm.scheduler != null ? "연결됨" : "없음")}, " +
            $"Waypoint: {(robotArm.waypoints != null ? robotArm.waypoints.Length : 0)}개"
        );

        startedByPLC = true;

        // Program1 실행
        robotArm.RunProgram(1);
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
        if (agvBatteryVisibility != null)
            agvBatteryVisibility.SetM524State(true);

        // 로봇팔 기존 배터리팩 숨김
        if (robotBatteryVisibility != null)
            robotBatteryVisibility.SetM524State(true);

        // PLC에 M524 완료 신호 전송
        StartCoroutine(SendAssemblyCompleteSignal());
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