using RAIN.Entities.Aspects;
using RAIN.Serialization;

/// <summary>
/// The team aspect is a special Visual Aspect that adds a team attribute.  This can be detected by regular Visual Sensors.
/// </summary>
[RAINSerializableClass]
public class TeamAspect : VisualAspect
{
    [RAINSerializableField]
    public string team;
}
