using UnityEngine;

// ==========================================
// 검사라인 초록불 검사 연출
// - 검사 대상(target)이 검사 위치(inspectionPoint)에 들어오면 초록 조명 ON
// - scanDuration 동안 초록 스캔 바가 대상 위를 왕복하며 검사하는 것처럼 보이게 함
// - 검사 시간이 끝나면 초록불은 켜진 채 유지, 대상이 검사 위치를 벗어나면 OFF
// ==========================================
public class InspectionLight : MonoBehaviour
{
    [Header("검사 대상 / 검사 위치")]
    public Transform target;
    public Transform inspectionPoint;

    [Tooltip("true: target 대신 검사 위치에 들어온 AGV(Pr1Controller)를 자동으로 찾음 (Filler로 생성된 AGV 포함)")]
    public bool trackAGVs = false;

    [Tooltip("검사 위치와의 수평 거리가 이 값 이하이면 검사 위치에 들어온 것으로 판단")]
    public float detectRadius = 0.3f;

    [Header("조명")]
    public Color lightColor = new Color(0.1f, 1f, 0.25f);
    public float lightHeight = 1.6f;
    public float lightIntensity = 8f;
    public float lightRange = 5f;
    public float spotAngle = 70f;

    [Header("스캔 연출")]
    public float scanDuration = 3f;
    public float scanSpeed = 1.5f;
    [Tooltip("스캔 바가 왕복하는 거리 (대상 중심 기준 ±)")]
    public float scanHalfLength = 0.85f;
    public Vector3 scanBarSize = new Vector3(1.9f, 0.015f, 0.04f);
    public float scanBarHeight = 0.12f;

    public bool IsInspecting { get; private set; }

    private Light spotLight;
    private GameObject lamp;
    private GameObject scanBar;
    private bool targetInside;
    private float inspectStartTime;

    private void Awake()
    {
        CreateVisuals();
        SetVisible(false, false);
    }

    private void Update()
    {
        bool inside = IsTargetInside();

        if (inside && !targetInside)
            BeginInspection();
        else if (!inside && targetInside)
            EndInspection();

        targetInside = inside;

        if (!targetInside)
            return;

        // 대상이 멈춘 위치 위에 조명 배치
        Vector3 basePos = target.position;
        transform.position = basePos + Vector3.up * lightHeight;

        float elapsed = Time.time - inspectStartTime;
        IsInspecting = elapsed < scanDuration;

        if (IsInspecting)
        {
            // 스캔 바 왕복 + 조명 깜빡임
            float offset = Mathf.Sin(elapsed * scanSpeed * Mathf.PI) * scanHalfLength;
            scanBar.transform.position = basePos + Vector3.up * scanBarHeight + Vector3.forward * offset;
            scanBar.transform.rotation = Quaternion.identity;
            spotLight.intensity = lightIntensity * (0.75f + 0.25f * Mathf.Sin(elapsed * 12f));
        }
        else if (scanBar.activeSelf)
        {
            // 검사 완료: 스캔 바 숨기고 초록불 유지
            scanBar.SetActive(false);
            spotLight.intensity = lightIntensity;
            Debug.Log("[InspectionLight] 검사 완료");
        }
    }

    private bool IsTargetInside()
    {
        if (inspectionPoint == null)
            return false;

        if (trackAGVs)
        {
            // 현재 대상이 아직 안에 있으면 유지, 아니면 검사 위치에 들어온 AGV 검색
            if (IsInside(target))
                return true;

            target = null;
            foreach (Pr1Controller agv in FindObjectsByType<Pr1Controller>(FindObjectsSortMode.None))
            {
                if (agv != null && IsInside(agv.transform))
                {
                    target = agv.transform;
                    return true;
                }
            }
            return false;
        }

        return IsInside(target);
    }

    private bool IsInside(Transform candidate)
    {
        if (candidate == null || !candidate.gameObject.activeInHierarchy)
            return false;

        Vector3 diff = candidate.position - inspectionPoint.position;
        diff.y = 0f;
        return diff.sqrMagnitude <= detectRadius * detectRadius;
    }

    private void BeginInspection()
    {
        inspectStartTime = Time.time;
        IsInspecting = true;
        SetVisible(true, true);
        Debug.Log("[InspectionLight] 검사라인 진입: 초록불 검사 시작");
    }

    private void EndInspection()
    {
        IsInspecting = false;
        SetVisible(false, false);
        Debug.Log("[InspectionLight] 검사라인 이탈: 초록불 OFF");
    }

    private void SetVisible(bool lightOn, bool scanOn)
    {
        if (spotLight != null) spotLight.enabled = lightOn;
        if (lamp != null) lamp.SetActive(lightOn);
        if (scanBar != null) scanBar.SetActive(scanOn);
    }

    // ==========================================
    // 조명 / 램프 / 스캔 바 생성 (씬에 별도 에셋 없이 런타임 생성)
    // ==========================================

    private void CreateVisuals()
    {
        Material glow = CreateGlowMaterial();

        GameObject lightObject = new GameObject("Green Spot Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        spotLight = lightObject.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = lightColor;
        spotLight.intensity = lightIntensity;
        spotLight.range = lightRange;
        spotLight.spotAngle = spotAngle;
        spotLight.shadows = LightShadows.None;

        lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lamp.name = "Inspection Lamp";
        Destroy(lamp.GetComponent<Collider>());
        lamp.transform.SetParent(transform, false);
        lamp.transform.localScale = new Vector3(0.25f, 0.03f, 0.25f);
        lamp.GetComponent<Renderer>().sharedMaterial = glow;

        scanBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scanBar.name = "Inspection Scan Bar";
        Destroy(scanBar.GetComponent<Collider>());
        scanBar.transform.SetParent(transform, true);
        scanBar.transform.localScale = scanBarSize;
        scanBar.GetComponent<Renderer>().sharedMaterial = glow;
    }

    private Material CreateGlowMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        Color hdr = lightColor * 2f;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", hdr);
        if (material.HasProperty("_Color")) material.SetColor("_Color", hdr);
        return material;
    }
}
