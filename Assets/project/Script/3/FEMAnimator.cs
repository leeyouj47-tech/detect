using UnityEngine;

public class FEMAnimator : MXObject
{
    public Animator FEMAnim;

    public DeviceAddress FEMtrigger = new DeviceAddress("M19");
    public DeviceAddress FEMAssembled = new DeviceAddress("M59");

    private bool isAssembled = false;
    void Start()
    {
        MXRequester.Get.AddDeviceAddress(FEMtrigger.address, Ontrigger);
    }

    private void Ontrigger(short obj)
    {
        if (obj == 1)
            FEMAnim.SetTrigger("P1 start");
    }

    public void OnFEMAnimationEnd()
    {
        if (isAssembled)
            return;

        isAssembled = true;

        MXRequester.Get.AddSetDeviceRequest(FEMAssembled.address, 1);
    }

    public void ResetAssembly()
    {
        isAssembled = false;

        MXRequester.Get.AddSetDeviceRequest(FEMAssembled.address, 0);
    }
}
