using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// 차체에 붙여서 사용. 범위(트리거) 안에 부품이 일정 시간 머물면 차체의 자식으로 만든다.
[RequireComponent(typeof(Collider))]
public class BodyAttachZone : MonoBehaviour
{
    [Header("대상 설정")]
    [SerializeField] private LayerMask partLayers = ~0;      // 부품 레이어 (문, 유리, 펜더 등)
    [SerializeField] private Transform attachParent;          // 비워두면 이 오브젝트(차체)가 부모

    [Header("부착 조건")]
    [SerializeField] private float attachDelay = 2f;          // 범위 안에 머물러야 하는 시간(초)
   
    [Header("이벤트")]
    public UnityEvent<Transform> onPartAttached;

    private class Entry
    {
        public int colliderCount;   // 부품에 콜라이더가 여러 개여도 1개 부품으로 처리
        public float timer;
    }

    private readonly Dictionary<Transform, Entry> _inside = new Dictionary<Transform, Entry>();
    private readonly List<Transform> _toAttach = new List<Transform>();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        if (attachParent == null) attachParent = transform;
    }

    private Transform GetPartRoot(Collider other)
    {
        return other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
    }

    private bool IsTarget(Collider other)
    {
        return (partLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsTarget(other)) return;

        Transform part = GetPartRoot(other);
        if (part == attachParent || part.IsChildOf(attachParent)) return; // 이미 부착됨

        if (!_inside.TryGetValue(part, out Entry entry))
        {
            entry = new Entry();
            _inside.Add(part, entry);
        }
        entry.colliderCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsTarget(other)) return;

        Transform part = GetPartRoot(other);
        if (!_inside.TryGetValue(part, out Entry entry)) return;

        entry.colliderCount--;
        if (entry.colliderCount <= 0)
            _inside.Remove(part);   // 범위를 벗어나면 타이머 초기화
    }

    private void Update()
    {
        if (_inside.Count == 0) return;

        _toAttach.Clear();
        foreach (var pair in _inside)
        {
            if (pair.Key == null) continue;

            // 그리퍼가 잡고 있는 동안은 타이머를 0으로 유지 → 놓은 뒤 다시 attachDelay를 채워야 붙음
            if (IsHeldByGripper(pair.Key))
            {
                pair.Value.timer = 0f;
                continue;
            }

            pair.Value.timer += Time.deltaTime;
            if (pair.Value.timer >= attachDelay)
                _toAttach.Add(pair.Key);
        }

        foreach (Transform part in _toAttach)
        {
            Attach(part);
            _inside.Remove(part);
        }
    }

    private void Attach(Transform part)
    {
        if (part == null) return;

        part.SetParent(attachParent, true); // true: 월드 위치/회전 유지

        if (part.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;       // Unity 6 이전 버전이면 rb.velocity
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        onPartAttached?.Invoke(part);
    }

    // 부품이 파괴되었을 때 딕셔너리에 null이 남지 않도록 정리
    private void LateUpdate()
    {
        if (_inside.Count == 0) return;

        List<Transform> dead = null;
        foreach (var key in _inside.Keys)
        {
            if (key == null)
            {
                if (dead == null) dead = new List<Transform>();
                dead.Add(key);
            }
        }
        if (dead != null)
            foreach (var d in dead) _inside.Remove(d);
    }

    private bool IsHeldByGripper(Transform part)
    {
        Transform p = part.parent;
        if (p == null) return false;
        if (part.IsChildOf(attachParent)) return false;   // 이미 차체에 붙은 부품

        return p.GetComponentInParent<GripperToolFilter>() != null
            || p.GetComponentInParent<HolderGripper>() != null;
    }
}