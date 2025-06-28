using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PopupAnimation : MonoBehaviour
{
    #region Fields
    [Header("Tranform to animate."), SerializeField] private Transform _transformToAnimate;
    [Space(10), Header("Duration of showing in seconds."), SerializeField] private float _showDuration = 1f;
    [Space(5), Header("Animation target scale."), SerializeField] private Vector3 _targetScale = Vector3.one;
    [Space(5), Header("Ease of showing animation."), SerializeField] private Ease _showEase = Ease.Linear;
    [Space(10), Header("Duration of hiding in seconds."), SerializeField] private float _hideDuration = 1f;
    [Space(5), Header("Ease of hiding animation."), SerializeField] private Ease _hideEase = Ease.Linear;

    private CancellationTokenSource _cancellationTokenSource;
    private Tween _currentTween;
    private Vector3 _startScale = Vector3.zero;
    #endregion

    #region Methods
    private void Start ()
    {
        if(_transformToAnimate != null)
            _transformToAnimate.localScale = _startScale;
    }

    public void PlayAnimationWithDelayBetweenShowAndHide(float Delay)
    {
        PlayAnimation(Delay);
    }

    private async void PlayAnimation(float delayBetweenTweens = 0)
    {
        if (_transformToAnimate == null)
            return;

        KillTween();

        if(_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        _transformToAnimate.localScale = _startScale;
        Tween scaleToTargetTween = _transformToAnimate.DOScale(_targetScale, _showDuration);
        scaleToTargetTween.SetEase(_showEase);
        scaleToTargetTween.onComplete = delegate { _currentTween = null;};
        _currentTween = scaleToTargetTween;
        scaleToTargetTween.Play();

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await UniTask.Delay(delayTimeSpan: TimeSpan.FromSeconds(_showDuration + delayBetweenTweens), cancellationToken: _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException exception)
        {
            _currentTween = null;
            return;
        }

        if(!_cancellationTokenSource.IsCancellationRequested)
        {
            _transformToAnimate.localScale = _targetScale;
            Tween scaleToStartTween = _transformToAnimate.DOScale(_startScale, _hideDuration);
            scaleToStartTween.SetEase(_showEase);
            scaleToStartTween.onComplete = delegate { _currentTween = null; };
            _currentTween = scaleToTargetTween;
            scaleToStartTween.Play();
        }
    }

    private void KillTween ()
    {
        if (_currentTween != null && _currentTween.IsPlaying())
        {
            _currentTween.Kill();
            _currentTween = null;
        }
    }

    private void OnDisable () => KillTween ();
    #endregion
}
