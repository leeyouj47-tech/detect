using UnityEngine;

public class B1Connector : MXObject
{
    [Header("Robot Arm Program")]
    public B1Program b1Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress B1On = new DeviceAddress("M28");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress B1Completed = new DeviceAddress("M68");
    public DeviceAddress B1Start = new DeviceAddress("M78");

    [Header("로봇팔 제어신호")]
    public DeviceAddress B1PositionNum = new DeviceAddress("position No");
    public DeviceAddress B1Positioning = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(B1On.address, B1ON);
        MXRequester.Get.AddDeviceAddress(B1PositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(B1Positioning.address, StartPositioning);
    }

    private void B1ON(short obj)
    {
        b1Program.IsOn = obj != 0;
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
            if (B1Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(B1Start.address, obj);
        }
    }
    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");
            b1Program.RunProgram(positionId);
            if (B1Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(B1Start.address, 1);

            needPositioning = false;
        }
    }

    public void B1CompletedCommand()
    {
        MXRequester.Get.AddSetDeviceRequest(B1Completed.address, 1);
    }
}
