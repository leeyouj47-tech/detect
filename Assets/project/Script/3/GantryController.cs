using System;
using System.Collections.Generic;
using UnityEngine;

public class GantryController : MonoBehaviour
{    
    // 축 1개 전용. GantryConnector의 포지셔닝 데이터와 ServoAmp를 사용해 스텝을 실행한다.
    // 지원: ABS_Linear / INC_Linear, End / Location (Continue는 End로 처리)
    // 무시: Forward_Speed / Reverse_Speed


    public GantryConnector connector;
    public int axisIndex = 1;   // 1~4 (1축~4축)

    private ServoAmp amp;
    private float originalMaxSpeed;     // ServoAmp 원래 maxSpeed (속도 상한)

    private List<PositioningData> steps;
    private int index;
    private bool running;
    private bool dwelling;
    private float dwellEndTime;

    private void Start()
    {
        amp = axisIndex switch
        {
            1 => connector.axis1,
            2 => connector.axis2,
            3 => connector.axis3,
            _ => connector.axis4
        };
        originalMaxSpeed = amp.maxSpeed;

        amp.onCompletedPositioning.AddListener(OnArrived);
        amp.onChangedError.AddListener(v => { if (v) Halt(); });
        amp.onChangedStop.AddListener(v => { if (v) Halt(); });
    }

    private void Update()
    {
        // Location: Dwell 정지가 끝나면 다음 스텝으로
        if (!running || !dwelling || Time.time < dwellEndTime)
            return;

        dwelling = false;
        index++;

        if (index >= steps.Count)
            Finish();
        else
            RunStep();
    }

    // stepNumber: 위치결정번호(1부터)
    public void StartPositioning(int stepNumber)
    {
        if (connector.data == null || axisIndex - 1 >= connector.data.axes.Count)
        {
            Debug.LogError($"{name} 포지셔닝 데이터가 없습니다.");
            return;
        }

        var list = connector.data.axes[axisIndex - 1].stepDataList;
        if (stepNumber < 1 || stepNumber > list.Count)
        {
            //Debug.LogError($"{name} 잘못된 위치결정번호: {stepNumber}");
            return;
        }

        steps = list;
        index = stepNumber - 1;
        dwelling = false;
        running = true;
        RunStep();
    }

    public void Halt()
    {
        running = false;
        dwelling = false;
        amp.maxSpeed = originalMaxSpeed;
    }

    private void RunStep()
    {
        var data = steps[index];

        if (data.method == ControlMethod.Forward_Speed || data.method == ControlMethod.Reverse_Speed)
        {
            Halt();     // 속도 제어는 무시
            return;
        }

        if (!amp.IsReady || amp.IsStopped || amp.IsError || amp.IsBusy)
        {
            //Debug.LogWarning($"{name} ServoAmp가 이동할 수 없는 상태입니다.");
            Halt();
            return;
        }

        // commandSpeed로 이동. 원래 maxSpeed보다 크면 maxSpeed로 제한
        amp.maxSpeed = Mathf.Min(data.commandSpeed, originalMaxSpeed);

        int pulse = UnitToPulse(data.positioningAddress);
        if (data.method == ControlMethod.INC_Linear)
            pulse += amp.GetCurrentPulse;

        amp.Positioning(pulse);
    }

    // ServoAmp.onCompletedPositioning
    private void OnArrived()
    {
        if (!running || dwelling)
            return;

        if (steps[index].pattern == OperationPattern.Location)
        {
            dwelling = true;
            dwellEndTime = Time.time + steps[index].dwellTime;
        }
        else
        {
            Finish();   // End, Continue(End로 처리)
        }
    }

    // 시퀀스 종료 -> 위치결정완료 피드백
    private void Finish()
    {
        Halt();

        switch (axisIndex)
        {
            case 1: connector.OnCompletedAxis1Positioning(); break;
            case 2: connector.OnCompletedAxis2Positioning(); break;
            case 3: connector.OnCompletedAxis3Positioning(); break;
            default: connector.OnCompletedAxis4Positioning(); break;
        }
    }

    // mm/degree -> pulse (ServoAmp에 공개 변환 함수가 없어서 같은 공식으로 직접 계산)
    private int UnitToPulse(float unit)
    {
        if (amp.controlUnit == ServoAmp.ControlUnit.pulse)
            return Mathf.RoundToInt(unit);

        float shaftRevs = (amp.controlUnit == ServoAmp.ControlUnit.mm)
            ? unit / amp.ballscrewLead
            : unit / 360f;

        return Mathf.RoundToInt(shaftRevs / amp.gearRatio * amp.motorResolution);
    }
}
