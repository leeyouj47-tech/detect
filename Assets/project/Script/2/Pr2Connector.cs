
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Pr2Connector : MXObject
{
    [Header("전체 컨트롤러")]
    public Pr2Controller controller;

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    // ==========================================
    // PLC -> Unity : 이동 명령
    // ==========================================

    [Header("AGV 이동 명령")]
    public DeviceAddress agvWP1Command = new DeviceAddress("M720");
    public DeviceAddress agvWP2Command = new DeviceAddress("M721");
    public DeviceAddress agvWP3Command = new DeviceAddress("M722");
    public DeviceAddress agvWP4Command = new DeviceAddress("M723");

    [Header("Rail 이동 명령")]
    public DeviceAddress railWP1Command = new DeviceAddress("M724");

    [Header("ICCU 이동 명령")]
    public DeviceAddress iccuWP1Command = new DeviceAddress("M725");
    public DeviceAddress iccuWP2Command = new DeviceAddress("M726");

    [Header("Lift 이동 명령")]
    public DeviceAddress liftWP1Command = new DeviceAddress("M727");
    public DeviceAddress liftWP2Command = new DeviceAddress("M728");
    public DeviceAddress liftWP3Command = new DeviceAddress("M729");
    public DeviceAddress liftWP4Command = new DeviceAddress("M730");

    // ==========================================
    // PLC -> Unity : 작업 명령
    // ==========================================

    [Header("작업 명령")]
    public DeviceAddress batteryCouplingCommand = new DeviceAddress("M731");
    public DeviceAddress agvLeaveCommand = new DeviceAddress("M732");
    public DeviceAddress robotArmCommand = new DeviceAddress("M733");
    public DeviceAddress iccuInspectionCommand = new DeviceAddress("M734");
    public DeviceAddress chassisInspectionCommand = new DeviceAddress("M735");

    // ==========================================
    // Unity -> PLC : 도착 및 완료 신호
    // ==========================================

    [Header("AGV 도착 신호")]
    public DeviceAddress agvWP1Arrived = new DeviceAddress("M740");
    public DeviceAddress agvWP2Arrived = new DeviceAddress("M741");
    public DeviceAddress agvWP3Arrived = new DeviceAddress("M742");
    public DeviceAddress agvWP4Arrived = new DeviceAddress("M743");

    [Header("Rail 도착 신호")]
    public DeviceAddress railWP1Arrived = new DeviceAddress("M744");

    [Header("ICCU 도착 신호")]
    public DeviceAddress iccuWP1Arrived = new DeviceAddress("M745");
    public DeviceAddress iccuWP2Arrived = new DeviceAddress("M746");

    [Header("Lift 도착 신호")]
    public DeviceAddress liftWP1Arrived = new DeviceAddress("M747");
    public DeviceAddress liftWP2Arrived = new DeviceAddress("M748");
    public DeviceAddress liftWP3Arrived = new DeviceAddress("M749");
    public DeviceAddress liftWP4Arrived = new DeviceAddress("M750");

    [Header("작업 완료 신호")]
    public DeviceAddress batteryCouplingCompleted = new DeviceAddress("M751");
    public DeviceAddress agvLeaveCompleted = new DeviceAddress("M752");
    public DeviceAddress robotArmCompleted = new DeviceAddress("M753");

    // ==========================================
    // Unity -> PLC : 검사 결과
    // ==========================================

    [Header("ICCU 검사 결과")]
    public DeviceAddress iccuInspectionCompleted = new DeviceAddress("M760");
    public DeviceAddress iccuGood = new DeviceAddress("M761");
    public DeviceAddress iccuBad = new DeviceAddress("M762");

    [Header("차체 검사 결과")]
    public DeviceAddress chassisInspectionCompleted = new DeviceAddress("M763");
    public DeviceAddress chassisGood = new DeviceAddress("M764");
    public DeviceAddress chassisBad = new DeviceAddress("M765");

    // ==========================================
    // PLC -> Unity : 공정 이상
    // ==========================================

    [Header("공정 이상 신호")]
    public DeviceAddress processFaultAddress = new DeviceAddress("M717");

    // ==========================================
    // 명령 중복 수신 방지
    // ==========================================

    private bool[] moveCommandReceived = new bool[11];
    private bool[] workCommandReceived = new bool[5];

    private DeviceAddress[] currentArrivedAddress =
        new DeviceAddress[4];

    private bool faulted;

    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (controller == null)
        {
            Debug.LogError(
                "[Pr2Connector] Pr2Controller가 연결되지 않았습니다.");
            return;
        }

        if (controller.agv == null ||
            controller.lift == null ||
            controller.rail == null ||
            controller.iccu == null)
        {
            Debug.LogError(
                "[Pr2Connector] 장치별 Route가 연결되지 않았습니다.");
            return;
        }

        // 이동 완료 이벤트 등록
        controller.agv.onCompleteMove.AddListener(OnAGVCompleted);
        controller.lift.onCompleteMove.AddListener(OnLiftCompleted);
        controller.rail.onCompleteMove.AddListener(OnRailCompleted);
        controller.iccu.onCompleteMove.AddListener(OnICCUCompleted);

        // AGV 명령 등록
        MXRequester.Get.AddDeviceAddress(
            agvWP1Command.address, AGVWP1Move);

        MXRequester.Get.AddDeviceAddress(
            agvWP2Command.address, AGVWP2Move);

        MXRequester.Get.AddDeviceAddress(
            agvWP3Command.address, AGVWP3Move);

        MXRequester.Get.AddDeviceAddress(
            agvWP4Command.address, AGVWP4Move);

        // Rail 명령 등록
        MXRequester.Get.AddDeviceAddress(
            railWP1Command.address, RailWP1Move);

        // ICCU 명령 등록
        MXRequester.Get.AddDeviceAddress(
            iccuWP1Command.address, ICCUWP1Move);

        MXRequester.Get.AddDeviceAddress(
            iccuWP2Command.address, ICCUWP2Move);

        // Lift 명령 등록
        MXRequester.Get.AddDeviceAddress(
            liftWP1Command.address, LiftWP1Move);

        MXRequester.Get.AddDeviceAddress(
            liftWP2Command.address, LiftWP2Move);

        MXRequester.Get.AddDeviceAddress(
            liftWP3Command.address, LiftWP3Move);

        MXRequester.Get.AddDeviceAddress(
            liftWP4Command.address, LiftWP4Move);

        // 작업 명령 등록
        MXRequester.Get.AddDeviceAddress(
            batteryCouplingCommand.address, BatteryCoupling);

        MXRequester.Get.AddDeviceAddress(
            agvLeaveCommand.address, AGVLeave);

        MXRequester.Get.AddDeviceAddress(
            robotArmCommand.address, RobotArm);

        MXRequester.Get.AddDeviceAddress(
            iccuInspectionCommand.address, ICCUInspection);

        MXRequester.Get.AddDeviceAddress(
            chassisInspectionCommand.address, ChassisInspection);

        // 불량 정지 신호 등록
        MXRequester.Get.AddDeviceAddress(
            processFaultAddress.address, OnProcessFault);

        Debug.Log("[Pr2Connector] PLC 주소 등록 완료");
    }

    // ==========================================
    // 이동 명령 공통 처리
    // ==========================================


    private void MoveCommand(
        short value,
        int commandIndex,
        int deviceIndex,
        int waypointID,
        DeviceAddress arrivedAddress,
        Pr2Route route,
        System.Func<bool> moveAction)
    {
        // PLC 명령 OFF
        if (value == 0)
        {
            moveCommandReceived[commandIndex] = false;
            return;
        }

        // 불량 발생 또는 이미 처리한 명령
        if (faulted || moveCommandReceived[commandIndex])
            return;

        // Route 확인
        if (route == null)
        {
            Debug.LogError("[Pr2Connector] Route가 연결되지 않았습니다.");
            return;
        }

        // 이미 이동 중인지 확인
        if (route.IsMoving())
        {
            Debug.LogWarning(
                $"[Pr2Connector] 장치 이동 중: {route.name}");
            return;
        }

        // 이동 실행 및 성공 여부 확인
        bool success = moveAction != null && moveAction.Invoke();

        if (!success)
        {
            Debug.LogError(
                $"[Pr2Connector] 이동 실패: {route.name}, WP{waypointID}");
            return;
        }

        // 이동 성공 후 도착 주소 저장
        currentArrivedAddress[deviceIndex] = arrivedAddress;
        moveCommandReceived[commandIndex] = true;

        Debug.Log(
            $"[Pr2Connector] 이동 시작: {route.name} / WP{waypointID}");
    }



    // ==========================================
    // AGV 이동 명령
    // ==========================================

    private void AGVWP1Move(short value)
    {
        MoveCommand(value, 0, 0, 1, agvWP1Arrived,
            controller.agv,
            () => controller.MoveAGV(1));
    }

    private void AGVWP2Move(short value)
    {
        MoveCommand(value, 1, 0, 2, agvWP2Arrived,
            controller.agv,
            () => controller.MoveAGV(2));
    }

    private void AGVWP3Move(short value)
    {
        MoveCommand(value, 2, 0, 3, agvWP3Arrived,
            controller.agv,
            () => controller.MoveAGV(3));
    }

    private void AGVWP4Move(short value)
    {
        MoveCommand(value, 3, 0, 4, agvWP4Arrived,
            controller.agv,
            () => controller.MoveAGV(4));
    }

    // ==========================================
    // Rail 이동 명령
    // ==========================================

    private void RailWP1Move(short value)
    {
        MoveCommand(value, 4, 2, 1, railWP1Arrived,
            controller.rail,
            () => controller.MoveRail(1));
    }

    // ==========================================
    // ICCU 이동 명령
    // ==========================================

    private void ICCUWP1Move(short value)
    {
        MoveCommand(value, 5, 3, 1, iccuWP1Arrived,
            controller.iccu,
            () => controller.MoveICCU(1));
    }

    private void ICCUWP2Move(short value)
    {
        MoveCommand(value, 6, 3, 2, iccuWP2Arrived,
            controller.iccu,
            () => controller.MoveICCU(2));
    }

    // ==========================================
    // Lift 이동 명령
    // ==========================================

    private void LiftWP1Move(short value)
    {
        MoveCommand(value, 7, 1, 1, liftWP1Arrived,
            controller.lift,
            () => controller.MoveLift(1));
    }

    private void LiftWP2Move(short value)
    {
        MoveCommand(value, 8, 1, 2, liftWP2Arrived,
            controller.lift,
            () => controller.MoveLift(2));
    }

    private void LiftWP3Move(short value)
    {
        MoveCommand(value, 9, 1, 3, liftWP3Arrived,
            controller.lift,
            () => controller.MoveLift(3));
    }

    private void LiftWP4Move(short value)
    {
        MoveCommand(value, 10, 1, 4, liftWP4Arrived,
            controller.lift,
            () => controller.MoveLift(4));
    }


    // ==========================================
    // 이동 완료 처리
    // ==========================================

    private void OnAGVCompleted()
    {
        CompleteMoveFeedback(0);
    }

    private void OnLiftCompleted()
    {
        CompleteMoveFeedback(1);
    }

    private void OnRailCompleted()
    {
        CompleteMoveFeedback(2);
    }

    private void OnICCUCompleted()
    {
        CompleteMoveFeedback(3);
    }

    private void CompleteMoveFeedback(int deviceIndex)
    {
        DeviceAddress address = currentArrivedAddress[deviceIndex];

        if (address == null)
            return;

        currentArrivedAddress[deviceIndex] = null;

        Debug.Log($"[Pr2Connector] 이동 완료: {address.address}");

        StartCoroutine(SendFeedbackCoroutine(address));
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

        if (faulted || workCommandReceived[index])
            return;

        workCommandReceived[index] = true;

        Debug.Log($"[Pr2Connector] 작업 명령 수신: {workName}");

        if (action != null)
            action.Invoke();
        else
            Debug.LogWarning(
                $"[{workName}] UnityEvent가 연결되지 않았습니다.");
    }

    private void BatteryCoupling(short value)
    {
        ExecuteWorkCommand(
            value, 0,
            controller.onBatteryCoupling,
            "배터리 결합");
    }

    private void AGVLeave(short value)
    {
        ExecuteWorkCommand(
            value, 1,
            controller.onAGVLeave,
            "AGV 이탈");
    }

    private void RobotArm(short value)
    {
        ExecuteWorkCommand(
            value, 2,
            controller.onRobotArm,
            "로봇팔");
    }

    private void ICCUInspection(short value)
    {
        ExecuteWorkCommand(
            value, 3,
            controller.onICCUInspection,
            "ICCU 검사");
    }

    private void ChassisInspection(short value)
    {
        ExecuteWorkCommand(
            value, 4,
            controller.onChassisInspection,
            "차체 검사");
    }

    // ==========================================
    // Unity -> PLC : 완료 신호
    // ==========================================

    public void SendFeedback(DeviceAddress address)
    {
        if (address == null)
            return;

        StartCoroutine(SendFeedbackCoroutine(address));
    }

    private IEnumerator SendFeedbackCoroutine(DeviceAddress address)
    {
        MXRequester.Get.AddSetDeviceRequest(
            address.address, (short)1);

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            address.address, (short)0);
    }

    // ==========================================
    // 배터리 결합 완료
    // Animation Event에서 호출
    // ==========================================

    public void OnBatteryCouplingCompleted()
    {
        SendFeedback(batteryCouplingCompleted);
    }

    // ==========================================
    // AGV 이탈 완료
    // Animation Event에서 호출
    // ==========================================

    public void OnAGVLeaveCompleted()
    {
        SendFeedback(agvLeaveCompleted);
    }

    // ==========================================
    // 로봇팔 완료
    // RobotArmConnector 등에서 호출
    // ==========================================

    public void OnRobotArmCompleted()
    {
        SendFeedback(robotArmCompleted);
    }

    // ==========================================
    // ICCU 검사 결과
    // 검사 결과에 따라 UnityEvent에서 호출
    // ==========================================

    public void ICCUInspectionGood()
    {
        MXRequester.Get.AddSetDeviceRequest(
            iccuGood.address, (short)1);

        MXRequester.Get.AddSetDeviceRequest(
            iccuBad.address, (short)0);

        SendFeedback(iccuInspectionCompleted);

        Debug.Log("[ICCU] 정상품 판정");
    }

    public void ICCUInspectionBad()
    {
        MXRequester.Get.AddSetDeviceRequest(
            iccuGood.address, (short)0);

        MXRequester.Get.AddSetDeviceRequest(
            iccuBad.address, (short)1);

        SendFeedback(iccuInspectionCompleted);

        Debug.LogWarning("[ICCU] 불량품 판정");
    }

    public void ClearICCUResult()
    {
        MXRequester.Get.AddSetDeviceRequest(
            iccuGood.address, (short)0);

        MXRequester.Get.AddSetDeviceRequest(
            iccuBad.address, (short)0);
    }

    // ==========================================
    // 차체 검사 결과
    // ==========================================

    public void ChassisInspectionGood()
    {
        MXRequester.Get.AddSetDeviceRequest(
            chassisGood.address, (short)1);

        MXRequester.Get.AddSetDeviceRequest(
            chassisBad.address, (short)0);

        SendFeedback(chassisInspectionCompleted);

        Debug.Log("[차체] 정상품 판정");
    }

    public void ChassisInspectionBad()
    {
        MXRequester.Get.AddSetDeviceRequest(
            chassisGood.address, (short)0);

        MXRequester.Get.AddSetDeviceRequest(
            chassisBad.address, (short)1);

        SendFeedback(chassisInspectionCompleted);

        Debug.LogWarning("[차체] 불량품 판정");
    }

    public void ClearChassisResult()
    {
        MXRequester.Get.AddSetDeviceRequest(
            chassisGood.address, (short)0);

        MXRequester.Get.AddSetDeviceRequest(
            chassisBad.address, (short)0);
    }

    // ==========================================
    // PLC 불량 신호 수신
    // M717 ON -> 전체 장치 정지
    // ==========================================

    private void OnProcessFault(short value)
    {
        bool isFault = value != 0;

        if (isFault && !faulted)
        {
            faulted = true;

            Debug.LogError(
                "[Pr2Connector] 불량 신호 수신. 전체 공정 정지");

            controller.StopAllDevices();
        }
        else if (!isFault)
        {
            faulted = false;

            Debug.Log(
                "[Pr2Connector] 공정 이상 신호 OFF");
        }
    }

    // ==========================================
    // 외부에서 공정 정지 상태 확인
    // ==========================================

    public bool IsFaulted()
    {
        return faulted;
    }

    // ==========================================
    // 종료 처리
    // ==========================================

    private void OnDestroy()
    {
        if (controller == null)
            return;

        if (controller.agv != null)
            controller.agv.onCompleteMove.RemoveListener(OnAGVCompleted);

        if (controller.lift != null)
            controller.lift.onCompleteMove.RemoveListener(OnLiftCompleted);

        if (controller.rail != null)
            controller.rail.onCompleteMove.RemoveListener(OnRailCompleted);

        if (controller.iccu != null)
            controller.iccu.onCompleteMove.RemoveListener(OnICCUCompleted);
    }
}
