using UnityEngine;

public class A2Program : OfflineProgram
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
        //P2
        this.MoveL(waypoints[10]);
        this.MoveL(waypoints[11]);
        this.MoveL(waypoints[12]);
        this.MoveL(waypoints[13]);
        this.MoveL(waypoints[14]);
        this.MoveL(waypoints[2]);
        this.Wait(100);
        this.Action(true,500);
        this.MoveL(waypoints[14]);
        //P3
        //this.Speed(100f, 60f, 30f);
        this.MoveL(waypoints[15]);
        this.MoveL(waypoints[16]);
        this.MoveL(waypoints[17]);
        this.MoveL(waypoints[18]);
        this.MoveL(waypoints[19]);
        //this.MoveJ(waypoints[20]);
        this.MoveL(waypoints[21]);
        this.MoveL(waypoints[4]);       
    }

    public void Program2()
    {
        this.MoveL(waypoints[5]);
        this.Action(false);
        this.Wait(100);
        this.MoveL(waypoints[22]);
        Home();
    }
}
