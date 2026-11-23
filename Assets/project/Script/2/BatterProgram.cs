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

        this.MoveL(waypoints[1]);

        this.Action(true, 100);

        this.MoveL(waypoints[2]);
        this.MoveL(waypoints[3]);
        this.MoveL(waypoints[4]);
        this.MoveL(waypoints[5]);

        this.Action(false);

        this.MoveL(waypoints[0]);

        Debug.Log("[TEST] Program1 명령 실행 종료!");
    }
}
