using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Pr2Connector : MXObject
{
    [Header("메인 컨트롤러")]
    public Pr2Controller pr2Controller;

    [Header("Waypoint ID - Inspector에서 실제 ID 확인")]
    public int agvWaypoint1 = 1;
    public int trolleyWaypoint1 = 1;
    public int iccuWaypoint1 = 1;
    public int iccuWaypoint2 = 2;
    public int carBodyWaypoint1 = 1;
    public int carBodyWaypoint2 = 2;
    public int carBodyWaypoint3 = 3;
    public int carBodyWaypoint4 = 4;

    [Header("PLC -> Unity : 이동 명령")]
    public DeviceAddress agvWP1MoveAddress = new DeviceAddress("M620 AGV WP1 출발");
    public DeviceAddress trolleyWP1MoveAddress = new DeviceAddress("M621 Trolley WP1 출발");
    public DeviceAddress iccuWP1MoveAddress = new DeviceAddress("M622 ICCU WP1 출발");
    public DeviceAddress iccuWP2MoveAddress = new DeviceAddress("M623 ICCU WP2 출발");
    public DeviceAddress carBodyWP1MoveAddress = new DeviceAddress("M624 CarBody WP1 출발");
    public DeviceAddress carBodyWP2MoveAddress = new DeviceAddress("M626 CarBody WP2 출발");
    public DeviceAddress carBodyWP3MoveAddress = new DeviceAddress("M627 CarBody WP3 출발");
    public DeviceAddress carBodyWP4MoveAddress = new DeviceAddress("M628 CarBody WP4 출발");

    [Header("PLC -> Unity : 로봇팔 작업 명령")]
    public DeviceAddress iccuAssemblyCommandAddress = new DeviceAddress("M625 ICCU 결합 명령");

    [Header("작업 실행 이벤트")]
    public UnityEvent onICCUAssembly = new UnityEvent();

    [Header("Unity -> PLC : 이동 도착 피드백")]
    public DeviceAddress agvWP1ArrivedAddress = new DeviceAddress("M640 AGV WP1 도착");
    public DeviceAddress trolleyWP1ArrivedAddress = new DeviceAddress("M641 Trolley WP1 도착");
    public DeviceAddress iccuWP1ArrivedAddress = new DeviceAddress("M642 ICCU WP1 도착");
    public DeviceAddress iccuWP2ArrivedAddress = new DeviceAddress("M643 ICCU WP2 도착");
    public DeviceAddress carBodyWP1ArrivedAddress = new DeviceAddress("M644 CarBody WP1 도착");
    public DeviceAddress carBodyWP2ArrivedAddress = new DeviceAddress("M646 CarBody WP2 도착");
    public DeviceAddress carBodyWP3ArrivedAddress = new DeviceAddress("M647 CarBody WP3 도착");
    public DeviceAddress carBodyWP4ArrivedAddress = new DeviceAddress("M648 CarBody WP4 도착");

    [Header("Unity -> PLC : 로봇팔 완료 피드백")]
    public DeviceAddress iccuAssemblyCompletedAddress = new DeviceAddress("M645 ICCU 결합 완료");

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    private bool[] moveCommandReceived = new bool[8];
    private bool iccuAssemblyCommandReceived;

    private DeviceAddress currentAGVArrivedAddress;
    private DeviceAddress currentTrolleyArrivedAddress;
    private DeviceAddress currentICCUArrivedAddress;
    private DeviceAddress currentCarBodyArrivedAddress;

    private void Start()
    {
        if (pr2Controller == null)
        {
            Debug.LogError("[Pr2Connector] Pr2Controller가 연결되지 않았습니다.");
            return;
        }

        // 이동 완료 이벤트 연결
        pr2Controller.onAGVCompleteMove.AddListener(OnAGVCompletedMove);
        pr2Controller.onTrolleyCompleteMove.AddListener(OnTrolleyCompletedMove);
        pr2Controller.onICCUCompleteMove.AddListener(OnICCUCompletedMove);
        pr2Controller.onCarBodyCompleteMove.AddListener(OnCarBodyCompletedMove);

        // AGV / Trolley / ICCU / CarBody 이동 명령 등록
        MXRequester.Get.AddDeviceAddress(agvWP1MoveAddress.address, AGVWP1Move);
        MXRequester.Get.AddDeviceAddress(trolleyWP1MoveAddress.address, TrolleyWP1Move);
        MXRequester.Get.AddDeviceAddress(iccuWP1MoveAddress.address, ICCUWP1Move);
        MXRequester.Get.AddDeviceAddress(iccuWP2MoveAddress.address, ICCUWP2Move);
        MXRequester.Get.AddDeviceAddress(carBodyWP1MoveAddress.address, CarBodyWP1Move);
        MXRequester.Get.AddDeviceAddress(carBodyWP2MoveAddress.address, CarBodyWP2Move);
        MXRequester.Get.AddDeviceAddress(carBodyWP3MoveAddress.address, CarBodyWP3Move);
        MXRequester.Get.AddDeviceAddress(carBodyWP4MoveAddress.address, CarBodyWP4Move);

        // 로봇팔 ICCU 결합 명령 등록
        MXRequester.Get.AddDeviceAddress(
            iccuAssemblyCommandAddress.address, ICCUAssemblyCommand);

        Debug.Log("[Pr2Connector] 2차공정 PLC 신호 등록 완료");
    }

    // ==========================================
    // 공통 이동 명령 처리
    // ==========================================

    private void MoveCommand(
        short value,
        int commandIndex,
        Pr2Vehicle vehicle,
        int waypointID,
        DeviceAddress arrivedAddress)
    {
        // PLC 명령 OFF 시 다음 명령을 받을 수 있도록 초기화
        if (value == 0)
        {
            moveCommandReceived[commandIndex] = false;
            return;
        }

        // 같은 명령이 계속 ON인 동안 중복 실행 방지
        if (moveCommandReceived[commandIndex])
            return;

        if (waypointID < 0)
        {
            Debug.LogWarning("[Pr2Connector] " + vehicle + " 목적지 Waypoint ID가 잘못되었습니다.");
            return;
        }

        if (pr2Controller == null)
        {
            Debug.LogError("[Pr2Connector] Pr2Controller가 연결되지 않았습니다.");
            return;
        }

        // 같은 오브젝트가 이미 이동 중이면 명령 보류
        if (pr2Controller.IsBusy(vehicle))
        {
            Debug.LogWarning("[Pr2Connector] " + vehicle + " 이동 중입니다. 이번 명령은 실행하지 않습니다.");
            return;
        }

        bool success = pr2Controller.Go(vehicle, waypointID);

        if (!success)
        {
            Debug.LogError("[Pr2Connector] " + vehicle + " 이동 실패 / 목적지: " + waypointID);
            return;
        }

        moveCommandReceived[commandIndex] = true;
        SetPendingArrivedAddress(vehicle, arrivedAddress);

        Debug.Log("[Pr2Connector] " + vehicle + " 이동 시작 / Waypoint: " + waypointID);
    }

    private void SetPendingArrivedAddress(Pr2Vehicle vehicle, DeviceAddress address)
    {
        if (vehicle == Pr2Vehicle.AGV)
            currentAGVArrivedAddress = address;
        else if (vehicle == Pr2Vehicle.Trolley)
            currentTrolleyArrivedAddress = address;
        else if (vehicle == Pr2Vehicle.ICCU)
            currentICCUArrivedAddress = address;
        else if (vehicle == Pr2Vehicle.CarBody)
            currentCarBodyArrivedAddress = address;
    }

    private DeviceAddress GetPendingArrivedAddress(Pr2Vehicle vehicle)
    {
        if (vehicle == Pr2Vehicle.AGV) return currentAGVArrivedAddress;
        if (vehicle == Pr2Vehicle.Trolley) return currentTrolleyArrivedAddress;
        if (vehicle == Pr2Vehicle.ICCU) return currentICCUArrivedAddress;
        if (vehicle == Pr2Vehicle.CarBody) return currentCarBodyArrivedAddress;
        return null;
    }

    private void ClearPendingArrivedAddress(Pr2Vehicle vehicle)
    {
        if (vehicle == Pr2Vehicle.AGV)
            currentAGVArrivedAddress = null;
        else if (vehicle == Pr2Vehicle.Trolley)
            currentTrolleyArrivedAddress = null;
        else if (vehicle == Pr2Vehicle.ICCU)
            currentICCUArrivedAddress = null;
        else if (vehicle == Pr2Vehicle.CarBody)
            currentCarBodyArrivedAddress = null;
    }

    // ==========================================
    // PLC 이동 명령별 콜백
    // ==========================================

    private void AGVWP1Move(short value)
    {
        MoveCommand(value, 0, Pr2Vehicle.AGV, agvWaypoint1, agvWP1ArrivedAddress);
    }

    private void TrolleyWP1Move(short value)
    {
        MoveCommand(value, 1, Pr2Vehicle.Trolley, trolleyWaypoint1, trolleyWP1ArrivedAddress);
    }

    private void ICCUWP1Move(short value)
    {
        MoveCommand(value, 2, Pr2Vehicle.ICCU, iccuWaypoint1, iccuWP1ArrivedAddress);
    }

    private void ICCUWP2Move(short value)
    {
        MoveCommand(value, 3, Pr2Vehicle.ICCU, iccuWaypoint2, iccuWP2ArrivedAddress);
    }

    private void CarBodyWP1Move(short value)
    {
        MoveCommand(value, 4, Pr2Vehicle.CarBody, carBodyWaypoint1, carBodyWP1ArrivedAddress);
    }

    private void CarBodyWP2Move(short value)
    {
        MoveCommand(value, 5, Pr2Vehicle.CarBody, carBodyWaypoint2, carBodyWP2ArrivedAddress);
    }

    private void CarBodyWP3Move(short value)
    {
        MoveCommand(value, 6, Pr2Vehicle.CarBody, carBodyWaypoint3, carBodyWP3ArrivedAddress);
    }

    private void CarBodyWP4Move(short value)
    {
        MoveCommand(value, 7, Pr2Vehicle.CarBody, carBodyWaypoint4, carBodyWP4ArrivedAddress);
    }

    // ==========================================
    // 이동 완료 이벤트
    // ==========================================

    private void OnAGVCompletedMove()
    {
        StartCoroutine(CompleteMoveFeedback(Pr2Vehicle.AGV));
    }

    private void OnTrolleyCompletedMove()
    {
        StartCoroutine(CompleteMoveFeedback(Pr2Vehicle.Trolley));
    }

    private void OnICCUCompletedMove()
    {
        StartCoroutine(CompleteMoveFeedback(Pr2Vehicle.ICCU));
    }

    private void OnCarBodyCompletedMove()
    {
        StartCoroutine(CompleteMoveFeedback(Pr2Vehicle.CarBody));
    }

    private IEnumerator CompleteMoveFeedback(Pr2Vehicle vehicle)
    {
        DeviceAddress arrivedAddress = GetPendingArrivedAddress(vehicle);

        if (arrivedAddress == null)
        {
            Debug.LogWarning("[Pr2Connector] " + vehicle + " 도착 주소가 지정되지 않았습니다.");
            yield break;
        }

        // 도착 신호 ON
        MXRequester.Get.AddSetDeviceRequest(arrivedAddress.address, (short)1);

        yield return new WaitForSeconds(feedbackTime);

        // 도착 신호 OFF
        MXRequester.Get.AddSetDeviceRequest(arrivedAddress.address, (short)0);

        ClearPendingArrivedAddress(vehicle);

        Debug.Log("[Pr2Connector] " + vehicle + " 도착 피드백 전송 완료");
    }

    // ==========================================
    // 로봇팔 ICCU 결합 명령
    // ==========================================

    private void ICCUAssemblyCommand(short value)
    {
        if (value == 0)
        {
            iccuAssemblyCommandReceived = false;
            return;
        }

        if (iccuAssemblyCommandReceived)
            return;

        iccuAssemblyCommandReceived = true;

        Debug.Log("[Pr2Connector] 로봇팔 ICCU 결합 명령 수신");

        if (onICCUAssembly != null)
            onICCUAssembly.Invoke();
        else
            Debug.LogWarning("[Pr2Connector] onICCUAssembly 이벤트가 연결되지 않았습니다.");
    }

    // 로봇팔 애니메이션 또는 작업 스크립트 완료 시 호출
    public void OnICCUAssemblyCompleted()
    {
        MXRequester.Get.AddSetDeviceRequest(
            iccuAssemblyCompletedAddress.address, (short)1);

        StartCoroutine(ResetAssemblyFeedback());
    }

    private IEnumerator ResetAssemblyFeedback()
    {
        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(
            iccuAssemblyCompletedAddress.address, (short)0);

        Debug.Log("[Pr2Connector] ICCU 결합 완료 피드백 전송");
    }

    private void OnDestroy()
    {
        if (pr2Controller == null)
            return;

        if (pr2Controller.onAGVCompleteMove != null)
            pr2Controller.onAGVCompleteMove.RemoveListener(OnAGVCompletedMove);

        if (pr2Controller.onTrolleyCompleteMove != null)
            pr2Controller.onTrolleyCompleteMove.RemoveListener(OnTrolleyCompletedMove);

        if (pr2Controller.onICCUCompleteMove != null)
            pr2Controller.onICCUCompleteMove.RemoveListener(OnICCUCompletedMove);

        if (pr2Controller.onCarBodyCompleteMove != null)
            pr2Controller.onCarBodyCompleteMove.RemoveListener(OnCarBodyCompletedMove);
    }
}
