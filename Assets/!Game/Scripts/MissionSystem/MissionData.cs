using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[Serializable, ExecuteInEditMode]
public class MissionData : ICloneable
{
    #region Fields
    [SerializeField] private uint _id = 0;
    [SerializeField] private string _name = "Mission";
    [SerializeField] private string _description = "Description";
    [SerializeField] private string _currentStateName = "Waiting";
    [SerializeField] private float _delayBeforeStart = 0f;
    [SerializeField, HideInInspector] private MonoScript _scriptAsset;
    [SerializeField, HideInInspector] private MissionSequenceScriptable _sequence;
    [SerializeField, HideInInspector] private bool _isClone = false;

    private IMission _controller = null;
    #endregion

    #region Properties

    public IMission Controller 
    {
        get
        {
            if (_controller == null)
                GetRealization();

            return _controller;
        } 
    }
    public MissionSequenceScriptable Sequence => _sequence;
    public MonoScript ScriptAsset => _scriptAsset;
    public string Name => _name;
    public string CurrentStateName => _currentStateName;
    public string Description => _description;
    public uint ID => _id;
    public float DelayBeforeStart => _delayBeforeStart;
    public string SequenceName => _sequence != null ? _sequence.Name : string.Empty;
    #endregion

    #region Constructor
    public MissionData(uint ID, float Delay, string Name, string Description, string CurrentStateName, MonoScript ScriptAsset, MissionSequenceScriptable Sequence, bool IsCopy = false)
    {
        _id = ID;
        _delayBeforeStart = Delay;
        _name = Name;
        _description = Description;
        _currentStateName = CurrentStateName;
        _scriptAsset = ScriptAsset;
        _sequence = Sequence;
        _isClone = IsCopy;
    }
    #endregion

    #region Methods
    public static uint GenerateID () => (uint)(UnityEngine.Random.Range(10_000_000, 99_999_999));

    private void GetRealization()
    {
        if (_scriptAsset == null)
            return;

        if (_controller != null)
            return;

        Type realizationType = _scriptAsset.GetClass();
        _controller = (IMission)Activator.CreateInstance(realizationType);
    }

    [ExecuteInEditMode]
    public object Clone()
    {
        return new MissionData(_id, _delayBeforeStart, _name, _description, _currentStateName, _scriptAsset, _sequence, true);
    }

    [ExecuteInEditMode]
    public void CloneValuesFromOther(MissionData OriginData)
    {
        _id = OriginData.ID;
        _delayBeforeStart = OriginData.DelayBeforeStart;
        _name = OriginData.Name;
        _description = OriginData.Description;
        _scriptAsset = OriginData.ScriptAsset;
        _sequence = OriginData.Sequence;
    }

    public void Subscribe()
    {
        if (Controller != null)
            Controller.OnStateChanged += (state) => { _currentStateName = state.ToString(); };
    }

    public void Dispose()
    {
        if (Controller != null)
            Controller.OnStateChanged -= (state) => { _currentStateName = state.ToString(); };
    }
    #endregion

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MissionData))]
public class MissionDataDrawer : PropertyDrawer
{
    private static readonly string _idPropertyName = "_id";
    private static readonly string _scriptAssetPropertyName = "_scriptAsset";
    private static readonly string _namePropertyName = "_name";
    private static readonly string _descriptionPropertyName = "_description";
    private static readonly string _isClonePropertyName = "_isClone";
    private static readonly string _sequencePropertyName = "_sequence";
    private static readonly string _currentStateName = "_currentStateName";
    private static readonly string _delayBeforeStartPropertyName = "_delayBeforeStart";

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty idProperty = property.FindPropertyRelative(_idPropertyName);
        EditorGUILayout.PropertyField(idProperty);
        SerializedProperty delayProperty = property.FindPropertyRelative(_delayBeforeStartPropertyName);
        EditorGUILayout.PropertyField(delayProperty);
        SerializedProperty nameProperty = property.FindPropertyRelative(_namePropertyName);
        EditorGUILayout.PropertyField(nameProperty);
        SerializedProperty descriptionProperty = property.FindPropertyRelative(_descriptionPropertyName);
        EditorGUILayout.PropertyField(descriptionProperty);
        SerializedProperty currentStateProperty = property.FindPropertyRelative(_currentStateName);
        EditorGUILayout.PropertyField(currentStateProperty);
        SerializedProperty scriptAssetProperty = property.FindPropertyRelative(_scriptAssetPropertyName);

        MonoScript monoScript = (MonoScript)scriptAssetProperty.objectReferenceValue;

        if (monoScript != null)
            scriptAssetProperty.objectReferenceValue = monoScript;

        scriptAssetProperty.objectReferenceValue = EditorGUILayout.ObjectField("Realization:", scriptAssetProperty.objectReferenceValue, typeof(MonoScript), false);

        SerializedProperty isCloneProperty = property.FindPropertyRelative(_isClonePropertyName);

        if (isCloneProperty.boolValue && !Application.IsPlaying(property.serializedObject.targetObject))
        {
            SerializedProperty sequenceProperty = property.FindPropertyRelative(_sequencePropertyName);
            MissionSequenceScriptable sequence = (MissionSequenceScriptable)sequenceProperty.boxedValue;

            if(sequence != null)
            {
                IEnumerable<MissionData> originalDataMatch = sequence.Missions.Where(mission => mission.ID == idProperty.uintValue);

                if(originalDataMatch != null && originalDataMatch.Count() > 0)
                {
                    MissionData originalData = originalDataMatch.FirstOrDefault();

                    idProperty.uintValue = originalData.ID;
                    delayProperty.floatValue = originalData.DelayBeforeStart; 
                    nameProperty.stringValue = originalData.Name;
                    descriptionProperty.stringValue = originalData.Description;
                    scriptAssetProperty.objectReferenceValue = originalData.ScriptAsset;
                    sequenceProperty.objectReferenceValue = originalData.Sequence;
                    currentStateProperty.stringValue = originalData.CurrentStateName;
                }
            }
        }

        EditorUtility.SetDirty(property.serializedObject.targetObject);
        EditorGUILayout.Space(10);
        EditorGUI.EndProperty();
    }
}
#endif