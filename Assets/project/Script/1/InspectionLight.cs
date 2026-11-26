using UnityEngine;

// ==========================================
// 검사라인 초록불 검사 연출
// [자동 모드] 검사 대상(target)이 검사 위치(inspectionPoint)에 들어오면 초록 조명 ON
//   - scanDuration 동안 초록 스캔 바가 대상 위를 왕복
//   - 검사 시간이 끝나면 초록불 유지, 대상이 검사 위치를 벗어나면 OFF
// [수동 모드] manualOnly = true 이면 위치 감지 없이 BeginManual / ShowResult 호출로 동작
//   - 2번 씬: Pr2Controller가 M834/M835 검사 시작 시 BeginManual, 끝나면 ShowResult
//   - 결과 정상 = 초록, 불량 = 빨강으로 resultHoldTime 동안 표시 후 OFF
// ==========================================
public class InspectionLight : MonoBehaviour
{
    [Header("검사 대상 / 검사 위치")]
    public Transform target;
    public Transform inspectionPoint;

    [Tooltip("true: target 대신 검사 위치에 들어온 AGV(Pr1Controller)를 자동으로 찾음 (Filler로 생성된 AGV 포함)")]
    public bool trackAGVs = false;

    [Tooltip("true: 위치 감지 없이 BeginManual / ShowResult 호출로만 동작")]
    public bool manualOnly = false;

    [Tooltip("검사 위치와의 수평 거리가 이 값 이하이면 검사 위치에 들어온 것으로 판단")]
    public float detectRadius = 0.3f;

    [Header("조명")]
    public Color lightColor = new Color(0.1f, 1f, 0.25f);
    public Color defectColor = new Color(1f, 0.12f, 0.08f);
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

    [Header("수동 모드 결과 표시")]
    public float resultHoldTime = 2f;

    public bool IsInspecting { get; private set; }

    private Light spotLight;
    private GameObject lamp;
    private GameObject scanBar;
    private Material glow;
    private bool targetInside;
    private float inspectStartTime;
    private float manualOffTime = -1f;

    private void Awake()
    {
        CreateVisuals();
        SetVisible(false, false);
    }

    private void Update()
    {
        if (!manualOnly)
        {
            bool inside = IsTargetInside();

            if (inside && !targetInside)
                BeginInspection();
            else if (!inside && targetInside)
                EndInspection();

            targetInside = inside;
        }
        else if (targetInside && manualOffTime > 0f && Time.time >= manualOffTime)
        {
            EndInspection();
        }

        if (!targetInside || target == null)
            return;

        // 대상 위에 조명 배치
        Vector3 basePos = target.position;
        transform.position = basePos + Vector3.up * lightHeight;

        if (!IsInspecting)
            return;

        float elapsed = Time.time - inspectStartTime;

        if (elapsed < scanDuration)
        {
            // 스캔 바 왕복 + 조명 깜빡임
            float offset = Mathf.Sin(elapsed * scanSpeed * Mathf.PI) * scanHalfLength;
            scanBar.transform.position = basePos + Vector3.up * scanBarHeight + Vector3.forward * offset;
            scanBar.transform.rotation = Quaternion.identity;
            spotLight.intensity = lightIntensity * (0.75f + 0.25f * Mathf.Sin(elapsed * 12f));
        }
        else if (!manualOnly)
        {
            // 자동 모드 검사 완료: 스캔 바 숨기고 초록불 유지
            FinishScan();
            Debug.Log("[InspectionLight] 검사 완료");
        }
    }

    // ==========================================
    // 수동 모드 (2번 씬 Pr2Controller에서 호출)
    // ==========================================

    /// <summary>검사 시작: duration 동안 스캔 (0 이하면 scanDuration 사용)</summary>
    public void BeginManual(Transform inspectTarget, float duration)
    {
        if (inspectTarget != null)
            target = inspectTarget;

        if (target == null)
        {
            Debug.LogWarning($"[InspectionLight] {name}: 검사 대상이 없습니다.");
            return;
        }

        if (duration > 0f)
            scanDuration = duration;

        manualOffTime = -1f;
        targetInside = true;
        SetColor(lightColor);
        BeginInspection();
    }

    /// <summary>검사 결과 표시: 정상 초록 / 불량 빨강, resultHoldTime 후 OFF</summary>
    public void ShowResult(bool good)
    {
        if (!targetInside)
            return;

        FinishScan();
        SetColor(good ? lightColor : defectColor);
        manualOffTime = Time.time + Mathf.Max(0.1f, resultHoldTime);
        Debug.Log($"[InspectionLight] {name}: 검사 결과 {(good ? "정상(초록)" : "불량(빨강)")}");
    }

    /// <summary>즉시 OFF (공정 정지 등)</summary>
    public void StopManual()
    {
        if (targetInside)
            EndInspection();
    }

    // ==========================================
    // 내부 처리
    // ==========================================

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
        Debug.Log($"[InspectionLight] {name}: 초록불 검사 시작");
    }

    private void FinishScan()
    {
        IsInspecting = false;
        if (scanBar != null) scanBar.SetActive(false);
        if (spotLight != null) spotLight.intensity = lightIntensity;
    }

    private void EndInspection()
    {
        IsInspecting = false;
        targetInside = false;
        manualOffTime = -1f;
        SetVisible(false, false);
        SetColor(lightColor);
        Debug.Log($"[InspectionLight] {name}: 초록불 OFF");
    }

    private void SetVisible(bool lightOn, bool scanOn)
    {
        if (spotLight != null) spotLight.enabled = lightOn;
        if (lamp != null) lamp.SetActive(lightOn);
        if (scanBar != null) scanBar.SetActive(scanOn);
    }

    private void SetColor(Color color)
    {
        if (spotLight != null) spotLight.color = color;

        if (glow != null)
        {
            Color hdr = color * 2f;
            if (glow.HasProperty("_BaseColor")) glow.SetColor("_BaseColor", hdr);
            if (glow.HasProperty("_Color")) glow.SetColor("_Color", hdr);
        }
    }

    // ==========================================
    // 조명 / 램프 / 스캔 바 생성 (씬에 별도 에셋 없이 런타임 생성)
    // ==========================================

    private void CreateVisuals()
    {
        glow = CreateGlowMaterial();

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
