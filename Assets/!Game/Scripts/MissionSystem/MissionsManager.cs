using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MissionsManager : MonoBehaviour
{
    #region Fields
    [Header("Displayed sequences."), SerializeField, HideInInspector] private MissionsSequence[] _sequences;
    [SerializeField, HideInInspector] private int _lastRemovedSequenceIndex = -1;
    [SerializeField, HideInInspector] private bool _sequencesExpanded = false;
    private static MissionsManager _instance;
    #endregion


    #region Properties
    public static MissionsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MissionsManager>();
            }

            return _instance;
        }
    }

    public int SequencesCount => _sequences.Length;
    public MissionsSequence[] DisplayedSequences => _sequences;
    #endregion

    #region Methods
    public void StartSequence(int SequenceID)
    {
        if (!IndexIsValid(SequenceID))
            return;

        MissionsSequence sequenceInfo = _sequences[SequenceID];

        if(!sequenceInfo.Started)
            sequenceInfo.StartNewMission();
    }

    public void FinishCurrentMissionInSequence (int SequenceID)
    {
        if (!IndexIsValid(SequenceID))
            return;

        MissionsSequence sequenceInfo = _sequences[SequenceID];
        sequenceInfo.FinishCurrentMission();
    }

    private bool IndexIsValid(int SequenceID)
    {
        bool IndexIsValid = !(SequenceID < 0) && !(SequenceID >= _sequences.Length);

        if (!IndexIsValid)
            Debug.LogError("Sequence with ID " + SequenceID + "doesn't exist!");

        return IndexIsValid;
    }

    private void OnEnable ()
    {
        foreach (MissionsSequence sequence in _sequences)
        {
            sequence.Subscribe();
        }
    }

    private void OnDisable ()
    {
        foreach (MissionsSequence sequence in _sequences)
        {
            sequence.Dispose();
        }
    }
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(MissionsManager))]
public class MissionsManagerEditor : Editor
{
    private static readonly string _sequencesPropertyName = "_sequences";
    private static readonly string _relativeSequenceIDPropertyName = "_id";
    private static readonly string _lastRemovedSequenceIndexPropertyName = "_lastRemovedSequenceIndex";
    private static readonly string _sequencesExpandedPropertyName = "_sequencesExpanded";


    private SerializedProperty _sequencesProperty;
    private SerializedProperty _lastRemovedSequenceIndexProperty;
    private SerializedProperty _sequencesExpandedProperty;

