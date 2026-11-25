using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 2공정 PLC <-> Unity 신호 처리
// 주소표: docu/공정비트.xlsx (sheet4)
//
// 명령 처리 방식
// - PLC 명령 비트가 ON이고 아직 처리하지 않았으면 매 프레임 실행 시도
//   (장비가 이동 중이면 끝날 때까지 기다렸다가 실행 -> 명령 유실 없음)
// - 명령 비트가 OFF가 되면 다음 ON을 받을 준비
// - 도착/완료 신호는 feedbackTime 동안 ON 후 OFF (펄스)
// ==========================================
public class Pr2Connector : MXObject
{
    [Header("전체 컨트롤러")]
    public Pr2Controller controller;

    [Header("공통 설정")]
    public float feedbackTime = 0.3f;

    // ==========================================
    // PLC 명령 번호 -> Unity Waypoint ID
    // ==========================================

    [Header("Waypoint ID 매핑 (PLC WP1, WP2... 순서)")]
    public int[] agvWaypointIDs = { 1, 2, 3, 4 };
    public int[] railWaypointIDs = { 1 };
    public int[] iccuWaypointIDs = { 1, 2 };
    [Tooltip("PLC 리프트 WP1~4 -> Unity 리프트 WP0~3 (WP4는 AGV 빠지기 후 자동 이동)")]
    public int[] liftWaypointIDs = { 0, 1, 2, 3 };

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
    public DeviceAddress liftWP4Command = new DeviceAddress("M830");

    // ==========================================
    // PLC -> Unity : 작업 명령
    // ==========================================

    [Header("작업 명령")]
    public DeviceAddress batteryCouplingCommand = new DeviceAddress("M831");
    public DeviceAddress agvLeaveCommand = new DeviceAddress("M832");
    public DeviceAddress robotArmCommand = new DeviceAddress("M833");
    public DeviceAddress iccuInspectionCommand = new DeviceAddress("M834");
    public DeviceAddress chassisInspectionCommand = new DeviceAddress("M835");

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
    // 내부 상태
    // ==========================================

    private enum ExecResult { Done, Busy, Failed }

    private class Command
    {
        public string name;
        public short value;
        public bool handled;
        public bool busyLogged;
        public Func<ExecResult> execute;
    }

    private readonly List<Command> commands = new List<Command>();

    // 이동 명령을 받은 장비 -> 도착 시 보낼 주소
    private readonly Dictionary<Pr2Route, DeviceAddress> pendingArrival =
        new Dictionary<Pr2Route, DeviceAddress>();

    private bool faulted;

    // ==========================================
    // 초기 설정
    // ==========================================

