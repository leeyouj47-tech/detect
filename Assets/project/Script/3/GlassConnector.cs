using UnityEngine;

public class GlassConnector : MXObject
{
    [Header("Robot Arm Program")]
    public GlassProgram glassProgram;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress glassOn = new DeviceAddress("M318");
    public DeviceAddress programStart = new DeviceAddress("M319");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress glassCompleted = new DeviceAddress("M320");
    public DeviceAddress glassStarted = new DeviceAddress("M321");
    public DeviceAddress glassBusy = new DeviceAddress("M322");

    private const int GlassProgramNumber = 1;
    private bool needStart;

    private void Start()
    {
        MXRequester.Get.AddDeviceAddress(glassOn.address, GlassOn);
        MXRequester.Get.AddDeviceAddress(programStart.address, ProgramStart);
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

    private void ProgramStart(short obj)
    {
        if (obj != 0)
        {
            needStart = true;
        }
        else
        {
            if (glassStarted.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassStarted.address, obj);
        }
    }

    private void Update()
    {
        if (needStart)
        {
            Debug.Log("유리 부착 프로그램 시작");
            MXRequester.Get.AddSetDeviceRequest(glassCompleted.address, 0);
            glassProgram.RunProgram(GlassProgramNumber);
            if (glassStarted.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassStarted.address, 1);
            if (glassBusy.useDevice)
                MXRequester.Get.AddSetDeviceRequest(glassBusy.address, 1);

            needStart = false;
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
