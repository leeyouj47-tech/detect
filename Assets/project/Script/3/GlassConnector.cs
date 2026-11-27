
using UnityEngine;

public class GlassConnector : MXObject
{
    [Header("Robot Arm Program")]
    public GlassProgram glassProgram;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress glassOn = new DeviceAddress("M318");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress glassCompleted = new DeviceAddress("M320");
    public DeviceAddress glassStart = new DeviceAddress("M321");
    public DeviceAddress glassBusy = new DeviceAddress("M322");

    [Header("로봇팔 제어신호")]
    public DeviceAddress glassPositionNum = new DeviceAddress("position No");
    public DeviceAddress glassPositioning = new DeviceAddress("positioning");

    private int positionId;
    private bool needPositioning;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(glassOn.address, GlassOn);
        MXRequester.Get.AddDeviceAddress(glassPositionNum.address, SetPosition);
        MXRequester.Get.AddDeviceAddress(glassPositioning.address, StartPositioning);
    }

    private void GlassOn(short obj)
    {
        if (obj != 0)
        {
            glassProgram.IsOn = true;
        }
        else
        {
            glassProgram.IsOn = false;
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
            if (glassStart.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassStart.address, obj);
        }
    }

    private void Update()
    {
        if (needPositioning && positionId != 0)
        {
            Debug.Log($"{positionId}번 프로그램 시작");

            MXRequester.Get.AddSetDeviceRequest(glassCompleted.address, 0);

            glassProgram.RunProgram(positionId);

            if (glassStart.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassStart.address, 1);

            if (glassBusy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassBusy.address, 1);

            needPositioning = false;
        }
    }

    public void GlassCompletedCommand(bool isCompleted)
    {
        if (isCompleted)
        {
            MXRequester.Get.AddSetDeviceRequest(glassCompleted.address, 1);

            if (glassBusy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassBusy.address, 0);
        }
        else
        {
            MXRequester.Get.AddSetDeviceRequest(glassCompleted.address, 0);
        }
    }
}
