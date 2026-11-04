using UnityEngine;

public class Screwdriver : ToolParent
{
    public GameObject effect;
    protected override void OnChangedValue(bool isOn)
    {
        if (effect == null)
            return;

        effect.SetActive(isOn);
    }
}
