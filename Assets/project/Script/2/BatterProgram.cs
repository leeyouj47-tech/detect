//26.11.20 LYJ: TEST.cs 수정중
using UnityEngine;

public class BatterProgram : OfflineProgram
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

        SyncWaypointsFromScene();

        // 속도/가속 설정 (Home을 거치지 않고 PLC M833으로 바로 실행돼도 동작하도록)
        this.AccTime(0.2f, 0.2f);
        this.Speed(1000f, 60f, 90f);

        this.MoveL(waypoints[1]);
        this.MoveL(waypoints[2]);

        this.Action(true, 100);

        this.MoveL(waypoints[3]);
        this.MoveL(waypoints[4]);
        this.MoveL(waypoints[5]);

        this.Action(false);

        this.MoveL(waypoints[0]);

        Debug.Log("[TEST] Program1 명령 실행 종료!");
    }

    // MoveL은 Target의 저장값(position/rotation)으로, MoveJ는 씬 위치로 이동하므로
    // 씬에서 옮긴 Target 위치를 저장값에 맞춰 둘이 같은 곳으로 가게 함
    private void SyncWaypointsFromScene()
    {
        foreach (TargetWaypoint waypoint in waypoints)
        {
            if (waypoint == null)
                continue;

            waypoint.position = waypoint.transform.position;
            waypoint.rotation = waypoint.transform.rotation;
        }
    }
}
