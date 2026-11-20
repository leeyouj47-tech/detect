
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

    private bool commandReceived = false;
    private bool isRunning = false;
    private bool isFeedbackRunning = false;

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

        // Program1 실행
        robotArm.RunProgram(1);
    }

    // ==========================================
    // 로봇팔 동작 완료
    // ==========================================

    private void OnRobotArmCompleted()
    {
        if (!isRunning)
            return;

        Debug.Log("[RobotArmConnector] 로봇팔 동작 완료");

        isRunning = false;

        StartCoroutine(CompleteFeedback());
    }

    // ==========================================
    // M524 완료 신호 전송
    // ==========================================

    private IEnumerator CompleteFeedback()
    {
        if (isFeedbackRunning)
            yield break;

        isFeedbackRunning = true;

        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address,
            1
        );

        Debug.Log("[RobotArmConnector] M524 ON");

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address,
            0
        );

        Debug.Log("[RobotArmConnector] M524 OFF");

        isFeedbackRunning = false;
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