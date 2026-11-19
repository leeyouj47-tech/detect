
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PackConnector : MXObject
{
    [Header("배터리팩 관련")]
    public PackController packController;

    // ==========================================
    // AGV 이동 Waypoint ID
    // ==========================================

    [Header("AGV 이동 Waypoint ID")]
    public int inspectionWaypoint = 2;  // M510

    // ==========================================
    // AGV 이동 상태
    // ==========================================

    private bool agvBusy = false;
    private bool[] moveCommandReceived = new bool[2];
    private DeviceAddress currentArrivedAddress;

    // ==========================================
    // PLC -> Unity : AGV 이동 명령
    // ==========================================

    [Header("PLC -> Unity : AGV 이동 명령")]
    public DeviceAddress inspectionMoveAddress = new DeviceAddress("M510");
    public DeviceAddress nextAssemblyMoveAddress = new DeviceAddress("M503");


    // ==========================================
    // Unity -> PLC : AGV 도착 신호
    // ==========================================

    [Header("Unity -> PLC : AGV 도착 신호")]

    public DeviceAddress inspectionArrivedAddress = new DeviceAddress("M540");
    public DeviceAddress nextAssemblyArrivedAddress = new DeviceAddress("M523");

    [Header("공통 설정")]
    public float feedbackTime = 2.0f;

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

        // PLC 이동 명령 등록
        MXRequester.Get.AddDeviceAddress(
            inspectionMoveAddress.address, InspectionMove);

        MXRequester.Get.AddDeviceAddress(
            nextAssemblyMoveAddress.address, NextAssemblyMove);

        Debug.Log("[PackConnector] PLC 신호 등록 완료");
    }

    private void NextAssemblyMove(short value)
    {
        Debug.Log($"[PLC 수신] M503 = {value}");

        MoveCommand(
            value,
            1,
            3,
            nextAssemblyArrivedAddress
        );
    }
    // ==========================================
    // AGV 이동 공통 처리
    // ==========================================


    private void MoveCommand(
        short value,
        int commandIndex,
        int waypointID,
        DeviceAddress arrivedAddress)
    {
        // PLC 명령 OFF 시 재수신 가능하도록 초기화
        if (value == 0)
        {
            moveCommandReceived[commandIndex] = false;
            return;
        }

        // 이미 처리한 명령이면 무시
        if (moveCommandReceived[commandIndex])
        {
            Debug.Log(
                $"[PackConnector] 이미 처리한 명령입니다. Index: {commandIndex}");
            return;
        }

        // 목적지 확인
        if (waypointID < 0)
        {
            Debug.LogError("[PackConnector] 목적지 Waypoint가 잘못되었습니다.");
            return;
        }

        // 이동 중인지 확인
        if (agvBusy)
        {
            Debug.LogWarning("[PackConnector] 이미 이동 중입니다.");
            return;
        }

        // Controller 확인
        if (packController == null)
        {
            Debug.LogError("[PackConnector] PackController가 연결되지 않았습니다.");
            return;
        }

        Debug.Log(
            $"[PackConnector] 이동 명령 수신 / Waypoint: {waypointID}");

        // ★ 이동 전에 도착 주소와 상태를 먼저 설정
        currentArrivedAddress = arrivedAddress;
        agvBusy = true;

        // 이동 명령 실행
        currentArrivedAddress = arrivedAddress;
        agvBusy = true;

        bool success = packController.Go(waypointID);

        if (!success)
        {
            currentArrivedAddress = null;
            agvBusy = false;
            return;
        }

        moveCommandReceived[commandIndex] = true;

        Debug.Log($"[PackConnector] Go 실행 결과: {success}");

        if (!success)
        {
            Debug.LogError(
                $"[PackConnector] 경로 탐색 실패 / Waypoint: {waypointID}");

            currentArrivedAddress = null;
            agvBusy = false;
            return;
        }

        moveCommandReceived[commandIndex] = true;

        Debug.Log(
            $"[PackConnector] 이동 시작 성공 / 목적지: Waypoint {waypointID}");
    }



    // ==========================================
    // AGV 이동 명령별 처리
    // ==========================================

    // M510 : 배터리팩 커버 불량검사라인 이동
    private void InspectionMove(short value)
    {
        Debug.Log(
            $"[PLC 수신] M510 = {value}");

        MoveCommand(
            value,
            0,
            inspectionWaypoint,
            inspectionArrivedAddress);
    }

    // ==========================================
    // AGV 이동 완료 처리
    // ==========================================

    private void OnCompletedMove()
    {
        if (currentArrivedAddress == null)
            return;

        Debug.Log(
            "[PackConnector] AGV 목적지 도착");

        StartCoroutine(CompleteMoveFeedback());
    }


    private IEnumerator CompleteMoveFeedback()
    {
        DeviceAddress arrivedAddress = currentArrivedAddress;


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


        currentArrivedAddress = null;

        agvBusy = false;

        Debug.Log(
            "[PackConnector] AGV 도착 피드백 전송 완료");
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
