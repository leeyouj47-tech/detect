
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Pr1Connector : MXObject
{
    [Header("AGV 관련")]
    public Pr1Controller agvController;

    [Header("AGV 이동 Waypoint ID")]
    public int firstDefectWaypoint = 1;       // M500
    public int batteryLineWaypoint = 2;      // M502
    //public int coverLineWaypoint = -1;       // M503 (미정)
    public int assemblyWaypoint = 3;         // M505
    public int secondDefectWaypoint = 4;     // M507
    public int secondProcessWaypoint = 5;    // M509

    private bool agvBusy = false;
    private bool[] moveCommandReceived = new bool[6];
    private DeviceAddress currentArrivedAddress;

    [Header("PLC -> Unity : AGV 이동 명령")]
    public DeviceAddress firstDefectMoveAddress = new DeviceAddress("AGV 1차 불량판정 위치이동");
    public DeviceAddress batteryLineMoveAddress = new DeviceAddress("AGV 배터리팩 조립라인 이동");
    public DeviceAddress coverLineMoveAddress = new DeviceAddress("배터리팩 커버 조립라인 이동");
    public DeviceAddress assemblyMoveAddress = new DeviceAddress("AGV 조립공정 이동");
    public DeviceAddress secondDefectMoveAddress = new DeviceAddress("AGV 2차 불량판정 위치 이동");
    public DeviceAddress secondProcessMoveAddress = new DeviceAddress("AGV 2차 공정 이동");

    [Header("PLC -> Unity : 작업 명령")]
    public DeviceAddress firstInspectionAddress = new DeviceAddress("1차 불량판정 실행");
    public DeviceAddress coverAssemblyAddress = new DeviceAddress("로봇팔 커버 조립");
    public DeviceAddress batteryAssemblyAddress = new DeviceAddress("로봇팔 배터리팩 조립");
    public DeviceAddress secondInspectionAddress = new DeviceAddress("2차 불량판정 실행");

    [Header("작업 실행 이벤트")]
    public UnityEvent onFirstInspection;
    public UnityEvent onCoverAssembly;
    public UnityEvent onBatteryAssembly;
    public UnityEvent onSecondInspection;

    private bool[] workCommandReceived = new bool[4];

    [Header("Unity -> PLC : 공정 완료 및 도착 신호")]
    public DeviceAddress firstDefectArrivedAddress = new DeviceAddress("1차 불량판정 위치 도착");
    public DeviceAddress firstInspectionCompletedAddress = new DeviceAddress("1차 불량판정 완료");
    public DeviceAddress batteryLineArrivedAddress = new DeviceAddress("AGV 배터리팩 조립라인 도착");
    public DeviceAddress coverLineArrivedAddress = new DeviceAddress("커버 팔레트 조립라인 도착 완료");
    public DeviceAddress coverAssemblyCompletedAddress = new DeviceAddress("커버 조립 완료");
    public DeviceAddress assemblyArrivedAddress = new DeviceAddress("AGV 조립공정 도착");
    public DeviceAddress batteryAssemblyCompletedAddress = new DeviceAddress("배터리팩 조립 완료");
    public DeviceAddress secondDefectArrivedAddress = new DeviceAddress("2차 불량판정 위치 도착");
    public DeviceAddress secondInspectionCompletedAddress = new DeviceAddress("2차 불량판정 완료");
    public DeviceAddress secondProcessArrivedAddress = new DeviceAddress("2차 공정 도착");

    [Header("불량판정 결과")]
    public DeviceAddress firstGoodAddress = new DeviceAddress("1차 정상품");
    public DeviceAddress firstBadAddress = new DeviceAddress("1차 불량품");
    public DeviceAddress secondGoodAddress = new DeviceAddress("2차 정상품");
    public DeviceAddress secondBadAddress = new DeviceAddress("2차 불량품");

    [Header("공정 상태")]
    public DeviceAddress processStartAddress = new DeviceAddress("1차 공정 시작 요청");
    public DeviceAddress autoRunAddress = new DeviceAddress("자동운전 상태");
    public DeviceAddress processResetAddress = new DeviceAddress("공정 초기화 요청");
    public DeviceAddress processPauseAddress = new DeviceAddress("공정 일시정지");
    public DeviceAddress processErrorAddress = new DeviceAddress("공정 이상발생");
    public DeviceAddress processCompletedAddress = new DeviceAddress("1차 공정 완료");
    public DeviceAddress goodStatusAddress = new DeviceAddress("정상품 판정 상태");
    public DeviceAddress badStatusAddress = new DeviceAddress("불량품 판정 상태");
    public DeviceAddress autoPermitAddress = new DeviceAddress("자동운전 시작 및 허가");
    public DeviceAddress resetCompletedAddress = new DeviceAddress("공정 초기화 완료");
    public DeviceAddress safetyAddress = new DeviceAddress("비상정지 및 안전조건 정상");
    public DeviceAddress simulationReadyAddress = new DeviceAddress("Unity 통신 및 시뮬 준비 완료");

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;


    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (agvController == null)
        {
            Debug.LogError("[Pr1Connector] AGVController가 연결되지 않았습니다.");
            return;
        }

        if (agvController.onCompleteMove == null)
            agvController.onCompleteMove = new UnityEvent();

        agvController.onCompleteMove.AddListener(OnCompletedMove);

        // AGV 이동 명령 등록
        MXRequester.Get.AddDeviceAddress(firstDefectMoveAddress.address, FirstDefectMove);
        MXRequester.Get.AddDeviceAddress(batteryLineMoveAddress.address, BatteryLineMove);
        //배터리팩 커버 이동위치 정하지 않아 주석처리
        /*MXRequester.Get.AddDeviceAddress(
            coverLineMoveAddress.address, CoverLineMove);*/
        MXRequester.Get.AddDeviceAddress(assemblyMoveAddress.address, AssemblyMove);
        MXRequester.Get.AddDeviceAddress(secondDefectMoveAddress.address, SecondDefectMove);
        MXRequester.Get.AddDeviceAddress(secondProcessMoveAddress.address, SecondProcessMove);


        // 작업 명령 등록
        MXRequester.Get.AddDeviceAddress(firstInspectionAddress.address, FirstInspectionCommand);
        MXRequester.Get.AddDeviceAddress(coverAssemblyAddress.address, CoverAssemblyCommand);
        MXRequester.Get.AddDeviceAddress(batteryAssemblyAddress.address, BatteryAssemblyCommand);
        MXRequester.Get.AddDeviceAddress(secondInspectionAddress.address, SecondInspectionCommand);


        Debug.Log("[Pr1Connector] PLC 신호 등록 완료");
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
            Debug.Log($"[AGV] 이미 처리한 명령입니다. Index: {commandIndex}");
            return;
        }

        // 목적지 확인
        if (waypointID < 0)
        {
            Debug.LogWarning("[AGV] 목적지 Waypoint가 지정되지 않았습니다.");
            return;
        }

        // AGV 이동 중인지 확인
        if (agvBusy)
        {
            Debug.LogWarning(
                $"[AGV] 이미 이동 중입니다. 명령 무시 / 목적지: {waypointID}");
            return;
        }

        // Controller 연결 확인
        if (agvController == null)
        {
            Debug.LogError("[AGV] Pr1Controller가 연결되지 않았습니다.");
            return;
        }

        Debug.Log($"[AGV] 이동 명령 시작 / 목적지: Waypoint {waypointID}");

        // 경로 탐색 및 이동 명령
        bool success = agvController.Go(waypointID);

        Debug.Log($"[AGV] Go 실행 결과: {success}");

        if (!success)
        {
            Debug.LogError(
                $"[AGV] 경로 탐색 실패 / 목적지: Waypoint {waypointID}");
            return;
        }

        // 이동 명령 성공 후 상태 변경
        moveCommandReceived[commandIndex] = true;
        agvBusy = true;
        currentArrivedAddress = arrivedAddress;

        Debug.Log(
            $"[AGV] 이동 시작 완료 / 목적지: Waypoint {waypointID}");
    }
    // ==========================================
    // AGV 이동 명령별 처리
    // ==========================================

    // M500 : 1차 불량판정 위치 이동
    private void FirstDefectMove(short value)
    {
        MoveCommand(
            value, 0, firstDefectWaypoint,
            firstDefectArrivedAddress);
    }

    //190~205 줄은 추후에 M503의 위치가 정해지고, 주석처리 풀기
    // M502 : 배터리팩 조립라인 이동
    /*private void BatteryLineMove(short value)
    {
        MoveCommand(
            value, 1, batteryLineWaypoint,
            batteryLineArrivedAddress);
    }*/

    // M503 : 커버 조립라인 이동 (Waypoint 미정)
    /*private void CoverLineMove(short value)
    {
        MoveCommand(
            value, 2, coverLineWaypoint,
            coverLineArrivedAddress);
    }*/

    //208~237은 M503위치 정해지면 주석처리 하기
    // M502 : 배터리팩 조립라인 이동
    private void BatteryLineMove(short value)
    {
        Debug.Log($"[PLC 수신] M502 = {value}");

        Debug.Log($"[AGV 상태] Busy = {agvBusy}");
        Debug.Log($"[Waypoint 설정] BatteryLineWaypoint = {batteryLineWaypoint}");
        Debug.Log($"[Controller 연결] {agvController != null}");

        MoveCommand(
            value,
            1,
            batteryLineWaypoint,
            batteryLineArrivedAddress
        );
    }
    //배터리팩 커버 이동위치 정하지 않아 수정한 소스
    // M503 : 커버 조립라인 이동 (Waypoint 미정)
    private void CoverLineMove(short value)
    {
        if (value == 0)
        {
            moveCommandReceived[2] = false;
            return;
        }

        if (moveCommandReceived[2])
            return;

        moveCommandReceived[2] = true;

        Debug.Log(
            "[Pr1Connector] M503 수신 - Waypoint 미정으로 이동 보류");
    }

    // M505 : 조립공정 이동
    private void AssemblyMove(short value)
    {
        MoveCommand(
            value, 3, assemblyWaypoint,
            assemblyArrivedAddress);
    }

    // M507 : 2차 불량판정 위치 이동
    private void SecondDefectMove(short value)
    {
        MoveCommand(
            value, 4, secondDefectWaypoint,
            secondDefectArrivedAddress);
    }

    // M509 : 2차 공정 이동
    private void SecondProcessMove(short value)
    {
        MoveCommand(
            value, 5, secondProcessWaypoint,
            secondProcessArrivedAddress);
    }


    // ==========================================
    // AGV 이동 완료 처리
    // ==========================================

    private void OnCompletedMove()
    {
        if (currentArrivedAddress == null)
            return;

        Debug.Log("[Pr1Connector] AGV 목적지 도착");

        StartCoroutine(CompleteMoveFeedback());
    }

    private IEnumerator CompleteMoveFeedback()
    {
        DeviceAddress arrivedAddress = currentArrivedAddress;

        // 도착 신호 ON
        MXRequester.Get.AddSetDeviceRequest(
            arrivedAddress.address, 1);

        yield return new WaitForSeconds(feedbackTime);

        // 도착 신호 OFF
        MXRequester.Get.AddSetDeviceRequest(
            arrivedAddress.address, 0);

        currentArrivedAddress = null;
        agvBusy = false;

        Debug.Log("[Pr1Connector] AGV 도착 피드백 전송 완료");
    }


    // ==========================================
    // PLC -> Unity : 작업 명령 처리
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

        Debug.Log($"[Pr1Connector] 작업 명령 수신: {workName}");

        if (action != null)
            action.Invoke();
        else
            Debug.LogWarning($"{workName} 실행 이벤트가 연결되지 않았습니다.");
    }


    // M501 : 1차 불량판정 실행
    private void FirstInspectionCommand(short value)
    {
        ExecuteWorkCommand(
            value, 0, onFirstInspection, "1차 불량판정");
    }

    // M504 : 로봇팔 커버 조립
    private void CoverAssemblyCommand(short value)
    {
        ExecuteWorkCommand(
            value, 1, onCoverAssembly, "커버 조립");
    }

    // M506 : 로봇팔 배터리팩 조립
    private void BatteryAssemblyCommand(short value)
    {
        ExecuteWorkCommand(
            value, 2, onBatteryAssembly, "배터리팩 조립");
    }

    // M508 : 2차 불량판정 실행
    private void SecondInspectionCommand(short value)
    {
        ExecuteWorkCommand(
            value, 3, onSecondInspection, "2차 불량판정");
    }


    // ==========================================
    // Unity -> PLC : 완료 및 도착 신호
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


    // AGV 1차 불량판정 위치 도착
    public void OnFirstDefectArrived()
    {
        SendFeedback(firstDefectArrivedAddress);
    }

    // 1차 불량판정 완료
    public void OnFirstInspectionCompleted()
    {
        SendFeedback(firstInspectionCompletedAddress);
    }

    // AGV 배터리팩 조립라인 도착
    public void OnBatteryLineArrived()
    {
        SendFeedback(batteryLineArrivedAddress);
    }

    // 커버 조립라인 도착
    public void OnCoverLineArrived()
    {
        SendFeedback(coverLineArrivedAddress);
    }

    // 커버 조립 완료
    public void OnCoverAssemblyCompleted()
    {
        SendFeedback(coverAssemblyCompletedAddress);
    }

    // AGV 조립공정 도착
    public void OnAssemblyArrived()
    {
        SendFeedback(assemblyArrivedAddress);
    }

    // 배터리팩 조립 완료
    public void OnBatteryAssemblyCompleted()
    {
        SendFeedback(batteryAssemblyCompletedAddress);
    }

    // AGV 2차 불량판정 위치 도착
    public void OnSecondDefectArrived()
    {
        SendFeedback(secondDefectArrivedAddress);
    }

    // 2차 불량판정 완료
    public void OnSecondInspectionCompleted()
    {
        SendFeedback(secondInspectionCompletedAddress);
    }

    // AGV 2차 공정 도착
    public void OnSecondProcessArrived()
    {
        SendFeedback(secondProcessArrivedAddress);
    }


    // ==========================================
    // 불량판정 결과 전달
    // ==========================================

    public void FirstGood()
    {
        MXRequester.Get.AddSetDeviceRequest(firstGoodAddress.address, 1);
        MXRequester.Get.AddSetDeviceRequest(firstBadAddress.address, 0);
    }

    public void FirstBad()
    {
        MXRequester.Get.AddSetDeviceRequest(firstGoodAddress.address, 0);
        MXRequester.Get.AddSetDeviceRequest(firstBadAddress.address, 1);
    }

    public void ClearFirstResult()
    {
        MXRequester.Get.AddSetDeviceRequest(firstGoodAddress.address, 0);
        MXRequester.Get.AddSetDeviceRequest(firstBadAddress.address, 0);
    }

    public void SecondGood()
    {
        MXRequester.Get.AddSetDeviceRequest(secondGoodAddress.address, 1);
        MXRequester.Get.AddSetDeviceRequest(secondBadAddress.address, 0);
    }

    public void SecondBad()
    {
        MXRequester.Get.AddSetDeviceRequest(secondGoodAddress.address, 0);
        MXRequester.Get.AddSetDeviceRequest(secondBadAddress.address, 1);
    }

    public void ClearSecondResult()
    {
        MXRequester.Get.AddSetDeviceRequest(secondGoodAddress.address, 0);
        MXRequester.Get.AddSetDeviceRequest(secondBadAddress.address, 0);
    }


    // ==========================================
    // 공정 상태 전달
    // ==========================================

    public void OnProcessCompleted()
    {
        MXRequester.Get.AddSetDeviceRequest(
            processCompletedAddress.address, 1);
    }

    public void OnGoodStatus()
    {
        MXRequester.Get.AddSetDeviceRequest(
            goodStatusAddress.address, 1);

        MXRequester.Get.AddSetDeviceRequest(
            badStatusAddress.address, 0);
    }

    public void OnBadStatus()
    {
        MXRequester.Get.AddSetDeviceRequest(
            goodStatusAddress.address, 0);

        MXRequester.Get.AddSetDeviceRequest(
            badStatusAddress.address, 1);
    }

    public void OnResetCompleted()
    {
        MXRequester.Get.AddSetDeviceRequest(
            resetCompletedAddress.address, 1);
    }

    public void SetSafetyStatus(bool isSafe)
    {
        MXRequester.Get.AddSetDeviceRequest(
            safetyAddress.address, isSafe ? 1 : 0);
    }

    public void SetSimulationReady(bool isReady)
    {
        MXRequester.Get.AddSetDeviceRequest(
            simulationReadyAddress.address, isReady ? 1 : 0);
    }

    public void SetAutoRun(bool isRunning)
    {
        MXRequester.Get.AddSetDeviceRequest(
            autoRunAddress.address, isRunning ? 1 : 0);
    }

    public void SetAutoPermit(bool isPermitted)
    {
        MXRequester.Get.AddSetDeviceRequest(
            autoPermitAddress.address, isPermitted ? 1 : 0);
    }


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
