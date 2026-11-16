using UnityEngine;

public class CarbodyanimEvent : MonoBehaviour
{
    [Header("CarBody")]
    public Transform Carbody;

    [Header("목적지 부모")]
    public Transform Carrier;
    public Transform Skid;

    [Header("이동 과정")]
    public Animator animatorCarrier;

    [Header("Battery")]
    public Transform Battery;

    // Carbody를 Carrier의 자식으로 변경
    public void CarbodyToCarrier()
    {
        if (Carbody != null && Carrier != null)
            Carbody.SetParent(Carrier, true);
    }

    // Battery를 Carbody의 자식으로 변경
    public void BatteryToCarbody()
    {
        if (Carbody != null && Battery != null)
            Battery.SetParent(Carbody, true);
    }
}
