using UnityEngine;

public class CarbodyEvent : MonoBehaviour
{
    public void ChangeParent(Transform newParent)
    {
        if (newParent == null)
        {
            Debug.LogWarning("새로운 부모가 지정되지 않았습니다.");
            return;
        }

        // 월드 위치와 회전을 유지하면서 부모 변경
        transform.SetParent(newParent, true);

        Debug.Log($"부모 변경 완료: {newParent.name}");
    }
}
