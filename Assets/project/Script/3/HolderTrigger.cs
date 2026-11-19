using System;
using System.Collections.Generic;
using UnityEngine;

public class HolderTrigger : MonoBehaviour
{
    [SerializeField] LayerMask grabbableMask;
    [SerializeField] Transform grabPoint;       // 잡은 물체의 부모. 비우면 이 오브젝트
    [SerializeField] Collider[] releaseZones;   // 전부 그리퍼 콜라이더 안에 들어오면 놓는다
    [SerializeField] ServoAmp[] servos;         // 하나라도 원점 복귀 중이면 잡지 않는다

    // 콜라이더가 여러 개인 물체는 "처음 닿았을 때" 한 번만 잡도록 접촉 수를 센다
    readonly Dictionary<Rigidbody, int> contactCount = new Dictionary<Rigidbody, int>();
    readonly HashSet<Collider> enteredZones = new HashSet<Collider>();

    Rigidbody heldRb;

    Transform Parent => grabPoint != null ? grabPoint : transform;

    public bool IsHolding => heldRb != null;

    public void Release()
    {
        if (heldRb == null) return;

        heldRb.transform.SetParent(null, true);
        heldRb = null;
    }

    void Grab(Rigidbody rb)
    {
        heldRb = rb;
        rb.transform.SetParent(Parent, true);
    }

    void OnTriggerEnter(Collider other)
    {
        // 놓을 위치 콜라이더: 전부 들어오면 놓기
        if (Array.IndexOf(releaseZones, other) >= 0)
        {
            enteredZones.Add(other);
            if (enteredZones.Count >= releaseZones.Length) Release();
            return;
        }

        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if ((grabbableMask.value & (1 << other.gameObject.layer)) == 0) return;

        contactCount.TryGetValue(rb, out int count);
        contactCount[rb] = count + 1;
        if (count == 0 && heldRb == null && !IsAnyHoming()) Grab(rb);   // 새 물체가 처음 들어왔을 때만 잡기
    }

    bool IsAnyHoming()
    {
        foreach (var servo in servos)
            if (servo != null && servo.IsHoming) return true;
        return false;
    }

    void OnTriggerExit(Collider other)
    {
        if (enteredZones.Remove(other)) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (!contactCount.TryGetValue(rb, out int count)) return;

        if (count > 1) contactCount[rb] = count - 1;
        else contactCount.Remove(rb);
    }

    void OnDisable()
    {
        contactCount.Clear();
        enteredZones.Clear();
    }
}