using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class C2Connector : MXObject
{
    [Header("Robot Arm Program")]
    public C2Program c2Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress C2On = new DeviceAddress("M34");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress C2Completed =  new DeviceAddress("M74");
    public DeviceAddress C2Start =  new DeviceAddress("M81");
    [Header("로봇팔 제어신호")]
    public DeviceAddress C2PositionNum = new DeviceAddress("position No");
    public DeviceAddress C2Positioning = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(C2On.address, C2ON);
        MXRequester.Get.AddDeviceAddress(C2PositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(C2Positioning.address, StartPositioning);
    }

    private void C2ON(short obj)
    {
        c2Program.IsOn = obj != 0;
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
            if (C2Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(C2Start.address, obj);
        }
    }

    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");
            c2Program.RunProgram(positionId);
            if (C2Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(C2Start.address, 1);

            needPositioning = false;
        }
    }

    public void C2CompletedCommand()
    {
        MXRequester.Get.AddSetDeviceRequest(C2Completed.address, 1);
    }

}
