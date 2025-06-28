using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

public class MissionController : IMission
{
    #region Fields
    public event Action OnStarted;
    public event Action OnMissionPointReached;
    public event Action OnFinished;
    public event Action<MissionState> OnStateChanged;

    private Timer _timer = new Timer();
    #endregion

    #region Properties
    [field: Header("Current mission state."), SerializeField] public MissionState State { get; set; } = MissionState.Waiting;
    #endregion

    #region Methods
    public void Start (float DelayInSeconds = 0, Action OnEndDelayCallback = null)
    {
        if(DelayInSeconds > 0f)
            StartWithDelay(DelayInSeconds);
        else
            StartImmediately();
    }

    private void StartImmediately ()
    {
        if (State != MissionState.Waiting)
            return;

        ManageMissionStates(MissionState.Started);
    }

    private async void StartWithDelay (float DelayInSeconds = 0, Action OnEndDelayCallback = null)
    {
        if (DelayInSeconds <= 0)
        {
            Debug.LogError("Delay can't be less or equal zero");
            return;
        }

        if (_timer == null)
        {
            Debug.LogError("Timer is null!");
            return;
        }

        await _timer.StartAsync(DelayInSeconds, delegate { OnEndDelayCallback?.Invoke(); StartImmediately(); });
    }

    public void Finish ()
    {
        if (State != MissionState.Started)
            return;

        ManageMissionStates(MissionState.Finished);
    }

    protected void ManageMissionStates (MissionState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);

        switch (State)
        {
            case MissionState.Waiting:
                break;

            case MissionState.Started:
                OnStarted?.Invoke();
                break;

            case MissionState.Finished:
                OnMissionPointReached?.Invoke();
                OnFinished?.Invoke();
                break;
        }
    }
    #endregion
}

public class Timer
{
    public bool InProgress { get; private set; } = false;

    private CancellationTokenSource _cancellationTokenSource;

    public async UniTask StartAsync (float DelayInSeconds, Action OnCompleteCallback = null)
    {
        if (InProgress)
            Cancel();

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            InProgress = true;
            await UniTask.Delay(delayTimeSpan: TimeSpan.FromSeconds(DelayInSeconds), cancellationToken: _cancellationTokenSource.Token);

            OnCompleteCallback?.Invoke();
        }
        catch (OperationCanceledException)
        {

        }

        InProgress = false;
    }

    public void Cancel ()
    {
        if (_cancellationTokenSource != null)
            _cancellationTokenSource?.Cancel();
    }
}
