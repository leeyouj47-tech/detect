
using UnityEngine;

public class BatteryPackVisibility : MonoBehaviour
{
    [Header("실제로 표시/숨김 처리할 오브젝트")]
    public GameObject targetObject;

    [Header("초기 표시 여부")]
    public bool initialVisible = false;

    [Header("M524 ON일 때 표시 여부")]
    public bool showWhenM524On = true;

    // Apply the initial state in Awake so it is hidden before the first frame.
    // An AGV cloned by AGVFiller also starts hidden even if the source AGV's cover was already shown.
    // Start may run later (first time the hidden object is shown), so the
    // initial state is applied only once here. Otherwise Start would hide it again.
    private bool initialApplied;

    private void Awake()
    {
        ApplyInitialState();
    }

    private void ApplyInitialState()
    {
        if (initialApplied || targetObject == null)
            return;

        initialApplied = true;
        targetObject.SetActive(initialVisible);
    }

    private void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError(
                $"[{gameObject.name}] Target Object가 연결되지 않았습니다."
            );
            return;
        }

        // 게임 시작 시 초기 상태 적용 (Awake에서 이미 적용했으면 다시 숨기지 않음)
        ApplyInitialState();

        Debug.Log(
            $"[{gameObject.name}] 초기 표시 상태: {initialVisible}"
        );
    }

    public void SetM524State(bool isOn)
    {
        if (targetObject == null)
            return;

        initialApplied = true;
        bool visible = showWhenM524On ? isOn : !isOn;

        targetObject.SetActive(visible);

        Debug.Log(
            $"[{gameObject.name}] M524={isOn}, 표시={visible}"
        );
    }
}
