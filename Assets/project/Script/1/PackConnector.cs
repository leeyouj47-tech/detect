
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PackConnector : MXObject
{
    [Header("배터리팩 관련")]
    public PackController packController;

    // ==========================================
    // 이동 Waypoint ID
    // ==========================================

    [Header("배터리팩 이동 Waypoint ID")]
    public int inspectionWaypoint = 2;   // M510
    public int nextAssemblyWaypoint = 3; // M503

    // ==========================================
    // 이동 상태
    // ==========================================

    private bool agvBusy = false;

    // 0: M510 / 1: M503
    private bool[] moveCommandReceived = new bool[2];

    private DeviceAddress currentArrivedAddress;

    // ==========================================
    // PLC -> Unity : 이동 명령
    // ==========================================

    [Header("PLC -> Unity : 이동 명령")]
    public DeviceAddress inspectionMoveAddress =
        new DeviceAddress("M510");

    public DeviceAddress nextAssemblyMoveAddress =
        new DeviceAddress("M503");

    // ==========================================
    // Unity -> PLC : 도착 신호
    // ==========================================

    [Header("Unity -> PLC : 도착 신호")]
    public DeviceAddress inspectionArrivedAddress =
        new DeviceAddress("M540");

    public DeviceAddress nextAssemblyArrivedAddress =
        new DeviceAddress("M523");

    [Header("공통 설정")]
    public float feedbackTime = 2.0f;


    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (packController == null)
        {
            Debug.LogError(
                "[PackConnector] PackController가 연결되지 않았습니다.");
            return;
        }

        if (packController.onCompleteMove == null)
            packController.onCompleteMove = new UnityEvent();

        packController.onCompleteMove.AddListener(OnCompletedMove);

        MXRequester.Get.AddDeviceAddress(
            inspectionMoveAddress.address,
            InspectionMove);

        MXRequester.Get.AddDeviceAddress(
            nextAssemblyMoveAddress.address,
            NextAssemblyMove);

        Debug.Log("[PackConnector] PLC 신호 등록 완료");
    }


    // ==========================================
    // M510 : 검사라인 이동
    // ==========================================

    private void InspectionMove(short value)
    {
        Debug.Log($"[PLC 수신] M510 = {value}");

        MoveCommand(
            value,
            0,
            inspectionWaypoint,
            inspectionArrivedAddress
        );
    }


    // ==========================================
    // M503 : 다음 조립공정 이동
    // ==========================================

    private void NextAssemblyMove(short value)
    {
        Debug.Log($"[PLC 수신] M503 = {value}");

        MoveCommand(
            value,
            1,
            nextAssemblyWaypoint,
            nextAssemblyArrivedAddress
        );
    }


    // ==========================================
    // 공통 이동 처리
    // ==========================================

    private void MoveCommand(
        short value,
        int commandIndex,
        int waypointID,
        DeviceAddress arrivedAddress)
    {
        // PLC 명령 OFF
        if (value == 0)
        {
            moveCommandReceived[commandIndex] = false;

            Debug.Log(
                $"[PackConnector] 이동 명령 초기화 / Index: {commandIndex}");

            return;
        }

        // 이미 처리한 명령이면 무시
        if (moveCommandReceived[commandIndex])
        {
            Debug.Log(
                $"[PackConnector] 이미 처리한 명령입니다. Index: {commandIndex}");
            return;
        }

        // 이동 중인지 확인
        if (agvBusy)
        {
            Debug.LogWarning(
                "[PackConnector] 현재 배터리팩 이동 중입니다.");
            return;
        }

        if (packController == null)
        {
            Debug.LogError(
                "[PackConnector] PackController가 연결되지 않았습니다.");
            return;
        }

        if (waypointID < 0)
        {
            Debug.LogError(
                "[PackConnector] 목적지 Waypoint가 잘못되었습니다.");
            return;
        }

        Debug.Log(
            $"[PackConnector] 이동 명령 실행 / 목적지: Waypoint {waypointID}");

        // 이동 명령 실행
        bool success = packController.Go(waypointID);

        if (!success)
        {
            Debug.LogWarning(
                $"[PackConnector] 이동 시작 실패 / 목적지: {waypointID}");
            return;
        }

        // 이동 성공 시에만 상태 변경
        currentArrivedAddress = arrivedAddress;
        agvBusy = true;
        moveCommandReceived[commandIndex] = true;

        Debug.Log(
            $"[PackConnector] 이동 시작 성공 / 목적지: Waypoint {waypointID}");
    }


    // ==========================================
    // 이동 완료
    // ==========================================

    private void OnCompletedMove()
    {
        if (currentArrivedAddress == null)
        {
            Debug.LogWarning(
                "[PackConnector] 도착 주소가 없습니다. 피드백 전송 생략");
            return;
        }

        Debug.Log("[PackConnector] 배터리팩 목적지 도착");

        StartCoroutine(CompleteMoveFeedback());
    }


    // ==========================================
    // 도착 피드백
    // ==========================================

    private IEnumerator CompleteMoveFeedback()
    {
        DeviceAddress arrivedAddress = currentArrivedAddress;

        if (arrivedAddress == null)
            yield break;

        // 도착 신호 ON
        MXRequester.Get.AddSetDeviceRequest(
            arrivedAddress.address,
            1);

        Debug.Log(
            $"[PackConnector] 도착 신호 ON: {arrivedAddress.address}");

        yield return new WaitForSeconds(feedbackTime);

        // 도착 신호 OFF
        MXRequester.Get.AddSetDeviceRequest(
            arrivedAddress.address,
            0);

        Debug.Log(
            $"[PackConnector] 도착 신호 OFF: {arrivedAddress.address}");

        currentArrivedAddress = null;
        agvBusy = false;

        Debug.Log(
            "[PackConnector] 도착 피드백 전송 완료");
    }


    // ==========================================
    // 종료 처리
    // ==========================================

    private void OnDestroy()
    {
        if (packController != null &&
            packController.onCompleteMove != null)
        {
            packController.onCompleteMove.RemoveListener(
                OnCompletedMove);
        }
    }
}
