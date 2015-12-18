using RAIN.Core;
using RAIN.Perception.Sensors;
using RAIN.Entities.Aspects;
using RAIN.Serialization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A special tactical sensor for detecting tactical aspects.  Note that since this sensor does not derive from
/// Visual Sensor, we won't see a visualization in the RAIN Editor
/// </summary>
[RAINSerializableClass, RAINElement("Tactical Sensor")]
public class TacticalSensor : PhysicalSensor
{
    public override string SensedAspectType
    {
        get { return TacticalAspect.cnstAspectType; }
    }
}
