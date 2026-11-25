using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 위치별 카메라 전환
// - cameras[0]은 기본(Main) 카메라
// - 버튼 OnClick -> SwitchTo(번호)
// - 이미 보고 있는 카메라 버튼을 다시 누르면 기본 카메라로 복귀
// - 활성 카메라만 Camera/이동 스크립트(CameraMovement, FreeCamera 등)를 켬
// - 활성 카메라에 MainCamera 태그를 옮김 (Camera.main을 쓰는 스크립트 대응)
// ==========================================
public class CameraSwitcher : MonoBehaviour
{
    [Header("카메라 목록 (0번 = 기본 카메라)")]
    public Camera[] cameras;

    [Header("시작 카메라 번호")]
    public int startIndex = 0;

    [Header("같은 버튼 다시 누르면 기본 카메라로 복귀")]
    public bool toggleBackToDefault = true;

    public int CurrentIndex { get; private set; } = -1;

    // 씬에서 처음부터 꺼져 있던 스크립트는 전환 시에도 켜지 않음
    // (예: AGV에 부착된 카메라의 FreeCamera)
    private readonly HashSet<MonoBehaviour> initiallyDisabled = new HashSet<MonoBehaviour>();

    private void Awake()
    {
        foreach (Camera cam in cameras)
        {
            if (cam == null)
                continue;

            foreach (MonoBehaviour behaviour in cam.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && !behaviour.enabled)
                    initiallyDisabled.Add(behaviour);
            }
        }
    }

    private void Start()
    {
        Apply(Mathf.Clamp(startIndex, 0, Mathf.Max(0, cameras.Length - 1)));
    }

    public void SwitchTo(int index)
    {
        if (cameras == null || index < 0 || index >= cameras.Length || cameras[index] == null)
        {
            Debug.LogWarning($"[CameraSwitcher] 잘못된 카메라 번호: {index}");
            return;
        }

        if (toggleBackToDefault && index == CurrentIndex && index != 0)
            index = 0;

        Apply(index);
    }

    public void SwitchToDefault() => Apply(0);

    private void Apply(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
                continue;

            bool active = i == index;

            cam.enabled = active;
            cam.tag = active ? "MainCamera" : "Untagged";

            // 이동/조작 스크립트도 활성 카메라만 동작
            foreach (MonoBehaviour behaviour in cam.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour == this || initiallyDisabled.Contains(behaviour))
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "UniversalAdditionalCameraData")
                    continue;

                behaviour.enabled = active;
            }
        }

        CurrentIndex = index;
        Debug.Log($"[CameraSwitcher] 카메라 전환: {cameras[index].name}");
    }
}
