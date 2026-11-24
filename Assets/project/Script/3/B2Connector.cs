using UnityEngine;

public class B2Connector : MXObject
{
    [Header("Robot Arm Program")]
    public B2Program b2Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress B2On = new DeviceAddress("M35");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress B2Completed = new DeviceAddress("M75");
    public DeviceAddress B2Start = new DeviceAddress("M79");

    [Header("로봇팔 제어신호")]
    public DeviceAddress B2PositionNum = new DeviceAddress("position No");
    public DeviceAddress B2Positioning   = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(B2On.address, B2ON);
        MXRequester.Get.AddDeviceAddress(B2PositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(B2Positioning.address, StartPositioning);
    }

    private void B2ON(short obj)
    {
        b2Program.IsOn = obj != 0;
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
            if (B2Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(B2Start.address, obj);
        }
    }

    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");
            b2Program.RunProgram(positionId);
            if (B2Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(B2Start.address, 1);

            needPositioning = false;
        }
    }

    public void B2CompletedCommand()
    {
        MXRequester.Get.AddSetDeviceRequest(B2Completed.address, 1);
    }

}
