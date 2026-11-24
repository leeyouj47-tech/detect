using UnityEngine;

public class FEMAnimator : MXObject
{
    public Animator FEM;

    public DeviceAddress FEMtrigger = new DeviceAddress("M19");

    void Start()
    {
        MXRequester.Get.AddDeviceAddress(FEMtrigger.address, Ontrigger);
    }

    private void Ontrigger(short obj)
    {
        if (obj == 1)
            FEM.SetTrigger("P1 start");
    }
}
