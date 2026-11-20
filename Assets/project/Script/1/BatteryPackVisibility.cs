
using UnityEngine;

public class BatteryPackVisibility : MonoBehaviour
{
    [Header("실제로 표시/숨김 처리할 오브젝트")]
    public GameObject targetObject;

    [Header("초기 표시 여부")]
    public bool initialVisible = false;

    [Header("M524 ON일 때 표시 여부")]
    public bool showWhenM524On = true;

    private void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError(
                $"[{gameObject.name}] Target Object가 연결되지 않았습니다."
            );
            return;
        }

        // 게임 시작 시 초기 상태 적용
        targetObject.SetActive(initialVisible);

        Debug.Log(
            $"[{gameObject.name}] 초기 표시 상태: {initialVisible}"
        );
    }

    public void SetM524State(bool isOn)
    {
        if (targetObject == null)
            return;

        bool visible = showWhenM524On ? isOn : !isOn;

        targetObject.SetActive(visible);

        Debug.Log(
            $"[{gameObject.name}] M524={isOn}, 표시={visible}"
        );
    }
}
