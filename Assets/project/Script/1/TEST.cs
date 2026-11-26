//26.11.20 LYJ: TEST.cs 수정중
using UnityEngine;

public class TEST : OfflineProgram
{
    [Header("Robot Arm Connector")]
    public RobotArmConnector connector;

    public override void Home()
    {
        this.AccTime(0.2f, 0.2f);
        this.Speed(1000f, 60f, 90f);

        this.MoveJ(waypoints[0]);
    }

    public void Program1()
    {
        Debug.Log("[TEST] Program1 시작!");

        // 속도/가속 설정 (Home을 거치지 않고 PLC로 바로 실행돼도 동작하도록)
        this.AccTime(0.2f, 0.2f);
        this.Speed(1000f, 60f, 90f);

        MoveLSafe(1);

        this.Action(true, 100);

        MoveLSafe(2);
        MoveLSafe(6);
        MoveLSafe(3);
        MoveLSafe(4);
        MoveLSafe(5);

        this.Action(false);

        MoveLSafe(0);

        Debug.Log("[TEST] Program1 명령 실행 종료!");
    }

    // 씬에 등록되지 않은 Waypoint 번호는 건너뜀 (IndexOutOfRangeException 방지)
    // Target_05, Target_06을 만들어 Waypoints 목록에 추가하면 자동으로 이동에 포함됨
    private void MoveLSafe(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Length || waypoints[index] == null)
        {
            Debug.LogWarning($"[TEST] waypoints[{index}]가 없어 건너뜁니다. (등록된 개수: {(waypoints == null ? 0 : waypoints.Length)})");
            return;
        }

        this.MoveL(waypoints[index]);
    }
}
