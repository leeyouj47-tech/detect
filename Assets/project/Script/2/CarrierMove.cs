using UnityEngine;

public class CarrierMove : MonoBehaviour
{
    public Animator carrier;
    public void PlayCarrier()
    {
        if (carrier != null)
            carrier.Play("LiftCarrier", 0, 0f);
    }
}
