using UnityEngine;

public class testprogram : OfflineProgram
{
    public override void Home()
    {
        this.MoveJ(waypoints[0]);
    }
}
