using UnityEngine;

public class B2Program : OfflineProgram
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
        this.MoveJ(waypoints[0]);
        this.Connect(true);
        this.MoveJ(waypoints[6]);
    }
}