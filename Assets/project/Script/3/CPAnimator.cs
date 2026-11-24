using System;
using UnityEngine;

public class CPAnimator : MXObject
{
    public Animator CP;

    public DeviceAddress CPtrigger= new DeviceAddress("M13");

    void Start()
    {
        MXRequester.Get.AddDeviceAddress(CPtrigger.address, Ontrigger);
    }

    private void Ontrigger(short obj)
    {
        if (obj == 1)
            CP.SetTrigger("P1start");
    }
}
