
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Pr3Connector : MXObject
{
    [Header("AGV 관련")]
    public Pr3Controller agvController;

    [Header("AGV 이동 Waypoint ID")]
    public int firstLineWaypoint = 2;       // M10 -> WP2
    public int secondLineWaypoint = 3;      // M16 -> WP3
    public int firstInspectionWaypoint = 4; // M20 -> WP4
    public int thirdLineWaypoint = 5;       // M22 -> WP5
    public int secondInspectionWaypoint = 6;// M24 -> WP6
    public int fourthLineWaypoint = 7;      // M26 -> WP7
    public int thirdInspectionWaypoint = 8; // M29 -> WP8
    public int finalDestinationWaypoint = 9;    //M31 -> WP9

    private bool agvBusy;
    private bool[] moveCommandReceived = new bool[7];
    private DeviceAddress currentArrivedAddress;

    [Header("PLC -> Unity : AGV 이동 명령")]
    public DeviceAddress firstLineMoveAddress = new DeviceAddress("1차 라인 스키드 이동");
    public DeviceAddress secondLineMoveAddress = new DeviceAddress("2차 라인 스키드 이동");
    public DeviceAddress firstInspectionLineMoveAddress = new DeviceAddress("1차 불량판정라인 스키드 전진");
    public DeviceAddress thirdLineMoveAddress = new DeviceAddress("3차 라인 스키드 이동");
    public DeviceAddress secondInspectionLineMoveAddress = new DeviceAddress("2차 불량판정 라인 스키드 이동");
    public DeviceAddress fourthLineMoveAddress = new DeviceAddress("4차 라인 스키드 이동");
    public DeviceAddress thirdInspectionLineMoveAddress = new DeviceAddress("3차 불량판정라인 스키드 이동");
    public DeviceAddress finalDestinationMoveAddress = new DeviceAddress("최종 스키드 이동");

    [Header("PLC -> Unity : 작업 명령")]
    public DeviceAddress cpMoveAddress = new DeviceAddress("CP 이동");
    public DeviceAddress xyzMove1Address = new DeviceAddress("XYZ 갠트리 이동 1");
    public DeviceAddress cpAssemblyAddress = new DeviceAddress("CP 조립 애니메이션");
    public DeviceAddress xyzMove2Address = new DeviceAddress("XYZ 갠트리 이동 2");
    public DeviceAddress femMoveAddress = new DeviceAddress("FEM 이동");
    public DeviceAddress femAssemblyAddress = new DeviceAddress("FEM 조립 애니메이션");
    public DeviceAddress firstInspectionAddress = new DeviceAddress("1차 불량판정");
    public DeviceAddress windshieldAddress = new DeviceAddress("앞유리 부착");
    public DeviceAddress secondInspectionAddress = new DeviceAddress("2차 불량판정");
    public DeviceAddress doorAssemblyAddress = new DeviceAddress("차문 조립");
    public DeviceAddress doorWeldingAddress = new DeviceAddress("차문 용접");
    public DeviceAddress thirdInspectionAddress = new DeviceAddress("3차 불량판정");

    [Header("Unity 작업 실행 이벤트")]
    public UnityEvent onCPMove;
    public UnityEvent onXYZMove1;
    public UnityEvent onCPAssembly;
    public UnityEvent onXYZMove2;
    public UnityEvent onFEMMove;
    public UnityEvent onFEMAssembly;
    public UnityEvent onFirstInspection;
    public UnityEvent onWindshield;
    public UnityEvent onSecondInspection;
    public UnityEvent onDoorAssembly;
    public UnityEvent onDoorWelding;
    public UnityEvent onThirdInspection;

    private bool[] workCommandReceived = new bool[12];

    [Header("Unity -> PLC : 완료 및 도착 신호")]
    public DeviceAddress firstLineArrivedAddress = new DeviceAddress("1차 라인 스키드 도착");
    public DeviceAddress cpMoveCompletedAddress = new DeviceAddress("CP 이동 완료");
    public DeviceAddress xyzMove1CompletedAddress = new DeviceAddress("XYZ 갠트리 이동 완료 1");
    public DeviceAddress cpAssemblyCompletedAddress = new DeviceAddress("CP 조립 완료");
    public DeviceAddress secondLineArrivedAddress = new DeviceAddress("2차 라인 스키드 도착");
    public DeviceAddress xyzMove2CompletedAddress = new DeviceAddress("2차 라인 갠트리 이동 완료");
    public DeviceAddress femMoveCompletedAddress = new DeviceAddress("FEM 이동 완료");
    public DeviceAddress femAssemblyCompletedAddress = new DeviceAddress("FEM 조립 완료");
    public DeviceAddress firstInspectionLineArrivedAddress = new DeviceAddress("1차 불량판정 라인 도착");
    public DeviceAddress firstInspectionCompletedAddress = new DeviceAddress("1차 불량판정 완료");
    public DeviceAddress thirdLineArrivedAddress = new DeviceAddress("3차 라인 스키드 도착");
    public DeviceAddress windshieldCompletedAddress = new DeviceAddress("앞유리 부착 완료");
    public DeviceAddress secondInspectionLineArrivedAddress = new DeviceAddress("2차 불량판정라인 도착");
    public DeviceAddress secondInspectionCompletedAddress = new DeviceAddress("2차 불량판정 완료");
    public DeviceAddress fourthLineArrivedAddress = new DeviceAddress("4차 라인 스키드 도착");
    public DeviceAddress doorAssemblyCompletedAddress = new DeviceAddress("차문 조립 완료");
    public DeviceAddress doorWeldingCompletedAddress = new DeviceAddress("차문 용접 완료");
    public DeviceAddress thirdInspectionLineArrivedAddress = new DeviceAddress("3차 불량판정라인 도착");
    public DeviceAddress thirdInspectionCompletedAddress = new DeviceAddress("3차 불량판정 완료");
    public DeviceAddress finalDestinationAddress = new DeviceAddress("최종도착");

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (agvController == null)
        {
            Debug.LogError("[Pr3Connector] Pr3Controller가 연결되지 않았습니다.");
            return;
        }

        if (agvController.onCompleteMove == null)
            agvController.onCompleteMove = new UnityEvent();

        agvController.onCompleteMove.AddListener(OnCompletedMove);

        // AGV 이동 명령 등록
        MXRequester.Get.AddDeviceAddress(firstLineMoveAddress.address, FirstLineMove);
        MXRequester.Get.AddDeviceAddress(secondLineMoveAddress.address, SecondLineMove);
        MXRequester.Get.AddDeviceAddress(firstInspectionLineMoveAddress.address, FirstInspectionLineMove);
        MXRequester.Get.AddDeviceAddress(thirdLineMoveAddress.address, ThirdLineMove);
        MXRequester.Get.AddDeviceAddress(secondInspectionLineMoveAddress.address, SecondInspectionLineMove);
        MXRequester.Get.AddDeviceAddress(fourthLineMoveAddress.address, FourthLineMove);
        MXRequester.Get.AddDeviceAddress(thirdInspectionLineMoveAddress.address, ThirdInspectionLineMove);
        MXRequester.Get.AddDeviceAddress(finalDestinationMoveAddress.address, FinalDestinationMove);

        // 작업 명령 등록
        MXRequester.Get.AddDeviceAddress(cpMoveAddress.address, CPMoveCommand);
        MXRequester.Get.AddDeviceAddress(xyzMove1Address.address, XYZMove1Command);
        MXRequester.Get.AddDeviceAddress(cpAssemblyAddress.address, CPAssemblyCommand);
        MXRequester.Get.AddDeviceAddress(xyzMove2Address.address, XYZMove2Command);
        MXRequester.Get.AddDeviceAddress(femMoveAddress.address, FEMMoveCommand);
        MXRequester.Get.AddDeviceAddress(femAssemblyAddress.address, FEMAssemblyCommand);
        MXRequester.Get.AddDeviceAddress(firstInspectionAddress.address, FirstInspectionCommand);
        MXRequester.Get.AddDeviceAddress(windshieldAddress.address, WindshieldCommand);
        MXRequester.Get.AddDeviceAddress(secondInspectionAddress.address, SecondInspectionCommand);
        MXRequester.Get.AddDeviceAddress(doorAssemblyAddress.address, DoorAssemblyCommand);
        MXRequester.Get.AddDeviceAddress(doorWeldingAddress.address, DoorWeldingCommand);
        MXRequester.Get.AddDeviceAddress(thirdInspectionAddress.address, ThirdInspectionCommand);

        Debug.Log("[Pr3Connector] PLC 신호 등록 완료");
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
        if (value == 0)
        {
            moveCommandReceived[commandIndex] = false;
            return;
        }

        if (moveCommandReceived[commandIndex])
            return;

        if (waypointID < 0)
        {
            Debug.LogWarning("[Pr3Connector] Waypoint가 지정되지 않았습니다.");
            return;
        }

        if (agvBusy)
        {
            Debug.LogWarning(
                $"[Pr3Connector] AGV 이동 중입니다. 목적지: WP{waypointID}"
            );
            return;
        }

        if (agvController == null)
        {
            Debug.LogError("[Pr3Connector] Pr3Controller가 없습니다.");
            return;
        }

        Debug.Log(
            $"[Pr3Connector] AGV 이동 명령 / WP{waypointID}"
        );

        bool success = agvController.Go(waypointID);

        if (!success)
        {
            Debug.LogError(
                $"[Pr3Connector] 경로 탐색 실패 / WP{waypointID}"
            );
            return;
        }

        moveCommandReceived[commandIndex] = true;
        agvBusy = true;
        currentArrivedAddress = arrivedAddress;
    }

    // ==========================================
    // AGV 이동 명령별 처리
    // ==========================================

    // M10 -> WP2
    private void FirstLineMove(short value)
    {
        MoveCommand(value, 0, firstLineWaypoint, firstLineArrivedAddress);
    }

    // M16 -> WP3
    private void SecondLineMove(short value)
    {
        MoveCommand(value, 1, secondLineWaypoint, secondLineArrivedAddress);
    }

    // M20 -> WP4
    private void FirstInspectionLineMove(short value)
    {
        MoveCommand(value, 2, firstInspectionWaypoint, firstInspectionLineArrivedAddress);
    }

    // M22 -> WP5
    private void ThirdLineMove(short value)
    {
        MoveCommand(value, 3, thirdLineWaypoint, thirdLineArrivedAddress);
    }

    // M24 -> WP6
    private void SecondInspectionLineMove(short value)
    {
        MoveCommand(value, 4, secondInspectionWaypoint, secondInspectionLineArrivedAddress);
    }

    // M26 -> WP7
    private void FourthLineMove(short value)
    {
        MoveCommand(value, 5, fourthLineWaypoint, fourthLineArrivedAddress);
    }

    // M29 -> WP8
    private void ThirdInspectionLineMove(short value)
    {
        MoveCommand(value, 6, thirdInspectionWaypoint, thirdInspectionLineArrivedAddress);
    }

    //M31 -> WP9
    private void FinalDestinationMove(short value)
    {
        MoveCommand(value, 7, finalDestinationWaypoint, finalDestinationAddress);
    }

    // ==========================================
    // AGV 이동 완료 처리
    // ==========================================

    private void OnCompletedMove()
    {
        if (currentArrivedAddress == null)
            return;

        StartCoroutine(CompleteMoveFeedback());
    }

    private IEnumerator CompleteMoveFeedback()
    {
        DeviceAddress arrivedAddress = currentArrivedAddress;

        MXRequester.Get.AddSetDeviceRequest(arrivedAddress.address, 1);

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(arrivedAddress.address, 0);

        currentArrivedAddress = null;
        agvBusy = false;

        Debug.Log("[Pr3Connector] AGV 도착 피드백 전송 완료");
    }

    // ==========================================
    // 작업 명령 공통 처리
    // ==========================================

    private void ExecuteWorkCommand(
        short value,
        int index,
        UnityEvent action,
        string workName)
    {
        if (value == 0)
        {
            workCommandReceived[index] = false;
            return;
        }

        if (workCommandReceived[index])
            return;

        workCommandReceived[index] = true;

        Debug.Log($"[Pr3Connector] 작업 명령 수신: {workName}");

        if (action != null)
            action.Invoke();
        else
            Debug.LogWarning(
                $"[Pr3Connector] {workName} 이벤트가 연결되지 않았습니다."
            );
    }

    // ==========================================
    // PLC 작업 명령별 처리
    // ==========================================

    private void CPMoveCommand(short value)
    {
        ExecuteWorkCommand(value, 0, onCPMove, "CP 이동");
    }

    private void XYZMove1Command(short value)
    {
        ExecuteWorkCommand(value, 1, onXYZMove1, "XYZ 갠트리 이동(1)");
    }

    private void CPAssemblyCommand(short value)
    {
        ExecuteWorkCommand(value, 2, onCPAssembly, "CP 조립");
    }

    private void XYZMove2Command(short value)
    {
        ExecuteWorkCommand(value, 3, onXYZMove2, "XYZ 갠트리 이동(2)");
    }

    private void FEMMoveCommand(short value)
    {
        ExecuteWorkCommand(value, 4, onFEMMove, "FEM 이동");
    }

    private void FEMAssemblyCommand(short value)
    {
        ExecuteWorkCommand(value, 5, onFEMAssembly, "FEM 조립");
    }

    private void FirstInspectionCommand(short value)
    {
        ExecuteWorkCommand(value, 6, onFirstInspection, "1차 불량판정");
    }

    private void WindshieldCommand(short value)
    {
        ExecuteWorkCommand(value, 7, onWindshield, "앞유리 부착");
    }

    private void SecondInspectionCommand(short value)
    {
        ExecuteWorkCommand(value, 8, onSecondInspection, "2차 불량판정");
    }

    private void DoorAssemblyCommand(short value)
    {
        ExecuteWorkCommand(value, 9, onDoorAssembly, "차문 조립");
    }

    private void DoorWeldingCommand(short value)
    {
        ExecuteWorkCommand(value, 10, onDoorWelding, "차문 용접");
    }

    private void ThirdInspectionCommand(short value)
    {
        ExecuteWorkCommand(value, 11, onThirdInspection, "3차 불량판정");
    }

    // ==========================================
    // Unity -> PLC 완료 피드백
    // ==========================================

    private void SendFeedback(DeviceAddress address)
    {
        MXRequester.Get.AddSetDeviceRequest(address.address, 1);
        StartCoroutine(ResetFeedback(address));
    }

    private IEnumerator ResetFeedback(DeviceAddress address)
    {
        yield return new WaitForSeconds(feedbackTime);
        MXRequester.Get.AddSetDeviceRequest(address.address, 0);
    }

    // 이동 도착 피드백
    public void OnFirstLineArrived() { SendFeedback(firstLineArrivedAddress); }
    public void OnSecondLineArrived() { SendFeedback(secondLineArrivedAddress); }
    public void OnFirstInspectionLineArrived() { SendFeedback(firstInspectionLineArrivedAddress); }
    public void OnThirdLineArrived() { SendFeedback(thirdLineArrivedAddress); }
    public void OnSecondInspectionLineArrived() { SendFeedback(secondInspectionLineArrivedAddress); }
    public void OnFourthLineArrived() { SendFeedback(fourthLineArrivedAddress); }
    public void OnThirdInspectionLineArrived() { SendFeedback(thirdInspectionLineArrivedAddress); }
    public void OnfinalDestinationMoveAddress() { SendFeedback(finalDestinationMoveAddress); }

    // 작업 완료 피드백
    public void OnCPMoveCompleted() { SendFeedback(cpMoveCompletedAddress); }
    public void OnXYZMove1Completed() { SendFeedback(xyzMove1CompletedAddress); }
    public void OnCPAssemblyCompleted() { SendFeedback(cpAssemblyCompletedAddress); }
    public void OnXYZMove2Completed() { SendFeedback(xyzMove2CompletedAddress); }
    public void OnFEMMoveCompleted() { SendFeedback(femMoveCompletedAddress); }
    public void OnFEMAssemblyCompleted() { SendFeedback(femAssemblyCompletedAddress); }
    public void OnFirstInspectionCompleted() { SendFeedback(firstInspectionCompletedAddress); }
    public void OnWindshieldCompleted() { SendFeedback(windshieldCompletedAddress); }
    public void OnSecondInspectionCompleted() { SendFeedback(secondInspectionCompletedAddress); }
    public void OnDoorAssemblyCompleted() { SendFeedback(doorAssemblyCompletedAddress); }
    public void OnDoorWeldingCompleted() { SendFeedback(doorWeldingCompletedAddress); }
    public void OnThirdInspectionCompleted() { SendFeedback(thirdInspectionCompletedAddress); }
    public void OnfinalDestinationAddress() { SendFeedback(finalDestinationAddress); }

    // ==========================================
    // 종료 처리
    // ==========================================

    private void OnDestroy()
    {
        if (agvController != null &&
            agvController.onCompleteMove != null)
        {
            agvController.onCompleteMove.RemoveListener(OnCompletedMove);
        }
    }
}
