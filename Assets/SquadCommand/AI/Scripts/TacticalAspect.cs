using RAIN.Entities.Aspects;
using RAIN.Serialization;

/// <summary>
/// A Tactical Aspect to match the Tactical Sensor.  The tactical aspect doesn't have any special attributes.
/// </summary>
[RAINSerializableClass]
public class TacticalAspect : RAINAspect
{
    public static string cnstAspectType = "tactical";

    public override string AspectType
    {
        get { return cnstAspectType; }
    }
}
