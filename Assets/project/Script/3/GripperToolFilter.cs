using System.Collections.Generic;
using UnityEngine;

public class GripperToolFilter : ToolParent
{
    public Animator anim;
    public LayerMask grabMask;   // 추가: 잡을 수 있는 레이어
    public List<Rigidbody> triggerList = new List<Rigidbody>();
    private Rigidbody grabbed;

    protected override void OnChangedValue(bool isOn)
    {
        anim.SetBool("IsGrab", isOn);
    }

    public void Grab()
    {
        if (grabbed == null && triggerList.Count > 0)
        {
            grabbed = triggerList[0];
            grabbed.isKinematic = true;
            grabbed.transform.SetParent(transform);
        }
    }

    public void Release()
    {
        if (grabbed != null)
        {
            grabbed.transform.SetParent(null);
            grabbed.isKinematic = false;
            grabbed = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 추가: 레이어 마스크에 포함되지 않으면 무시
        if ((grabMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (other.attachedRigidbody == null)
            return;

        if (triggerList.Contains(other.attachedRigidbody))
            return;

        triggerList.Add(other.attachedRigidbody);

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == null)
            return;

        if (!triggerList.Contains(other.attachedRigidbody))
            return;

        triggerList.Remove(other.attachedRigidbody);
    }
}

