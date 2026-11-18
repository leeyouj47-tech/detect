/*기본 wayPoint.cs
 * using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WayPoint : MonoBehaviour
{
    [Header("웨이포인트 설정")]
    public int id;      //고유 아이디 1, 2, 3

    [Header("연결된 주변 웨이포인트 리스트")]
    public List<WayPoint> neighbors = new List<WayPoint>();

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.02f);

        Vector3 direction = Vector3.zero;
        for (int i = 0; i < neighbors.Count; i++)
        {
            direction = neighbors[i].transform.position - transform.position;
            direction.y = 0f;
            Vector3 linePos = transform.position + Vector3.up * 0.07f;
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.ArrowHandleCap(i, linePos, Quaternion.LookRotation(direction), 0.1f, EventType.Repaint);
        }

        //씬 뷰에서 ID가 시각적으로 표현 
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.05f, $"ID: {id}");

        if (neighbors == null)
            return;

        Gizmos.color = Color.blue;
        foreach (var neighbors in neighbors)
        {
            if (neighbors != null)
            {
                Gizmos.DrawLine(transform.position, neighbors.transform.position);
            }
        }
    }
#endif
}
*/

using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WayPoint : MonoBehaviour
{
    [Header("웨이포인트 설정")]
    public int id;      //고유 아이디 1, 2, 3

    [Header("연결된 주변 웨이포인트 리스트")]
    public List<WayPoint> neighbors = new List<WayPoint>();


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        if (neighbors == null)
            return;

        foreach (WayPoint neighbor in neighbors)
        {
            if (neighbor == null)
                continue;

            Vector3 direction =
                neighbor.transform.position - transform.position;

            direction.y = 0f;

            // 방향 벡터가 0이면 LookRotation 실행하지 않음
            if (direction.sqrMagnitude < 0.0001f)
                continue;

            Gizmos.DrawLine(transform.position, neighbor.transform.position);

            Quaternion rotation = Quaternion.LookRotation(direction);

            UnityEditor.Handles.ArrowHandleCap(
                0,
                transform.position + direction.normalized * 0.5f,
                rotation,
                0.5f,
                EventType.Repaint
            );
        }
    }
#endif

}