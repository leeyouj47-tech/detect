using System;
using UnityEngine;

public class CPAnimator : MXObject
{
    public Animator CPAnim;

    public DeviceAddress CPtrigger= new DeviceAddress("M13");
    public DeviceAddress CPAssembled = new DeviceAddress("M53");

    private bool isAssembled = false;

    void Start()
    {
        MXRequester.Get.AddDeviceAddress(CPtrigger.address, Ontrigger);
    }

    private void Ontrigger(short obj)
    {
        if (obj == 1)
            CPAnim.SetTrigger("P1start");
    }

    public void OnCPAnimationEnd()
    {
        if (isAssembled)
            return;

        isAssembled = true;

        MXRequester.Get.AddSetDeviceRequest(CPAssembled.address, 1);
    }

    public void ResetAssembly()
    {
        isAssembled = false;

        MXRequester.Get.AddSetDeviceRequest(CPAssembled.address, 0);
    }

}