    private void Start()
    {
        if (controller == null)
        {
            Debug.LogError("[Pr2Connector] Pr2Controller가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        if (controller.agv == null || controller.lift == null ||
            controller.rail == null || controller.iccu == null)
        {
            Debug.LogError("[Pr2Connector] 장치별 Route가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        if (MXRequester.Get == null)
        {
            Debug.LogError("[Pr2Connector] 씬에 MXRequester가 없습니다.");
            enabled = false;
            return;
        }

        controller.agv.Arrived += OnAGVArrived;
        controller.lift.Arrived += OnLiftArrived;
        controller.rail.Arrived += OnRailArrived;
        controller.iccu.Arrived += OnICCUArrived;

        // 이동 명령
        RegisterMove("AGV WP1", agvWP1Command, controller.agv, agvWaypointIDs, 0, agvWP1Arrived);
        RegisterMove("AGV WP2", agvWP2Command, controller.agv, agvWaypointIDs, 1, agvWP2Arrived);
        RegisterMove("AGV WP3", agvWP3Command, controller.agv, agvWaypointIDs, 2, agvWP3Arrived);
        RegisterMove("AGV WP4", agvWP4Command, controller.agv, agvWaypointIDs, 3, agvWP4Arrived);

        RegisterMove("Rail WP1", railWP1Command, controller.rail, railWaypointIDs, 0, railWP1Arrived);

        RegisterMove("ICCU WP1", iccuWP1Command, controller.iccu, iccuWaypointIDs, 0, iccuWP1Arrived);
        RegisterMove("ICCU WP2", iccuWP2Command, controller.iccu, iccuWaypointIDs, 1, iccuWP2Arrived);

        RegisterMove("Lift WP1", liftWP1Command, controller.lift, liftWaypointIDs, 0, liftWP1Arrived);
        RegisterMove("Lift WP2", liftWP2Command, controller.lift, liftWaypointIDs, 1, liftWP2Arrived);
        RegisterMove("Lift WP3", liftWP3Command, controller.lift, liftWaypointIDs, 2, liftWP3Arrived);
        RegisterMove("Lift WP4", liftWP4Command, controller.lift, liftWaypointIDs, 3, liftWP4Arrived);

        // 작업 명령
        Register("배터리 결합", batteryCouplingCommand, BatteryCoupling);
        Register("AGV 빠지기", agvLeaveCommand, AGVLeave);
        Register("로봇팔 동작", robotArmCommand, RobotArm);
        Register("ICCU 검사", iccuInspectionCommand, ICCUInspection);
        Register("차체 검사", chassisInspectionCommand, ChassisInspection);

        // 공정 이상
        MXRequester.Get.AddDeviceAddress(processFaultAddress.address, OnProcessFault);

        Debug.Log("[Pr2Connector] PLC 주소 등록 완료");
    }

    private void Register(string commandName, DeviceAddress address, Func<ExecResult> execute)
    {
        if (address == null || string.IsNullOrEmpty(address.address))
        {
            Debug.LogError($"[Pr2Connector] {commandName} 주소가 비어 있습니다.");
            return;
        }

        var command = new Command { name = commandName, execute = execute };
        commands.Add(command);

        MXRequester.Get.AddDeviceAddress(address.address, value =>
        {
            command.value = value;

            if (value == 0)
            {
                command.handled = false;
                command.busyLogged = false;
            }
        });
    }

    private void RegisterMove(
        string commandName, DeviceAddress address, Pr2Route route,
        int[] waypointIDs, int index, DeviceAddress arrivedAddress)
    {
        Register(commandName, address, () =>
        {
            if (waypointIDs == null || index >= waypointIDs.Length)
            {
                Debug.LogError($"[Pr2Connector] {commandName}: Waypoint ID 매핑이 없습니다.");
                return ExecResult.Failed;
            }

            if (route.IsMoving())
                return ExecResult.Busy;

            if (!route.Go(waypointIDs[index]))
                return ExecResult.Failed;

            pendingArrival[route] = arrivedAddress;
            return ExecResult.Done;
        });
    }

    // ==========================================
    // 명령 실행 (매 프레임)
    // ==========================================

    private void Update()
    {
        if (faulted)
            return;

        foreach (Command command in commands)
        {
            if (command.value == 0 || command.handled)
                continue;

            ExecResult result = command.execute();

            switch (result)
            {
                case ExecResult.Done:
                    command.handled = true;
                    Debug.Log($"[Pr2Connector] 명령 실행: {command.name}");
                    break;

                case ExecResult.Failed:
                    command.handled = true;
                    Debug.LogError($"[Pr2Connector] 명령 실행 실패: {command.name}");
                    break;

                case ExecResult.Busy:
                    if (!command.busyLogged)
                    {
                        command.busyLogged = true;
                        Debug.Log($"[Pr2Connector] 장비 동작 중, 대기: {command.name}");
                    }
                    break;
            }

            if (faulted)
                return;
        }
    }

    // ==========================================
    // 이동 완료 처리
    // ==========================================

    private void OnAGVArrived(int id) => OnRouteArrived(controller.agv);
    private void OnLiftArrived(int id) => OnRouteArrived(controller.lift);
    private void OnRailArrived(int id) => OnRouteArrived(controller.rail);
    private void OnICCUArrived(int id) => OnRouteArrived(controller.iccu);

    private void OnRouteArrived(Pr2Route route)
    {
        if (!pendingArrival.TryGetValue(route, out DeviceAddress address))
            return;

        pendingArrival.Remove(route);

        Debug.Log($"[Pr2Connector] 도착 신호: {route.name} -> {address.address}");
        SendFeedback(address);
    }

    // ==========================================
    // 작업 명령
    // ==========================================

    private ExecResult BatteryCoupling()
    {
        controller.RunBatteryCoupling(() => SendFeedback(batteryCouplingCompleted));
        return ExecResult.Done;
    }

    private ExecResult AGVLeave()
    {
        if (!controller.CanRunAGVLeave())
            return ExecResult.Busy;

        controller.RunAGVLeave(() => SendFeedback(agvLeaveCompleted));
        return ExecResult.Done;
    }

    private ExecResult RobotArm()
    {
        return controller.RunRobotArm(() => SendFeedback(robotArmCompleted))
            ? ExecResult.Done
            : ExecResult.Busy;
    }

    private ExecResult ICCUInspection()
    {
        ClearICCUResult();

        controller.RunICCUInspection(good =>
        {
            if (good) ICCUInspectionGood();
            else ICCUInspectionBad();
        });

        return ExecResult.Done;
    }

    private ExecResult ChassisInspection()
    {
        ClearChassisResult();

        controller.RunChassisInspection(good =>
        {
            if (good) ChassisInspectionGood();
            else ChassisInspectionBad();
        });

        return ExecResult.Done;
    }

    // ==========================================
    // Unity -> PLC : 완료 신호 (펄스)
    // ==========================================

    public void SendFeedback(DeviceAddress address)
    {
        if (address == null || string.IsNullOrEmpty(address.address))
            return;

        StartCoroutine(SendFeedbackCoroutine(address));
    }

    private IEnumerator SendFeedbackCoroutine(DeviceAddress address)
    {
        MXRequester.Get.AddSetDeviceRequest(address.address, (short)1);

        yield return new WaitForSeconds(feedbackTime);

        MXRequester.Get.AddSetDeviceRequest(address.address, (short)0);
    }

    // 외부(Animation Event, 버튼 등)에서 수동 호출용
    public void OnBatteryCouplingCompleted() => SendFeedback(batteryCouplingCompleted);
    public void OnAGVLeaveCompleted() => SendFeedback(agvLeaveCompleted);
    public void OnRobotArmCompleted() => SendFeedback(robotArmCompleted);

    // ==========================================
    // ICCU 검사 결과 (결과 비트는 다음 검사 시작까지 유지)
    // ==========================================

    public void ICCUInspectionGood()
    {
        MXRequester.Get.AddSetDeviceRequest(iccuGood.address, (short)1);
        MXRequester.Get.AddSetDeviceRequest(iccuBad.address, (short)0);
        SendFeedback(iccuInspectionCompleted);

        Debug.Log("[ICCU] 정상품 판정");
    }

    public void ICCUInspectionBad()
    {
        MXRequester.Get.AddSetDeviceRequest(iccuGood.address, (short)0);
        MXRequester.Get.AddSetDeviceRequest(iccuBad.address, (short)1);
        SendFeedback(iccuInspectionCompleted);

        Debug.LogWarning("[ICCU] 불량품 판정");
    }

    public void ClearICCUResult()
    {
        MXRequester.Get.AddSetDeviceRequest(iccuGood.address, (short)0);
        MXRequester.Get.AddSetDeviceRequest(iccuBad.address, (short)0);
    }

    // ==========================================
    // 차체 검사 결과
    // ==========================================

    public void ChassisInspectionGood()
    {
        MXRequester.Get.AddSetDeviceRequest(chassisGood.address, (short)1);
        MXRequester.Get.AddSetDeviceRequest(chassisBad.address, (short)0);
        SendFeedback(chassisInspectionCompleted);

        Debug.Log("[차체] 정상품 판정");
    }

    public void ChassisInspectionBad()
    {
        MXRequester.Get.AddSetDeviceRequest(chassisGood.address, (short)0);
        MXRequester.Get.AddSetDeviceRequest(chassisBad.address, (short)1);
        SendFeedback(chassisInspectionCompleted);

        Debug.LogWarning("[차체] 불량품 판정");
    }

    public void ClearChassisResult()
    {
        MXRequester.Get.AddSetDeviceRequest(chassisGood.address, (short)0);
        MXRequester.Get.AddSetDeviceRequest(chassisBad.address, (short)0);
    }

    // ==========================================
    // PLC 공정 이상 (M717) -> 전체 장치 정지
    // ==========================================

    private void OnProcessFault(short value)
    {
        bool isFault = value != 0;

        if (isFault && !faulted)
        {
            faulted = true;
            pendingArrival.Clear();
            controller.StopAllDevices();

            Debug.LogError("[Pr2Connector] 공정 이상 신호 수신. 전체 공정 정지");
        }
        else if (!isFault && faulted)
        {
            faulted = false;
            Debug.Log("[Pr2Connector] 공정 이상 신호 OFF");
        }
    }

    public bool IsFaulted() => faulted;

    // ==========================================
    // 종료 처리
    // ==========================================

    private void OnDestroy()
    {
        if (controller == null)
            return;

        if (controller.agv != null) controller.agv.Arrived -= OnAGVArrived;
        if (controller.lift != null) controller.lift.Arrived -= OnLiftArrived;
        if (controller.rail != null) controller.rail.Arrived -= OnRailArrived;
        if (controller.iccu != null) controller.iccu.Arrived -= OnICCUArrived;
    }
}
