using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NotificationController : MonoBehaviour
{
    #region Fields
    [Header("Popup animation of the notification."), SerializeField] private PopupAnimation _popup;
    [Header("Mission name text."), SerializeField] private TextMeshProUGUI _missionNameText;
    [Header("Mission description text."), SerializeField] private TextMeshProUGUI _missionDescriptionText;
    [Header("Mission state text."), SerializeField] private TextMeshProUGUI _missionStateText;
    #endregion

    #region Methods
    private void OnEnable ()
    {
        MissionsSequence[] sequences = MissionsManager.Instance.DisplayedSequences;

        foreach (MissionsSequence sequence in sequences)
        {
            sequence.OnCurrentMissionStateChanged += (mission) => UpdateTexts(mission);
        }
    }

    private void UpdateTexts(MissionData mission)
    {
        if (_popup != null)
            _popup.PlayAnimationWithDelayBetweenShowAndHide(2f);

        _missionNameText.text = "Миссия: " + mission.Name;
        _missionDescriptionText.text = mission.Description;
        _missionStateText.text = TranslateState(mission.Controller.State);
    }

    private string TranslateState(MissionState state)
    {
        switch(state)
        {
            case MissionState.Started:
                return "Миссия началась!";

            case MissionState.Finished:
                return "Миссия завершена!";
        }

        return state.ToString();
    }
    
    private void OnDisable ()
    {
        
    }
    #endregion
}
