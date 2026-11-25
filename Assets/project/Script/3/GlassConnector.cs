using System;
using UnityEngine;

public class GlassConnector : MXObject
{
    [Header("Robot Arm Program")]
    public GlassProgram glassProgram;

    [Header("PLC -> Unity : 시작 신호")]
    public DeviceAddress glassOn = new DeviceAddress("M23");
    public DeviceAddress programStart = new DeviceAddress("M48");

    [Header("Unity -> PLC : 완료 신호")]
    public DeviceAddress glassCompleted = new DeviceAddress("M63");
    
    private bool isrunning = false;

    void Start()
    { 
        MXRequester.Get.AddDeviceAddress(glassOn.address, glassON);
        MXRequester.Get.AddDeviceAddress(programStart.address, ProgramStart);
    }

    private void glassON(short obj)
    {
        glassProgram.IsOn = obj != 0;
    }

    private void ProgramStart(short obj)
    {
        if(obj != 0 || !isrunning)
        {
            isrunning = true;
            glassProgram.Program1();
        }
    }

    public void GlassCompletedCommand()
    {
        MXRequester.Get.AddSetDeviceRequest(glassCompleted.address, 1);
        isrunning = false;
    }
}