using UnityEngine;

public class A2Connector : MonoBehaviour
{
    [Header("Robot Arm Program")]
    public A2Program A2Program;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress A2On = new DeviceAddress("M32");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress A2completed = new DeviceAddress("M72");
    public DeviceAddress A2Start = new DeviceAddress("M77");

    [Header("로봇팔 제어신호")]
    public DeviceAddress A2PositionNum = new DeviceAddress("position No");
    public DeviceAddress A2Positioning = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(A2On.address, A2ON);
        MXRequester.Get.AddDeviceAddress(A2PositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(A2Positioning.address, StartPositioning);
    }

    private void A2ON(short obj)
    {
        A2Program.IsOn = obj != 0;
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
            if (A2Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(A2Start.address, obj);
        }
    }

    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");
            A2Program.RunProgram(positionId);
            if (A2Start.useDevice)
                MXRequester.Get.AddSetDeviceRequest(A2Start.address, 1);

            needPositioning = false;
        }
    }

    public void A2CompletedCommand()
    {
        MXRequester.Get.AddSetDeviceRequest(A2completed.address, 1);
    }

}