    private void OnEnable ()
    {
        _sequencesProperty = serializedObject.FindProperty(_sequencesPropertyName);
        _lastRemovedSequenceIndexProperty = serializedObject.FindProperty(_lastRemovedSequenceIndexPropertyName);
        _sequencesExpandedProperty = serializedObject.FindProperty(_sequencesExpandedPropertyName);
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();

        if(_sequencesProperty != null)
        {
            int sequencesCount = _sequencesProperty.arraySize;

            if (sequencesCount == 0)
            {
                EditorGUILayout.LabelField("Current sequences:");
                EditorGUILayout.LabelField("NONE", new GUIStyle(EditorStyles.label) { normal = new GUIStyleState() { textColor = Color.red } });
                if(_sequencesExpandedProperty != null)
                    _sequencesExpandedProperty.boolValue = false;
            }
            else
            {
                bool expanded = EditorGUILayout.Foldout(_sequencesExpandedProperty.boolValue, "Current sequences:");
                _sequencesExpandedProperty.boolValue = expanded;

                if (expanded)
                {
                    MissionsManager manager = (MissionsManager)target;
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < sequencesCount; i++)
                    {
                        SerializedProperty arrayElement = _sequencesProperty.GetArrayElementAtIndex(i);
                        SerializedProperty idProperty = arrayElement.FindPropertyRelative(_relativeSequenceIDPropertyName);
                        EditorGUILayout.PropertyField(arrayElement);
                        idProperty.intValue = i;
                        EditorGUILayout.Space(30);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(35);

            EditorGUILayout.LabelField("Operations:");
            EditorGUILayout.Space(25);

            bool addButtonPressed = GUILayout.Button("Add new sequence.");
            if (addButtonPressed)
            {
                int insertIndex = _sequencesProperty.arraySize == 0 ? 0 : _sequencesProperty.arraySize - 1;
                _sequencesProperty.InsertArrayElementAtIndex(insertIndex);
            }
            EditorGUILayout.Space(25);

            if(sequencesCount > 0)
            {
                if(_lastRemovedSequenceIndexProperty != null)
                {
                    int lastRemovedIndex = _lastRemovedSequenceIndexProperty.intValue;
                    bool removeButtonPressed = GUILayout.Button("Remove sequence with id: ");
                    if (removeButtonPressed)
                    {
                        if(lastRemovedIndex >= sequencesCount || lastRemovedIndex <= 0)
                        {
                            Debug.LogError("Removing sequence's index is out of range!");
                        }
                        else
                        {
                            _sequencesProperty.DeleteArrayElementAtIndex(lastRemovedIndex);
                            _lastRemovedSequenceIndexProperty.intValue = -1;
                        }
                    }

                    lastRemovedIndex = _lastRemovedSequenceIndexProperty.intValue;
                    string indexText = lastRemovedIndex == -1 ? string.Empty : lastRemovedIndex.ToString();
                    string text = EditorGUILayout.TextField(indexText);
                    if(int.TryParse(text, out int result))
                    {
                        _lastRemovedSequenceIndexProperty.intValue = result;
                    }

                    EditorGUILayout.Space(25);
                }

                bool clearButtonPressed = GUILayout.Button("Clear sequences list.");
                if (clearButtonPressed)
                {
                    _sequencesProperty.ClearArray();
                }
                EditorGUILayout.Space(10);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

[Serializable, ExecuteInEditMode]
public class MissionsSequence
{
    #region Fields
    [Header("Sequence scriptable."), SerializeField] private MissionSequenceScriptable _sequence;
    [Header("Sequence id."), SerializeField] private int _id = 0;
    [Header("Total amount of missions."), SerializeField] private uint _totalMissions = 0;
    [Header("Number of waiting missions."), SerializeField] private uint _missionsWaiting = 0;
    [Header("Number of started missions."), SerializeField] private uint _missionsStarted = 0;
    [Header("Number of finished missions."), SerializeField] private uint _missionsFinished = 0;
    [Header("Number of failed missions"), SerializeField] private uint _missionsFailed = 0;
    [Header("Missions details."), SerializeField] private List<MissionData> _missions = new List<MissionData>();

    [SerializeField, HideInInspector] private bool _expanded = false;

    private MissionData _currentMission = null;
    private int _missionPointer = -1;
    #endregion

    #region Properties
    public bool Started { get; private set; } = false;
    public int ID { get { return ID; } set { _id = value; } }
    private uint currentMissionID => _currentMission == null ? 0 : _currentMission.ID;
    public event Action<MissionData> OnCurrentMissionStateChanged;
    #endregion

    #region Methods
    public void Subscribe()
    {
        if (_missions == null || _missions.Count == 0)
            return;

        foreach (MissionData mission in _missions)
        {
            mission.Subscribe();
            IMission controller = mission.Controller;

            if (controller != null)
            {
                controller.OnStarted += delegate { _missionsStarted++; OnCurrentMissionStateChanged?.Invoke(_currentMission); };
                controller.OnFinished += delegate { _missionsFinished++; _missionsStarted--; OnCurrentMissionStateChanged?.Invoke(_currentMission); };
            }
        }
    }

    public void Dispose()
    {
        if (_missions == null || _missions.Count == 0)
            return;

        foreach (MissionData mission in _missions)
        {
            mission.Dispose();
            IMission controller = mission.Controller;

            if (controller != null)
            {
                controller.OnStarted -= delegate { _missionsStarted++; OnCurrentMissionStateChanged?.Invoke(_currentMission); };
                controller.OnFinished -= delegate { _missionsFinished++; _missionsStarted--; OnCurrentMissionStateChanged?.Invoke(_currentMission); };
            }
        }
    }

    private bool IndexIsValid(int index)
    {
        if (index >= 0 && index < _missions.Count)
        {
            return true;
        }
        else
        {
            Debug.LogError("Mission pointer is out of range: " + index);
            return false;
        }
    }

    public void StartNewMission()
    {
        int newMissionPointerValue = _missionPointer + 1;
        StartMission(newMissionPointerValue);
    }

    private void StartMission(int newMissionPointerValue)
    {
        if (!IndexIsValid(newMissionPointerValue))
            return;

        if (_currentMission != null)
        {
            if (_currentMission.Controller.State == MissionState.Started)
            {
                Debug.Log("You need to complete current mission before start next!");
                return;
            }
            else if(_currentMission.Controller.State == MissionState.Finished)
            {
                Debug.Log("You need to wait until next mission has been started!");
                return;
            }
        }

        _missionPointer = newMissionPointerValue;
        _currentMission = _missions[newMissionPointerValue];
        _currentMission.Controller.Start(_missions[newMissionPointerValue].DelayBeforeStart);

        if(!Started)
            Started = true;
    }

    public void FinishCurrentMission()
    {
        if (_currentMission != null)
        {
            if (_currentMission.Controller.State != MissionState.Started)
            {
                Debug.Log("There is no started missions yet! Nothing to finish!");
                return;
            }
        }
        else
            return;

        if (_missionPointer <= (_missions.Count - 1))
        {
            _currentMission.Controller.Finish();
            _currentMission = null;
            StartNewMission();
        }
        else
            Debug.Log("Sequence completed!");
        
    }
    #endregion
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MissionsSequence))]
public class DisplayedSequenceDrawer: PropertyDrawer
{
    private static readonly string _sequencePropertyName = "_sequence"; 
    private static readonly string _idPropertyName = "_id";
    private static readonly string _totalMissionsPropertyName = "_totalMissions";
    private static readonly string _missionsWaitingPropertyName = "_missionsWaiting";
    private static readonly string _missionsStartedPropertyName = "_missionsStarted";
    private static readonly string _missionsFinishedPropertyName = "_missionsFinished";
    private static readonly string _missionsPropertyName = "_missions";
    private static readonly string _expandedPropertyName = "_expanded";

    private SerializedProperty FindPropertyFieldAndDrawIt(SerializedProperty serializedProperty, string propertyName)
    {
        SerializedProperty findingProperty = serializedProperty.FindPropertyRelative(propertyName);
        EditorGUILayout.PropertyField(findingProperty);
        return findingProperty;
    }

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty sequenceProperty = FindPropertyFieldAndDrawIt(property, _sequencePropertyName);
        MissionSequenceScriptable _sequence = (MissionSequenceScriptable)sequenceProperty.boxedValue;

        if (sequenceProperty != null && _sequence != null)
        {
            SerializedProperty idProperty = FindPropertyFieldAndDrawIt(property, _idPropertyName);

            SerializedProperty totalMissionsProperty = FindPropertyFieldAndDrawIt(property, _totalMissionsPropertyName);
            totalMissionsProperty.uintValue = (uint)_sequence.Missions.Length;
            SerializedProperty missionsWaitingProperty = FindPropertyFieldAndDrawIt(property, _missionsWaitingPropertyName);
            SerializedProperty missionsStartedProperty = FindPropertyFieldAndDrawIt(property, _missionsStartedPropertyName);
            SerializedProperty missionsFinishedProperty = FindPropertyFieldAndDrawIt(property, _missionsFinishedPropertyName);

            SerializedProperty missionsProperty = property.FindPropertyRelative(_missionsPropertyName);
            
            if(missionsProperty != null)
            {
                MissionsSequence sequenceInfo = property.boxedValue as MissionsSequence;

                SerializedProperty expandedProperty = property.FindPropertyRelative(_expandedPropertyName);

                if(expandedProperty != null)
                {
                    EditorGUILayout.Space(8);
                    expandedProperty.boolValue = EditorGUILayout.Foldout(expandedProperty.boolValue, "Missions in sequence:");

                    if(expandedProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        UpdateMissions(missionsProperty, sequenceProperty);
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        EditorUtility.SetDirty(property.serializedObject.targetObject);
    }

    public void UpdateMissions (SerializedProperty missionsProperty, SerializedProperty sequenceProperty)
    {
        MissionSequenceScriptable sequence = (MissionSequenceScriptable)sequenceProperty.boxedValue;

        if (sequence == null)
            return;

        MissionData[] data = sequence.Missions;
        if (data == null || data.Length == 0)
        {
            missionsProperty.ClearArray();
            return;
        }

        List<MissionData> currentMissions = new List<MissionData>();

        for(int i = 0; i < missionsProperty.arraySize; i++)
        {
            currentMissions.Add((MissionData)(missionsProperty.GetArrayElementAtIndex(i).boxedValue));
        }

        for (int i = 0; i < data.Length; i++)
        {
            MissionData originalMissionData = data[i];
            uint originalMissionID = originalMissionData.ID;
            IEnumerable<MissionData> missionsWithSameOriginalID = currentMissions.Where(mission => mission.ID == originalMissionID);

            if(missionsWithSameOriginalID != null && missionsWithSameOriginalID.Count() > 0)
            {
                MissionData missionClone = missionsWithSameOriginalID.FirstOrDefault();
                int cloneIndex = currentMissions.IndexOf(missionClone);

                if(cloneIndex != i)
                {
                    missionsProperty.MoveArrayElement(cloneIndex, i);
                }
            }
            else
            {
                int newElementIndex = missionsProperty.arraySize;
                missionsProperty.arraySize++;
                missionsProperty.InsertArrayElementAtIndex(newElementIndex);
                SerializedProperty newElement = missionsProperty.GetArrayElementAtIndex(newElementIndex);
                MissionData newMissionData = (MissionData)originalMissionData.Clone();
                newElement.boxedValue = newMissionData;
                missionsProperty.MoveArrayElement(newElementIndex, i);
            }
        }

        int missionsArraySize = missionsProperty.arraySize;

        if (missionsArraySize <= 0)
            return;

        for(int i = 0; i < missionsArraySize; i++)
        {
            if (missionsArraySize > missionsProperty.arraySize)
                break;

            EditorGUILayout.LabelField("______Mission #" + i + " ______");
            SerializedProperty arrayElement = missionsProperty.GetArrayElementAtIndex(i);
            MissionData arrayElementData = (MissionData)arrayElement.boxedValue;
            IEnumerable<MissionData> missionsWithOriginalID = data.Where(mission => mission.ID == arrayElementData.ID);

            if (missionsWithOriginalID == null || missionsWithOriginalID.Count() == 0)
            {
                missionsProperty.DeleteArrayElementAtIndex(i);
                continue;
            }

            EditorGUILayout.PropertyField(arrayElement);
        }
    }
}
#endif