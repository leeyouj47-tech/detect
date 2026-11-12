using UnityEngine;

public class RobotArmprogram : OfflineProgram
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
        this.AccTime(0.2f,0.2f);
        this.Speed(TCPNormal, TCPAngleSlow, JogNormal);
        this.MoveJ(waypoints[24]);
        this.MoveJ(waypoints[0]);
        this.Connect(true);
        this.MoveJ(waypoints[3]);
        this.MoveC(waypoints[4], waypoints[5],TCPFast,TCPAngleFast);
    }
        

    public void Program1()
    {
        //P2
        
        this.MoveJ(waypoints[6]);
        this.MoveJ(waypoints[7],90f,Pattern.Continue);
        this.MoveJ(waypoints[8],90f, Pattern.Continue);
        
        this.MoveJ(waypoints[9]);
        //사이에 웨이포인트 추가, movel로 천천히 접근
        this.MoveJ(waypoints[2]);
        //P3
        this.Action(true, 100);
        this.MoveJ(waypoints[9]);
        this.MoveJ(waypoints[10]);
        this.MoveJ(waypoints[11],90f,Pattern.End,0,3000);
        this.MoveJ(waypoints[23],90f,Pattern.Continue);
        this.MoveJ(waypoints[12],90f, Pattern.Continue);
        this.MoveJ(waypoints[13],90f, Pattern.Continue);
        this.MoveJ(waypoints[14]);
        this.MoveJ(waypoints[15],30f,Pattern.End,0,5000);
        this.MoveJ(waypoints[16],30f);
        this.MoveJ(waypoints[17],30f, Pattern.End,0,3000);
        this.Action(false, 100);
        //P4
        this.MoveJ(waypoints[18]);
        this.MoveJ(waypoints[19]);
        this.MoveJ(waypoints[20]);
        this.MoveJ(waypoints[26], 90f, Pattern.Continue);
        this.MoveJ(waypoints[25]);
        this.MoveJ(waypoints[21]);
        this.MoveJ(waypoints[22]);
        this.MoveJ(waypoints[5]);

    }
}
