using UnityEngine;

public class CameraSwitherC : MonoBehaviour
{
    [Header("전환할 카메라 목록")]
    public Camera[] cameras;

    [Header("시작 카메라 번호")]
    public int startIndex = 0;

    private int currentIndex;

    void Start()
    {
        if (cameras == null || cameras.Length == 0)
            return;

        currentIndex = Mathf.Clamp(startIndex, 0, cameras.Length - 1);

        SwitchCamera(currentIndex);
    }

    // 버튼 클릭 시 다음 카메라로 전환
    public void NextCamera()
    {
        if (cameras == null || cameras.Length == 0)
            return;

        currentIndex++;

        if (currentIndex >= cameras.Length)
            currentIndex = 0;

        SwitchCamera(currentIndex);
    }

    // 카메라 활성화
    private void SwitchCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
                cameras[i].enabled = (i == index);
        }

        currentIndex = index;
    }
}
