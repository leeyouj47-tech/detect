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

        if(scheduler.currentAttachedTool != null)
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
        //P2
        this.MoveJ(waypoints[10]);
        this.MoveJ(waypoints[11]);
        this.MoveJ(waypoints[12]);
        this.MoveJ(waypoints[13]);
        this.MoveJ(waypoints[14]);
        this.MoveJ(waypoints[2]);
        //this.Action(true);
        //P3
        Home();
    }

}
