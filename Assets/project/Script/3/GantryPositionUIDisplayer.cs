using UnityEngine;
using UnityEngine.UI;

public class GantryPositionUIDisplayer : MonoBehaviour
{
    public GantryPositioningManager manager;
    public ServoAmp axis1, axis2, axis3, axis4;
    public Text idText, axis1Text, axis2Text, axis3Text, axis4Text;

    private int positionAxis1, positionAxis2, positionAxis3, positionAxis4;


    //처음 생성되서 활성화 될 때(초기화)
    public void Initialize(int id, int a1, int a2, int a3, int a4)
    {
        positionAxis1 = a1;
        positionAxis2 = a2;
        positionAxis3 = a3;
        positionAxis4 = a4;
        //표현식 결과는 동일하지만 다양하게 표현식을 사용할 수 있다.
        //idText.text = "P" + id.ToString("D3");
        idText.text = $"P{id:D3}";
        axis1Text.text = positionAxis1.ToString();
        axis2Text.text = positionAxis2.ToString();
        axis3Text.text = positionAxis3.ToString();
        axis4Text.text = positionAxis4.ToString();

        gameObject.SetActive(true);
    }

    //순서가 변경될 때 처리하는 함수
    public void ChangeIndex(int index)
    {
        idText.text = $"P{index:D3}";
    }

    //삭제될 때 호출되는 함수
    public void Delete(bool removeData)
    {
        if (removeData)
            manager.RemoveData(this);

        Destroy(gameObject);
    }


    //서보앰프에 이동 명령내리는 함수
    public void Go()
    {
        axis1.Positioning(positionAxis1);
        axis2.Positioning(positionAxis2);
        axis3.Positioning(positionAxis3);
        axis4.Positioning(positionAxis4);
    }

}
