
using UnityEngine;

public class A1Connector : MXObject
{
    [Header("Robot Arm Program")]
    public A1Program a1Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress a1On = new DeviceAddress("M251");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress a1Completed = new DeviceAddress("M280");
    public DeviceAddress a1Start = new DeviceAddress("M286");
    public DeviceAddress a1Busy = new DeviceAddress("M312");

    [Header("로봇팔 제어신호")]
    public DeviceAddress a1PositionNum = new DeviceAddress("position No");
    public DeviceAddress a1Positioning = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(a1On.address, A1On);
        MXRequester.Get.AddDeviceAddress(a1PositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(a1Positioning.address, StartPositioning);
    }

    private void A1On(short obj)
    {
        if(obj != 0)
        {
            a1Program.IsOn = true;
        }
        else
        {
            a1Program.IsOn = false;
        }
    }

    private void SetPosition(short obj)
    {
        positionId = obj;
        Debug.Log($"{positionId}번으로 설정됨");
    }

    private void StartPositioning(short obj)
    {
        if (obj != 0)
        {
            needPositioning = true;
        }
        else
        {
            if (a1Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(a1Start.address, obj);
        }
    }

    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");
            MXRequester.Get.AddSetDeviceRequest(a1Completed.address, 0);
            a1Program.RunProgram(positionId);
            if (a1Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(a1Start.address, 1);
            if (a1Busy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(a1Busy.address, 1);

            needPositioning = false;
        }
    }

    public void A1CompletedCommand(bool isCompleted)
    {
        if (isCompleted)
        {
            MXRequester.Get.AddSetDeviceRequest(a1Completed.address, 1);
            if (a1Busy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(a1Busy.address, 0);
        }
        else
        {
            MXRequester.Get.AddSetDeviceRequest(a1Completed.address, 0);
        }
    }
}
