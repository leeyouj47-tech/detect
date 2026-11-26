using UnityEngine;

public class C1Connector : MXObject
{
    [Header("Robot Arm Program")]
    public C1Program c1Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress C1On = new DeviceAddress("M255"); //시작

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress C1Completed = new DeviceAddress("M284"); //최종완료
    public DeviceAddress C1Start = new DeviceAddress("M290"); //기동완료
    public DeviceAddress C1Busy = new DeviceAddress("M316");

    [Header("로봇팔 제어신호")]
    public DeviceAddress C1PositionNum = new DeviceAddress("position No");
    public DeviceAddress C1Positioning = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(C1On.address, C1ON);
        MXRequester.Get.AddDeviceAddress(C1PositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(C1Positioning.address, StartPositioning);
    }

    private void C1ON(short obj)
    {
        if(obj != 0)
        {
            c1Program.IsOn = true;
        }
        else
        {
            c1Program.IsOn = false;
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
            if (C1Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(C1Start.address, obj);
        }
    }

    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");
            MXRequester.Get.AddSetDeviceRequest(C1Completed.address, 0);
            c1Program.RunProgram(positionId);
            if (C1Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(C1Start.address, 1);
            if (C1Busy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(C1Busy.address, 1);

            needPositioning = false;
        }
    }
        
    public void C1CompletedCommand(bool isCompleted)
    {
        if (isCompleted)
        {
            MXRequester.Get.AddSetDeviceRequest(C1Completed.address, 1);
            if (C1Busy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(C1Busy.address, 0);
        }
        else
        {
            MXRequester.Get.AddSetDeviceRequest(C1Completed.address, 0);
        }
    }
}
