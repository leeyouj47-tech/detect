
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Pr2Connector : MXObject
{
    [Header("AGV")]
    public Pr1Controller agvController;

    [Header("차체")]
    public BodyController bodyController;

    [Header("ICCU")]
    public ICCUController iccuController;

    [Header("Waypoint ID")]
    public int agvWaypoint1 = 1;
    public int bodyWaypoint1 = 1;
    public int bodyWaypoint2 = 2;
    public int bodyWaypoint3 = 3;
    public int bodyWaypoint4 = 4;
    public int iccuWaypoint1 = 1;
    public int iccuWaypoint2 = 2;

    [Header("PLC -> Unity : 이동 명령")]
    public DeviceAddress agvMoveAddress =
        new DeviceAddress("AGV WP1 이동");

    public DeviceAddress bodyWP1MoveAddress =
        new DeviceAddress("차체 WP1 이동");

    public DeviceAddress iccuWP1MoveAddress =
        new DeviceAddress("ICCU WP1 이동");

    public DeviceAddress iccuWP2MoveAddress =
        new DeviceAddress("ICCU WP2 이동");

    public DeviceAddress bodyWP2MoveAddress =
        new DeviceAddress("차체 WP2 롤러 이동");

    public DeviceAddress bodyWP3MoveAddress =
        new DeviceAddress("차체 WP3 이동");

    public DeviceAddress bodyWP4MoveAddress =
        new DeviceAddress("차체 WP4 이동");


    [Header("PLC -> Unity : 장비 동작 명령")]
    public DeviceAddress liftAddress =
        new DeviceAddress("리프트 배터리 결합");

    public DeviceAddress robotArmAddress =
        new DeviceAddress("ICCU 로봇팔 결합");

    public DeviceAddress rollerAddress =
        new DeviceAddress("롤러 동작");


    [Header("Unity 동작 이벤트")]
    public UnityEvent onLiftOperation;
    public UnityEvent onRobotArmOperation;
    public UnityEvent onRollerOperation;

    [Header("불량검사 이벤트")]
    public UnityEvent onICCUInspection;
    public UnityEvent onBodyInspection;

    [Header("Unity -> PLC : 도착 및 완료 신호")]
    public DeviceAddress agvArrivedAddress =
        new DeviceAddress("AGV WP1 도착");

    public DeviceAddress bodyWP1ArrivedAddress =
        new DeviceAddress("차체 WP1 도착");

    public DeviceAddress liftCompletedAddress =
        new DeviceAddress("리프트 결합 완료");

    public DeviceAddress iccuWP1ArrivedAddress =
        new DeviceAddress("ICCU WP1 도착");

    public DeviceAddress iccuWP2ArrivedAddress =
        new DeviceAddress("ICCU WP2 도착");

    public DeviceAddress iccuAssemblyCompletedAddress =
        new DeviceAddress("ICCU 결합 완료");

    public DeviceAddress bodyWP2ArrivedAddress =
        new DeviceAddress("차체 WP2 도착");

    public DeviceAddress bodyWP3ArrivedAddress =
        new DeviceAddress("차체 WP3 도착");

    public DeviceAddress bodyWP4ArrivedAddress =
        new DeviceAddress("차체 WP4 도착");


    [Header("검사 결과 신호")]
    public DeviceAddress iccuGoodAddress =
        new DeviceAddress("ICCU 검사 정상");

    public DeviceAddress iccuBadAddress =
        new DeviceAddress("ICCU 검사 불량");

    public DeviceAddress bodyGoodAddress =
        new DeviceAddress("차체 검사 정상");

    public DeviceAddress bodyBadAddress =
        new DeviceAddress("차체 검사 불량");


    [Header("공통 설정")]
    public float feedbackTime = 0.3f;


    // ==========================================
    // 이동 상태
    // ==========================================

    private bool agvBusy = false;
    private bool bodyBusy = false;
    private bool iccuBusy = false;

    private bool agvCommandReceived = false;
    private bool bodyWP1CommandReceived = false;
    private bool bodyWP2CommandReceived = false;
    private bool bodyWP3CommandReceived = false;
    private bool bodyWP4CommandReceived = false;

    private bool iccuWP1CommandReceived = false;
    private bool iccuWP2CommandReceived = false;

    private bool liftCommandReceived = false;
    private bool robotCommandReceived = false;
    private bool rollerCommandReceived = false;


    private DeviceAddress agvCurrentAddress;
    private DeviceAddress bodyCurrentAddress;
    private DeviceAddress iccuCurrentAddress;


    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (agvController == null ||
            bodyController == null ||
            iccuController == null)
        {
            Debug.LogError(
                "[Process2Connector] Controller 연결을 확인하세요.");
            return;
        }

        agvController.onCompleteMove.AddListener(OnAGVCompleted);
        bodyController.onCompleteMove.AddListener(OnBodyCompleted);
        iccuController.onCompleteMove.AddListener(OnICCUCompleted);


        // AGV 이동
        MXRequester.Get.AddDeviceAddress(
            agvMoveAddress.address, AGVMove);


        // 차체 이동
        MXRequester.Get.AddDeviceAddress(
            bodyWP1MoveAddress.address, BodyWP1Move);

        MXRequester.Get.AddDeviceAddress(
            bodyWP2MoveAddress.address, BodyWP2Move);

        MXRequester.Get.AddDeviceAddress(
            bodyWP3MoveAddress.address, BodyWP3Move);

        MXRequester.Get.AddDeviceAddress(
            bodyWP4MoveAddress.address, BodyWP4Move);


        // ICCU 이동
        MXRequester.Get.AddDeviceAddress(
            iccuWP1MoveAddress.address, ICCUWP1Move);

        MXRequester.Get.AddDeviceAddress(
            iccuWP2MoveAddress.address, ICCUWP2Move);


        // 장비 동작
        MXRequester.Get.AddDeviceAddress(
            liftAddress.address, LiftCommand);

        MXRequester.Get.AddDeviceAddress(
            robotArmAddress.address, RobotArmCommand);

        MXRequester.Get.AddDeviceAddress(
            rollerAddress.address, RollerCommand);


        Debug.Log("[Process2Connector] PLC 신호 등록 완료");
    }


    // ==========================================
    // 이동 공통 처리
    // ==========================================

    private void MoveCommand(
        short value,
        ref bool commandReceived,
        ref bool busy,
        Pr2Controller controller,
        int waypointID,
        DeviceAddress arrivedAddress,
        string deviceName)
    {
        if (value == 0)
        {
            commandReceived = false;
            return;
        }

        if (commandReceived || busy)
            return;

        if (controller == null)
        {
            Debug.LogError(
                $"[{deviceName}] Controller가 연결되지 않았습니다.");
            return;
        }

        bool success = controller.Go(waypointID);

        if (!success)
        {
            Debug.LogError(
                $"[{deviceName}] 경로 이동 실패: WP{waypointID}");
            return;
        }

        commandReceived = true;
        busy = true;

        if (controller == bodyController)
            bodyCurrentAddress = arrivedAddress;

        else if (controller == iccuController)
            iccuCurrentAddress = arrivedAddress;

        Debug.Log(
            $"[{deviceName}] 이동 시작 / WP{waypointID}");
    }


    // ==========================================
    // AGV 이동
    // ==========================================

    private void AGVMove(short value)
    {
        if (value == 0)
        {
            agvCommandReceived = false;
            return;
        }

        if (agvCommandReceived || agvBusy)
            return;

        if (agvController == null)
            return;

        bool success = agvController.Go(agvWaypoint1);

        if (!success)
            return;

        agvCommandReceived = true;
        agvBusy = true;

        agvCurrentAddress = agvArrivedAddress;

        Debug.Log("[Process2] AGV WP1 이동 시작");
    }


    // ==========================================
    // 차체 이동
    // ==========================================

    private void BodyWP1Move(short value)
    {
        MoveCommand(
            value,
            ref bodyWP1CommandReceived,
            ref bodyBusy,
            bodyController,
            bodyWaypoint1,
            bodyWP1ArrivedAddress,
            "차체 WP1");
    }

    private void BodyWP2Move(short value)
    {
        if (value == 0)
        {
            bodyWP2CommandReceived = false;
            return;
        }

        if (bodyWP2CommandReceived || bodyBusy)
            return;

        bool success = bodyController.Go(bodyWaypoint2);

        if (!success)
            return;

        bodyWP2CommandReceived = true;
        bodyBusy = true;

        bodyCurrentAddress = bodyWP2ArrivedAddress;

        // 차체 이동과 동시에 롤러 동작
        onRollerOperation?.Invoke();

        Debug.Log("[Process2] 차체 WP2 이동 및 롤러 동작 시작");
    }
    private void BodyWP3Move(short value)
    {
        MoveCommand(
            value,
            ref bodyWP3CommandReceived,
            ref bodyBusy,
            bodyController,
            bodyWaypoint3,
            bodyWP3ArrivedAddress,
            "차체 WP3");
    }

    private void BodyWP4Move(short value)
    {
        MoveCommand(
            value,
            ref bodyWP4CommandReceived,
            ref bodyBusy,
            bodyController,
            bodyWaypoint4,
            bodyWP4ArrivedAddress,
            "차체 WP4");
    }


    // ==========================================
    // ICCU 이동
    // ==========================================

    private void ICCUWP1Move(short value)
    {
        MoveCommand(
            value,
            ref iccuWP1CommandReceived,
            ref iccuBusy,
            iccuController,
            iccuWaypoint1,
            iccuWP1ArrivedAddress,
            "ICCU WP1");
    }

    private void ICCUWP2Move(short value)
    {
        MoveCommand(
            value,
            ref iccuWP2CommandReceived,
            ref iccuBusy,
            iccuController,
            iccuWaypoint2,
            iccuWP2ArrivedAddress,
            "ICCU WP2");
    }


    // ==========================================
    // 장비 동작 명령
    // ==========================================

    private void ExecuteWork(
        short value,
        ref bool received,
        UnityEvent action,
        string workName)
    {
        if (value == 0)
        {
            received = false;
            return;
        }

        if (received)
            return;

        received = true;

        Debug.Log($"[Process2] {workName} 실행");

        if (action != null)
            action.Invoke();
        else
            Debug.LogWarning(
                $"{workName} 이벤트가 연결되지 않았습니다.");
    }

    private void LiftCommand(short value)
    {
        ExecuteWork(
            value, ref liftCommandReceived,
            onLiftOperation, "리프트 배터리 결합");
    }

    private void RobotArmCommand(short value)
    {
        ExecuteWork(
            value, ref robotCommandReceived,
            onRobotArmOperation, "ICCU 로봇팔 결합");
    }

    private void RollerCommand(short value)
    {
        ExecuteWork(
            value, ref rollerCommandReceived,
            onRollerOperation, "롤러 동작");
    }


    // ==========================================
    // AGV 이동 완료
    // ==========================================

    private void OnAGVCompleted()
    {
        if (agvCurrentAddress == null)
            return;

        StartCoroutine(
            CompleteMoveFeedback(agvCurrentAddress, 0));
    }


    // ==========================================
    // 차체 이동 완료
    // ==========================================

    private void OnBodyCompleted()
    {
        if (bodyCurrentAddress == null)
            return;

        bool isInspectionPosition =
            bodyCurrentAddress == bodyWP3ArrivedAddress;

        if (isInspectionPosition)
            onBodyInspection?.Invoke();

        StartCoroutine(
            CompleteMoveFeedback(bodyCurrentAddress, 1));
    }

    // ==========================================
    // ICCU 이동 완료
    // ==========================================

    private void OnICCUCompleted()
    {
        if (iccuCurrentAddress == null)
            return;

        bool isInspectionPosition =
            iccuCurrentAddress == iccuWP1ArrivedAddress;

        if (isInspectionPosition)
            onICCUInspection?.Invoke();

        StartCoroutine(
            CompleteMoveFeedback(iccuCurrentAddress, 2));
    }


    // ==========================================
    // 이동 완료 피드백
    // ==========================================

    private IEnumerator CompleteMoveFeedback(
        DeviceAddress address, int deviceIndex)
    {
        MXRequester.Get.AddSetDeviceRequest(
            address.address, 1);

        Debug.Log(
            $"[Process2] 도착 신호 ON: {address.address}");

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            address.address, 0);

        if (deviceIndex == 0)
        {
            agvBusy = false;
            agvCurrentAddress = null;
        }
        else if (deviceIndex == 1)
        {
            bodyBusy = false;
            bodyCurrentAddress = null;
        }
        else
        {
            iccuBusy = false;
            iccuCurrentAddress = null;
        }

        Debug.Log(
            $"[Process2] 도착 피드백 완료: {address.address}");
    }


    // ==========================================
    // 장비 동작 완료 피드백
    // ==========================================

    public void OnLiftCompleted()
    {
        SendFeedback(liftCompletedAddress);
    }

    public void OnICCUAssemblyCompleted()
    {
        SendFeedback(iccuAssemblyCompletedAddress);
    }

    public void OnRollerCompleted()
    {
        SendFeedback(bodyWP2ArrivedAddress);
    }


    // ==========================================
    // 검사 결과 전달
    // ==========================================

    public void ICCUGood()
    {
        SetResult(iccuGoodAddress, iccuBadAddress, true);
    }

    public void ICCUBad()
    {
        SetResult(iccuGoodAddress, iccuBadAddress, false);
    }

    public void BodyGood()
    {
        SetResult(bodyGoodAddress, bodyBadAddress, true);
    }

    public void BodyBad()
    {
        SetResult(bodyGoodAddress, bodyBadAddress, false);
    }

    public void ClearICCUResult()
    {
        ClearResult(iccuGoodAddress, iccuBadAddress);
    }

    public void ClearBodyResult()
    {
        ClearResult(bodyGoodAddress, bodyBadAddress);
    }

    private void SetResult(
        DeviceAddress good,
        DeviceAddress bad,
        bool isGood)
    {
        MXRequester.Get.AddSetDeviceRequest(
            good.address, isGood ? 1 : 0);

        MXRequester.Get.AddSetDeviceRequest(
            bad.address, isGood ? 0 : 1);
    }

    private void ClearResult(
        DeviceAddress good,
        DeviceAddress bad)
    {
        MXRequester.Get.AddSetDeviceRequest(
            good.address, 0);

        MXRequester.Get.AddSetDeviceRequest(
            bad.address, 0);
    }


    // ==========================================
    // 공통 피드백
    // ==========================================

    private void SendFeedback(DeviceAddress address)
    {
        StartCoroutine(FeedbackCoroutine(address));
    }

    private IEnumerator FeedbackCoroutine(DeviceAddress address)
    {
        MXRequester.Get.AddSetDeviceRequest(
            address.address, 1);

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            address.address, 0);
    }


    // ==========================================
    // 종료 처리
    // ==========================================

    private void OnDestroy()
    {
        if (agvController != null)
            agvController.onCompleteMove.RemoveListener(OnAGVCompleted);

        if (bodyController != null)
            bodyController.onCompleteMove.RemoveListener(OnBodyCompleted);

        if (iccuController != null)
            iccuController.onCompleteMove.RemoveListener(OnICCUCompleted);
    }
}
