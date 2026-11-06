
using UnityEngine;

public class AGVFiller : MonoBehaviour
{
    [Header("AGV Prefab")]
    public GameObject[] prefabs;
    public string prefabName;

    [Header("Spawn Settings")]
    public float startTime = 1f;
    public float delay = 3f;

    private float fillTime;
    private bool isFilled;

    private bool plcConnected;
    private bool spawnRequested;

    void Start()
    {
        fillTime = Time.time + startTime;
    }

    void Update()
    {
        // PLC 연결 및 생성 요청 확인
        if (!spawnRequested)
            return;

        if (isFilled || Time.time < fillTime)
            return;

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[Filler] AGV Prefab이 설정되지 않았습니다.");
            spawnRequested = false;
            return;
        }

        GameObject selectedPrefab =
            prefabs[Random.Range(0, prefabs.Length)];

        GameObject go = Instantiate(
            selectedPrefab,
            transform.position,
            transform.rotation
        );

        go.name = prefabName;

        isFilled = true;
        spawnRequested = false;

        Debug.Log("[Filler] AGV 생성 완료");
    }

    public void SetPLCConnection(bool connected)
    {
        plcConnected = connected;

        if (!connected)
            spawnRequested = false;

        Debug.Log($"[Filler] PLC 연결 상태: {connected}");
    }

    public void RequestSpawn()
    {
        if (spawnRequested || isFilled)
        {
            Debug.LogWarning("[Filler] 이미 생성 중이거나 AGV가 존재합니다.");
            return;
        }

        spawnRequested = true;

        Debug.Log("[Filler] AGV 생성 요청 수신");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.isKinematic)
            return;

        if (!other.gameObject.name.Contains(prefabName))
            return;

        isFilled = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.name.Contains(prefabName))
            return;

        isFilled = false;
        fillTime = Time.time + delay;
    }
}
