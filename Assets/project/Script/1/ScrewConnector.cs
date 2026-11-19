
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ScrewConnector : MXObject
{
    [Header("로봇 프로그램 4개")]
    public OfflineProgram[] screwPrograms = new OfflineProgram[4];

    [Header("PLC -> Unity")]
    public DeviceAddress startAddress =
        new DeviceAddress("M506");

    [Header("Unity -> PLC")]
    public DeviceAddress completedAddress =
        new DeviceAddress("M526");

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    private bool commandReceived = false;
    private bool isRunning = false;
    private bool isFeedbackRunning = false;

    private bool[] completed;
    private int completedCount = 0;

    private void Start()
    {
        if (screwPrograms == null || screwPrograms.Length != 4)
        {
            Debug.LogError(
                "[ScrewConnector] 스크류 프로그램 4개를 등록해야 합니다.");
            return;
        }

        completed = new bool[4];

        // 각 스크류의 완료 이벤트 등록
        for (int i = 0; i < screwPrograms.Length; i++)
        {
            if (screwPrograms[i] == null)
            {
                Debug.LogError(
                    $"[ScrewConnector] Screw{i + 1} 연결 안 됨");
                return;
            }

            if (screwPrograms[i].onProgramCompleted == null)
                screwPrograms[i].onProgramCompleted = new UnityEvent();

            int index = i;

            screwPrograms[i].onProgramCompleted.AddListener(
                () => OnScrewCompleted(index)
            );
        }

        // M506 입력 등록
        MXRequester.Get.AddDeviceAddress(
            startAddress.address,
            OnStartCommand
        );

        Debug.Log("[ScrewConnector] 4개 스크류 PLC 연결 완료");
    }

    // ==========================================
    // M506 : 스크류 4개 동시 실행
    // ==========================================

    private void OnStartCommand(short value)
    {
        Debug.Log($"[PLC 수신] M506 = {value}");

        if (value == 0)
        {
            commandReceived = false;
            return;
        }

        if (commandReceived || isRunning)
            return;

        // 네 스크류 모두 실행 가능한 상태인지 확인
        foreach (OfflineProgram screw in screwPrograms)
        {
            if (screw == null)
                return;

            if (screw.currentState != TaskState.Wait &&
                screw.currentState != TaskState.Done)
            {
                Debug.LogWarning(
                    "[ScrewConnector] 스크류가 아직 실행 가능한 상태가 아닙니다.");
                return;
            }
        }

        commandReceived = true;
        isRunning = true;
        completedCount = 0;

        for (int i = 0; i < completed.Length; i++)
            completed[i] = false;

        Debug.Log("[ScrewConnector] 스크류 4개 동시 실행 시작");

        // 네 개의 프로그램을 연속 호출
        // 각 프로그램은 독립적인 명령 큐를 실행
        for (int i = 0; i < screwPrograms.Length; i++)
        {
            OfflineProgram screw = screwPrograms[i];

            if (screw == null)
            {
                Debug.LogError($"Screw{i + 1} 연결 안 됨");
                continue;
            }

            Debug.Log(
                $"[Screw{i + 1}] " +
                $"현재 상태: {screw.currentState}, " +
                $"Scheduler: {(screw.scheduler != null ? "연결됨" : "없음")}, " +
                $"Waypoint 개수: {(screw.waypoints != null ? screw.waypoints.Length : 0)}"
            );

            screw.RunProgram(1);
        }
    }

    // ==========================================
    // 각 스크류 프로그램 완료 확인
    // ==========================================

    private void OnScrewCompleted(int index)
    {
        if (!isRunning || completed[index])
            return;

        completed[index] = true;
        completedCount++;

        Debug.Log(
            $"[ScrewConnector] Screw{index + 1} 완료 " +
            $"({completedCount}/4)"
        );

        // 4개 모두 완료된 경우
        if (completedCount == 4)
        {
            Debug.Log("[ScrewConnector] 스크류 4개 전체 작업 완료");

            isRunning = false;

            StartCoroutine(CompleteFeedback());
        }
    }

    // ==========================================
    // M526 완료 신호 전송
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

        Debug.Log("[ScrewConnector] M526 ON");

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            completedAddress.address,
            0
        );

        Debug.Log("[ScrewConnector] M526 OFF");

        isFeedbackRunning = false;
    }
}
