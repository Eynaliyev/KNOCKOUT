using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;
using RAIN.Navigation;
using RAIN.Navigation.Graph;

[RAINAction("Choose Wander Location")]
public class AIRandomWander : RAINAction
{
	private static float _startTime = 0f;

	public AIRandomWander()
	{
		actionName = "AIRandomWander";
	}

	public override void Start(AI ai)
	{
		_startTime += Time.time;

		base.Start(ai);
	}

	public override ActionResult Execute(AI ai)
	{
		Vector3 loc = Vector3.zero;

		List<RAINNavigationGraph> found = new List<RAINNavigationGraph>();
		do
		{
			loc = new Vector3(ai.Kinematic.Position.x + Random.Range(-5f, 5f),
				ai.Kinematic.Position.y,
				ai.Kinematic.Position.z + Random.Range(-5f, 5f));
			found = NavigationManager.Instance.GraphsForPoints(ai.Kinematic.Position, loc, ai.Motor.StepUpHeight, NavigationManager.GraphType.Navmesh, ((BasicNavigator)ai.Navigator).GraphTags);

		} while ((Vector3.Distance(ai.Kinematic.Position, loc) < 2f) || (found.Count == 0));

		ai.WorkingMemory.SetItem<Vector3>("wanderTarget", loc);

		if(_startTime > 500f)
		{
			ai.WorkingMemory.SetItem("DoSearch", 0);
			_startTime = 0;
		}

		return ActionResult.SUCCESS;
	}

	public override void Stop(AI ai)
	{
		base.Stop(ai);
	}
}