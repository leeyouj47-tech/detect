//26.11.16 LYJ: A1Program.cs 생성

using Preliy.Flange;
using UnityEngine;

public class A1Program : OfflineProgram
{
    private const float TCPFast = 500f;
    private const float TCPNormal = 300f;
    private const float TCPSlow = 100f;

    private const float TCPAngleFast = 90f;
    private const float TCPAngleNormal = 60f;
    private const float TCPAngleSlow = 30f;

    private const float JogFast = 120f;
    private const float JogNormal = 90f;
    private const float JogSlow = 60f;

    public override void Home()
    {
        this.AccTime(0.2f, 0.2f);
        this.Speed(300f, 30f, 90f);

        this.MoveJ(waypoints[3]);

        if (scheduler.currentAttachedTool != null)
        {
            this.MoveJ(waypoints[7]);
            this.MoveJ(waypoints[8]);
            this.MoveJ(waypoints[9]);
        }
        else
        {
            this.MoveJ(waypoints[0]);
            this.Connect(true);
            this.MoveJ(waypoints[6]);
            this.MoveJ(waypoints[8]);
            this.MoveJ(waypoints[9]);
        }
    }

    public void Program1()
    {
        // =================================
        // P2 작업
        // =================================

        this.MoveL(waypoints[10]);
        this.MoveL(waypoints[11]);
        this.MoveL(waypoints[12]);
        this.MoveL(waypoints[13]);
        this.MoveL(waypoints[14]);
        this.MoveL(waypoints[2]);
        this.Wait(100);
        // 제품 잡기
        this.Action(true, 100);

        // =================================
        // P3 작업
        // =================================

        this.Speed(100f, 60f, 30f);

        this.MoveL(waypoints[14]);
        this.MoveJ(waypoints[15]);
        this.MoveJ(waypoints[16]);
        this.MoveJ(waypoints[17]);
        this.MoveL(waypoints[18]);
        this.MoveJ(waypoints[19]);
        this.MoveL(waypoints[23]);
        this.MoveL(waypoints[21]);
        this.MoveL(waypoints[4]);
        this.MoveL(waypoints[5]);

        // 제품 놓기
        this.Action(false);

        this.MoveJ(waypoints[22]);

        // Home 복귀
        Home();
    }
}
