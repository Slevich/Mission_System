using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IMission
{
    public event Action OnStarted;
    public event Action OnMissionPointReached;
    public event Action OnFinished;
    public event Action<MissionState> OnStateChanged;

    public MissionState State { get; set; }

    public void Start (float DelayInSeconds = 0, Action OnEndDelay = null);
    public void Finish ();
}

public enum MissionState
{
    Waiting,
    Started,
    Finished,
}