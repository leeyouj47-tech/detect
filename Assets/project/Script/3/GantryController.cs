using System;
using System.Collections.Generic;
using UnityEngine;

public class GantryController : MonoBehaviour
{
    // 축 1개 전용. GantryConnector의 포지셔닝 데이터와 ServoAmp를 사용해 스텝을 실행한다.
    // 지원: ABS_Linear / INC_Linear, End / Location (Continue는 End로 처리)
    // 무시: Forward_Speed / Reverse_Speed


    private ServoAmp amp;
    private AxisData axisData;
    private Action onFinished;          // 시퀀스 종료 시 호출 (위치결정완료 피드백)
    private float originalMaxSpeed;     // ServoAmp 원래 maxSpeed (속도 상한)

    private List<PositioningData> steps;
    private int index;
    private bool running;
    private bool dwelling;
    private float dwellEndTime;

    // GantryConnector가 호출. 사용할 ServoAmp, 해당 축의 스텝 데이터, 종료 콜백을 전달받는다.
    public void Initialize(ServoAmp amp, AxisData axisData, Action onFinished)
    {
        this.amp = amp;
        this.axisData = axisData;
        this.onFinished = onFinished;
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
        if (amp == null || axisData == null)
        {
            Debug.LogError($"{name} 초기화되지 않았거나 포지셔닝 데이터가 없습니다.");
            return;
        }

        var list = axisData.stepDataList;
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
        onFinished?.Invoke();
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