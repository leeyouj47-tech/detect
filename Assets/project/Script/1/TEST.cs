using UnityEngine;

public class TEST : OfflineProgram
{
    public override void Home()
    {
        this.AccTime(0.2f, 0.2f);
        this.Speed(300f, 30f, 90f);

        // 홈 복귀 시 처음 지점인 Target_00으로 이동
        this.MoveJ(waypoints[0]);
    }

    public void Program1()
    {
        // Target_00부터 Target_05까지 순서대로 이동
        this.MoveJ(waypoints[0]); // Target_00 (시작)
        this.MoveJ(waypoints[1]); // Target_01
        this.MoveJ(waypoints[2]); // Target_02
        this.MoveJ(waypoints[3]);
        this.MoveJ(waypoints[4]);
        this.MoveJ(waypoints[5]);
        this.MoveJ(waypoints[6]);
        this.MoveJ(waypoints[7]);
        // 작업 완료 후 Home() 호출 (다시 Target_00으로 복귀)
        this.MoveJ(waypoints[0]);
    }
}