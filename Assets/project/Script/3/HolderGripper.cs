using System.Collections.Generic;
using UnityEngine;

public class HolderGripper : MonoBehaviour
{
    [SerializeField] LayerMask grabbableMask;
    [SerializeField] Transform grabPoint;   // 잡은 물체의 부모. 비우면 이 오브젝트
    public List<Rigidbody> triggerList = new List<Rigidbody>();

    [Header("자동 잡기 (범위 안에 일정 시간 머물면 자식으로)")]
    [SerializeField] bool autoGrab = true;
    [SerializeField] float grabDelay = 1f;
    [SerializeField] Transform bodyRoot;    // 지정하면 이 하위(차체에 이미 붙은 부품)는 잡기 대상에서 제외

    // 콜라이더가 여러 개인 물체가 일부만 나가도 리스트에서 빠지지 않도록 접촉 수를 센다
    readonly Dictionary<Rigidbody, int> contactCount = new Dictionary<Rigidbody, int>();

    Transform held;
    Rigidbody heldRb;
    bool wasKinematic;

    Rigidbody candidate;       // 자동 잡기 후보
    float candidateTimer;
    Rigidbody ignoreRb;        // 방금 놓은 물체: 범위를 벗어나기 전까지 자동 잡기 제외

    Transform Parent => grabPoint != null ? grabPoint : transform;

    void Update()
    {
        if (!autoGrab || held != null) { ResetCandidate(); return; }

        Rigidbody next = FindGrabbable(true);
        if (next == null) { ResetCandidate(); return; }

        if (next != candidate) { candidate = next; candidateTimer = 0f; }   // 후보가 바뀌면 초기화

        candidateTimer += Time.deltaTime;
        if (candidateTimer >= grabDelay)
        {
            GrabRb(next);
            ResetCandidate();
        }
    }

    void ResetCandidate() { candidate = null; candidateTimer = 0f; }

    public void Grab() { TryGrab(); }   // UnityEvent 등에서 void로 호출용

    public bool TryGrab()
    {
        if (held != null) return false;

        Rigidbody rb = FindGrabbable(false);
        if (rb == null) return false;

        GrabRb(rb);
        return true;
    }

    public void Release()
    {
        if (held == null) return;

        held.SetParent(null, true);
        if (heldRb) heldRb.isKinematic = wasKinematic;
        ignoreRb = (heldRb != null && contactCount.ContainsKey(heldRb)) ? heldRb : null;  // 놓은 직후 다시 잡히는 것 방지
        held = null; heldRb = null;
    }

    void GrabRb(Rigidbody rb)
    {
        held = rb.transform;
        heldRb = rb;
        wasKinematic = rb.isKinematic; rb.isKinematic = true;
        held.SetParent(Parent, true);
    }

    // triggerList에서 먼저 들어온 순서대로 잡을 수 있는 물체를 찾는다
    Rigidbody FindGrabbable(bool forAuto)
    {
        for (int i = triggerList.Count - 1; i >= 0; i--)    // 파괴된 물체 정리
        {
            if (triggerList[i] == null)
            {
                contactCount.Remove(triggerList[i]);
                triggerList.RemoveAt(i);
            }
        }

        foreach (var rb in triggerList)
        {
            if (bodyRoot != null && rb.transform.IsChildOf(bodyRoot)) continue;   // 차체에 붙은 부품 제외
            if (forAuto && rb == ignoreRb) continue;                              // 자동 잡기에서만 제외
            return rb;
        }
        return null;
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if ((grabbableMask.value & (1 << rb.gameObject.layer)) == 0) return;   // 루트 레이어 기준

        contactCount.TryGetValue(rb, out int count);
        contactCount[rb] = count + 1;
        if (count == 0) triggerList.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (!contactCount.TryGetValue(rb, out int count)) return;

        if (count > 1) { contactCount[rb] = count - 1; return; }

        contactCount.Remove(rb);
        triggerList.Remove(rb);
        if (rb == ignoreRb) ignoreRb = null;   // 범위를 벗어나면 제외 해제
    }

    void OnDisable()
    {
        triggerList.Clear();
        contactCount.Clear();
        ResetCandidate();
    }
}