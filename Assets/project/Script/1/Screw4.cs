using UnityEngine;

public class Screw4 : OfflineProgram
{
    public override void Home()
    {
        this.AccTime(0.2f, 0.2f);
        this.Speed(1000f, 60f, 90f);

        this.MoveJ(waypoints[0]);
    }

    public void Program4()
    {
        this.MoveJ(waypoints[1]);

        this.Action(true, 200);

        this.Action(false);

        this.MoveJ(waypoints[0]);
    }
}