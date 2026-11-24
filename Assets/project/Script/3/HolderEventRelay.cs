using UnityEngine;

public class HolderEventRelay : MonoBehaviour
{
    [SerializeField] HolderTrigger holder;   // 인스펙터에서 깊은 자식을 드래그해서 연결

    // 애니메이션 이벤트에서 호출할 함수
    public void ReleaseHolder()
    {
        if (holder != null) holder.Release();
    }
}