using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModestTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission_Sequence", menuName = "ScriptableObjects/New MissionData Sequence Scriptable", order = 1)]
public class MissionSequenceScriptable : ScriptableObject
{
    [Header("Name of the missions sequence."), SerializeField] private string _name = "Sequence";
    [Header("Total delay before start for each mission."), SerializeField] private float _totalDelayInSeconds = 0f;
    [Header("Missions in the sequence (ordered)."), SerializeField] private MissionData[] _missions;
    [SerializeField, HideInInspector] private bool _isFoldout = false;
    [SerializeField, HideInInspector] private uint _lastRemovedMissionID = 0;

    public MissionData[] Missions => _missions;
    public float TotalDelayInSeconds => _totalDelayInSeconds;
    public string Name => _name;
}

#if UNITY_EDITOR
[CustomEditor(typeof(MissionSequenceScriptable))]
public class MissionSequenceScriptableEditor : Editor
{
    private readonly string _sequenceNamePropertyName = "_name";
    private readonly string _totalDelayInSecondsPropertyName = "_totalDelayInSeconds";
    private readonly string _missionsPropertyName = "_missions";
    private readonly string _isFoldoutPropertyName = "_isFoldout";
    private readonly string _lastRemovedIDPropertyName = "_lastRemovedMissionID";

    private SerializedProperty _sequenceNameProperty;
    private SerializedProperty _totalDelayProperty;
    private SerializedProperty _missionsProperty;
    private SerializedProperty _foldoutProperty;
    private SerializedProperty _lastRemovedMissionProperty;

    private void OnEnable ()
    {
        _sequenceNameProperty = serializedObject.FindProperty(_sequenceNamePropertyName);
        _totalDelayProperty = serializedObject.FindProperty(_totalDelayInSecondsPropertyName);
        _missionsProperty = serializedObject.FindProperty(_missionsPropertyName);
        _foldoutProperty = serializedObject.FindProperty(_isFoldoutPropertyName);
        _lastRemovedMissionProperty = serializedObject.FindProperty(_lastRemovedIDPropertyName);
    }

    public override void OnInspectorGUI ()
    {
        if (_missionsProperty == null)
            return;

        serializedObject.Update();

        EditorGUILayout.PropertyField(_sequenceNameProperty);
        EditorGUILayout.PropertyField(_totalDelayProperty);

        int arraySize = _missionsProperty.arraySize;

        if(arraySize > 0)
        {
            bool foldout = EditorGUILayout.Foldout(_foldoutProperty.boolValue, "Sequence missions:");
            _foldoutProperty.boolValue = foldout;

            if (foldout)
            {
                for (int i = 0; i < arraySize; i++)
                {
                    EditorGUILayout.Space(20);
                    EditorGUILayout.LabelField($"______Mission #{i}______");
                    EditorGUI.indentLevel++;
                    SerializedProperty arrayElement = _missionsProperty.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(arrayElement);
                    EditorGUI.indentLevel--;

                    SerializedProperty totalDelayProperty = serializedObject.FindProperty("_totalDelayInSeconds");
                    float totalDelay = totalDelayProperty.floatValue;

                    SerializedProperty sequenceProperty = arrayElement.FindPropertyRelative("_sequence");
                    sequenceProperty.boxedValue = (MissionSequenceScriptable)target;

                    if (totalDelay > 0)
                        arrayElement.FindPropertyRelative("_delayBeforeStart").floatValue = totalDelay;

                    SerializedProperty idProperty = arrayElement.FindPropertyRelative("_id");
                    uint idValue = idProperty.uintValue;

                    if (i == 0 && idValue != 0)
                        continue;

                    if (i > 0)
                    {
                        uint previousElementID = _missionsProperty.GetArrayElementAtIndex(i - 1).FindPropertyRelative("_id").uintValue;

                        if (idValue != 0 && idValue != previousElementID)
                            continue;
                    }

                    uint id = MissionData.GenerateID();
                    idProperty.uintValue = id;
                }
            }

            GUILayout.Space(25);
            bool clearButtonPressed = GUILayout.Button("Clear missions.");
            if(clearButtonPressed)
            {
                _missionsProperty.ClearArray();
            }

            if (_lastRemovedMissionProperty != null)
            {
                uint lastRemovedID = _lastRemovedMissionProperty.uintValue;
                GUILayout.Space(25);
                bool removeButtonPressed = GUILayout.Button("Remove mission with id: ");
                if (removeButtonPressed)
                {
                    MissionSequenceScriptable sequence = (MissionSequenceScriptable)target;
                    IEnumerable<MissionData> missionWithID = sequence.Missions.Where(mission => mission.ID == lastRemovedID);

                    if(missionWithID != null && missionWithID.Count() > 0)
                    {
                        int index = sequence.Missions.IndexOf(missionWithID.FirstOrDefault());
                        _missionsProperty.DeleteArrayElementAtIndex(index);
                    }
                    else
                    {
                        Debug.LogError("Wrong mission ID to delete!");
                    }
                }
            
                lastRemovedID = _lastRemovedMissionProperty.uintValue;
                string idText = lastRemovedID == 0 ? string.Empty : lastRemovedID.ToString();
                string text = EditorGUILayout.TextField(idText);
                if (uint.TryParse(text, out uint result))
                {
                    _lastRemovedMissionProperty.uintValue = result;
                }
                else
                {
                    _lastRemovedMissionProperty.uintValue = 0;
                }

                EditorGUILayout.Space(25);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Sequence missions:");
            EditorGUILayout.LabelField("NONE", new GUIStyle(EditorStyles.label) { normal = new GUIStyleState() { textColor = Color.red } });
        }

        GUILayout.Space(5);
        bool addElementButtonPressed = GUILayout.Button("Add new mission.");
        if (addElementButtonPressed)
        {
            int insertIndex = _missionsProperty.arraySize == 0 ? 0 : _missionsProperty.arraySize - 1;
            _missionsProperty.InsertArrayElementAtIndex(insertIndex);
        }
        GUILayout.Space(10);

        EditorUtility.SetDirty(serializedObject.targetObject);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif