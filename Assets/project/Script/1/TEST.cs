using UnityEngine;

public class TEST : OfflineProgram
{
    public override void Home()
    {
        this.AccTime(0.2f, 0.2f);
        this.Speed(1000f, 60f, 90f);

        // 홈 복귀 시 처음 지점인 Target_00으로 이동
        this.MoveJ(waypoints[0]);
    }

    public void Program1()
    {
        // Target_01부터 Target_05까지 순서대로 이동
        this.MoveL(waypoints[1]); // Target_02
        this.Action(true,100);
        this.MoveL(waypoints[2]); 
        this.MoveL(waypoints[3]); // Target_01
        this.MoveL(waypoints[4]); // Target_02
        this.Action(false);
        // 작업 완료 후 Home() 호출 (다시 Target_00으로 복귀)
        this.MoveL(waypoints[0]);
    }
}