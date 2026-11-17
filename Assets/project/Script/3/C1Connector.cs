using System.Collections;
using UnityEngine;

public class C1Connector : MXObject
{
    [Header("Robot Arm Program")]
    public C1Program C1Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress startAddress =
        new DeviceAddress("M33");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress completedAddress =
        new DeviceAddress("M73");

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    private bool commandReceived = false;
    private bool isRunning = false;
    private bool completionSent = false;

    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (C1Program == null)
        {
            Debug.LogError(
                "[C1Connector] C1Program이 연결되지 않았습니다."
            );
            return;
        }

        MXRequester.Get.AddDeviceAddress(
            startAddress.address,
            C1StartCommand
        );

        Debug.Log("[C1Connector] PLC 신호 등록 완료");
    }

    // ==========================================
    // PLC -> Unity : M33 시작 명령
    // ==========================================

    private void C1StartCommand(short value)
    {
        // PLC 시작 신호 OFF
        if (value == 0)
        {
            commandReceived = false;
            return;
        }

        // 이미 실행 중이거나 명령을 받은 경우 무시
        if (commandReceived || isRunning)
            return;

        commandReceived = true;
        isRunning = true;
        completionSent = false;

        Debug.Log("[C1Connector] M33 ON - C1 로봇팔 시작");

        // Program1 실행
        C1Program.Program1();
    }

    // ==========================================
    // 로봇팔 실행 완료 확인
    // ==========================================

    private void Update()
    {
        if (!isRunning || completionSent)
            return;

        // OfflineProgram의 실제 명령 큐 실행 완료 확인
        if (C1Program.currentState == TaskState.Done)
        {
            Debug.Log("[C1Connector] C1 로봇팔 동작 완료");

            completionSent = true;

            SendCompletedFeedback();
        }

        // 실행 오류 확인
        if (C1Program.currentState == TaskState.Error)
        {
            Debug.LogError(
                "[C1Connector] 로봇팔 프로그램 실행 오류"
            );
        }
    }

    // ==========================================
    // Unity -> PLC : M73 완료 피드백
    // ==========================================

    private void SendCompletedFeedback()
    {
        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address, 1
        );

        StartCoroutine(ResetCompletedFeedback());
    }

    private IEnumerator ResetCompletedFeedback()
    {
        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address, 0
        );

        isRunning = false;

        Debug.Log("[C1Connector] M73 완료 신호 전송 완료");
    }
}
