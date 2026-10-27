using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

public class GantryPositioningManager : MonoBehaviour
{
    [System.Serializable]
    public class Position
    {
        public int axis1, axis2, axis3, axis4;

        private GantryPositionUIDisplayer uiData;

        public GantryPositionUIDisplayer GetUIData => uiData;

        //생성자
        public Position(int axis1, int axis2, int axis3, int axis4)
        {
            this.axis1 = axis1;
            this.axis2 = axis2;
            this.axis3 = axis3;
            this.axis4 = axis4;
        }

        //데이터와 연계된 UI를 연결하는 함수
        public void ConnectUI(GantryPositionUIDisplayer uiData)
        {
            this.uiData = uiData;
        }
    }


    //ui displayer 원본
    public GantryPositionUIDisplayer origin;

    public List<Position> positionList = new();
    public ServoAmp axis1;
    public ServoAmp axis2;
    public ServoAmp axis3;
    public ServoAmp axis4;

    //저장/불러오기에 사용할 데이터 이름 (확장자 제외)
    public string dataName = "PositionData";

    //저장 폴더 (Assets 폴더 기준 하위 경로, 예: Data/Positions). 비워두면 Assets 폴더 바로 아래.
    public string saveFolder = "";

    //UI 버튼을 눌렀을 때 데이터 추가.
    public void AddData()
    {
        AddData(axis1.GetCurrentPulse, axis2.GetCurrentPulse, axis3.GetCurrentPulse, axis4.GetCurrentPulse, true);
    }

    public void AddData(int a1, int a2, int a3, int a4, bool needSave = false)
    {
        Position pos = new Position(a1, a2, a3, a4);
        positionList.Add(pos);
        GantryPositionUIDisplayer uiData = Instantiate(origin, transform);
        uiData.Initialize(positionList.Count, a1, a2, a3, a4);
        pos.ConnectUI(uiData);

        if (needSave)
            SaveData();

    }

    public void RemoveData(GantryPositionUIDisplayer uiData)
    {
        //삭제하고 싶은 UI와 연결된 위치결정 데이터를 찾는다.
        Position pos = positionList.Find(x => x.GetUIData == uiData);
        //데이터를 찾았다면 리스트에서 지운다.
        if (pos != null)
        {
            positionList.Remove(pos);
        }

        //바뀐 리스트 순서에 맞게 UI 아이디를 수정한다.
        for (int i = 0; i < positionList.Count; i++)
        {
            positionList[i].GetUIData.ChangeIndex(i + 1);
        }

        SaveData();
    }

    //dataName 필드에 지정된 이름으로 저장 (UI 버튼 연결용)
    public void SaveData()
    {
        SaveData(dataName);
    }

    //지정한 이름으로 저장 (InputField 등에서 이름을 넘겨받을 때 사용)
    public void SaveData(string name)
    {
        string path = Path.Combine(Application.dataPath, saveFolder, name + ".csv");

        string[] csvDatas = new string[positionList.Count + 1];
        csvDatas[0] = "Position ID, Axis 1, Axis 2, Axis 3, Axis 4";
        for (int i = 0; i < positionList.Count; ++i)
        {
            csvDatas[i + 1] = i.ToString() + ',' +
                positionList[i].axis1.ToString() + ',' +
                positionList[i].axis2.ToString() + ',' +
                positionList[i].axis3.ToString() + ',' +
                positionList[i].axis4.ToString();
        }

        //폴더가 없으면 만든다.
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        //해당 파일 경로로 저장하기
        File.WriteAllLines(path, csvDatas);
    }

    //dataName 필드에 지정된 이름으로 불러오기 (UI 버튼 연결용)
    public void LoadData()
    {
        LoadData(dataName);
    }

    //지정한 이름으로 불러오기 (InputField 등에서 이름을 넘겨받을 때 사용)
    public void LoadData(string name)
    {
        string path = Path.Combine(Application.dataPath, saveFolder, name + ".csv");
        if (!File.Exists(path))
        {
            Debug.LogError("파일이 존재하지 않아 불러올 수 없습니다.");
            return;
        }

        string[] csvDatas = File.ReadAllLines(path);
        if (csvDatas.Length > 1)
        {
            //기존 데이터에 연결된 UI데이터들을 삭제함.
            foreach (var position in positionList)
            {
                position.GetUIData.Delete(false);
            }

            //기존 데이터 완전 삭제.
            positionList.Clear();

            //파일 기준으로 데이터를 재구성
            for (int i = 1; i < csvDatas.Length; ++i)
            {
                //','를 기준으로 문자열을 잘라내서 분류해야 함.
                string[] datas = csvDatas[i].Split(',');
                AddData(int.Parse(datas[1]), int.Parse(datas[2]), int.Parse(datas[3]), int.Parse(datas[4]));
            }
        }
        else
        {
            Debug.LogWarning("파일 안에 데이터가 들어있지 않아 불러오기를 취소합니다.");
        }
    }

    private void Start()
    {
        LoadData();
    }
}