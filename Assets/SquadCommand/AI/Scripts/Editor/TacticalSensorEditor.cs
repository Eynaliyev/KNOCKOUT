using RAINEditor;
using RAINEditor.Perception.Sensors;
using RAIN.Perception.Sensors;
using RAIN.Serialization;
using System;
using UnityEditor;

/// <summary>
/// Editor for the Tactical Sensor for finding cover points and other tactical elements with the "tactical" aspect
/// NOTE: This sensor does not have a visualization, so you won't see the sensor ring in the Unity Editor
/// </summary>
public class TacticalSensorEditor : RAINSensorEditor
{
    /// <summary>
    /// The sensor type, used by RAIN to manage sensor lists
    /// </summary>
    public override Type EditedType
    {
        get { return typeof(TacticalSensor); }
    }

    /// <summary>
    /// Support switching between Advanced and Basic modes of the RAIN Editor
    /// </summary>
    /// <param name="aLabel">The sensor label to draw (unused)</param>
    /// <param name="aWalker">The RAIN serialized data for the sensor</param>
    /// <returns>true if the field was edited by the user, false otherwise</returns>
    public override bool DrawInspector(string aLabel, FieldWalkerList aWalker)
    {
        bool tDirty = false;

        if (aWalker.FirstChild())
        {
            do
            {
                if (RAINSettings.Instance.ShowAdvanced)
                {
                    if (aWalker.FieldName == "_sensorColor" ||
                        aWalker.FieldName == "_mountPoint" ||
                        aWalker.FieldName == "_canDetectSelf")
                    {
                        EditorGUILayout.Space();
                    }
                }

                tDirty |= DrawFieldForInspector(aWalker);
            }
            while (aWalker.NextSibling());
        }

        return tDirty;
    }
}

