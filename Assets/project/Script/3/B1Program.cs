using UnityEngine;

public class B1Program : OfflineProgram
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
        this.MoveJ(waypoints[0]);
    }
    public void Program1()
    {
        this.MoveJ(waypoints[9]);
        this.MoveL(waypoints[10]);
        this.MoveL(waypoints[1]);
        this.MoveL(waypoints[2]);
        this.Action(true);
        this.Wait(1500);
        this.Action(false);
        this.MoveL(waypoints[1]);
        this.MoveL(waypoints[3]);
        this.MoveL(waypoints[4]);
        this.Action(true);
        this.Wait(1500);
        this.Action(false);
        this.MoveL(waypoints[3]);
        this.MoveL(waypoints[11]);
        this.MoveL(waypoints[12]);
        this.MoveL(waypoints[9]);
    }

    public void Program2()
    {
        this.MoveJ(waypoints[9]);
        this.MoveL(waypoints[13]);
        this.MoveL(waypoints[5]);
        this.MoveL(waypoints[6]);
        this.Action(true);
        this.Wait(1500);
        this.Action(false);
        this.MoveL(waypoints[5]);
        this.MoveL(waypoints[13]);
        this.MoveL(waypoints[9]);
        this.Wait(1500);
        this.MoveL(waypoints[14]);
        this.MoveL(waypoints[7]);
        this.MoveL(waypoints[8]);
        this.Action(true);
        this.Wait(1500);
        this.Action(false);
        this.MoveL(waypoints[7]);
        this.MoveL(waypoints[14]);
        this.MoveJ(waypoints[9]);
    }

}
