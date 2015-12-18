using RAIN.Action;
using RAIN.Core;
using RAIN.Entities;
using RAIN.Entities.Aspects;
using RAIN.Representation;
using RAIN.Perception.Sensors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DetectEnemy is a RAIN behavior tree action that looks for enemies based on their Team.
/// To use it, create a custom action in your behavior tree and choose the "Detect Enemy" action.
/// Set EnemyVariable to the name of the variable you want to assign the enemy to in AI memory.
/// </summary>
[RAINAction("Detect Enemy")]
public class DetectEnemy : RAINAction
{
    /// <summary>
    /// The name of the variable that will contain the enemy game object when detected
    /// </summary>
    public Expression EnemyVariable = new Expression();

    /// <summary>
    /// We store our TeamElement so we don't have to retrieve it each time we run
    /// </summary>
    private TeamElement _teamElement = null;

    /// <summary>
    /// We remember our current enemy and prefer it as the enemy when we get more than one enemy result
    /// </summary>
    private GameObject _myEnemy = null;

    /// <summary>
    /// We remember our current enemy aspect to help speed up validation of our current enemy
    /// </summary>
    private TeamAspect _myEnemyAspect = null;

    /// <summary>
    /// The enemy variable name evaluated from the EnemyVariable expression
    /// </summary>
    private string _enemyVariableName = null;

    /// <summary>
    /// Set up the teamElement and enemyVariableName
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        //only look for the teamElement if we don't already have one 
        if (_teamElement == null)
            _teamElement = ai.GetCustomElement<TeamElement>();

        _enemyVariableName = null;
        if (EnemyVariable.IsValid)
        {
            if (EnemyVariable.IsVariable)
            {
                _enemyVariableName = EnemyVariable.VariableName;
            }
            else if (EnemyVariable.IsConstant)
            {
                _enemyVariableName = EnemyVariable.Evaluate<string>(ai.DeltaTime, ai.WorkingMemory);
            }
        }
    }

    /// <summary>
    /// When executing, this action uses Visual Sensors to look for team aspects.  It then
    /// looks for either the nearest match or a known enemy, then assigns the value (or null)
    /// to the EnemyVariable.
    /// </summary>
    /// <param name="ai">The AI executing the action</param>
    /// <returns>FAILURE if no match was found, SUCCESS otherwise</returns>
    public override ActionResult Execute(AI ai)
    {
        //Fail if our team or variable are missing
        if (string.IsNullOrEmpty(_enemyVariableName))
            return ActionResult.FAILURE;
        if (_teamElement == null)
            return ActionResult.FAILURE;

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        //Do a quick check to see if our last enemy is still valid
        if ((_myEnemyAspect != null) && (_myEnemyAspect.team != _teamElement.Team) &&
            (ai.Senses.IsDetected("Visual Sensor", _myEnemyAspect, "team") == _myEnemyAspect))
        {
            _myEnemy = _myEnemyAspect.Entity.Form;

            //set the enemy variable in AI memory
            ai.WorkingMemory.SetItem<GameObject>(_enemyVariableName, _myEnemy);

            return ActionResult.SUCCESS;
        }

        IList<RAINAspect> matches = ai.Senses.Sense("Visual Sensor", "team", RAINSensor.MatchType.ALL);
        //Get all matches from Visual Sensors
        foreach (TeamAspect aspect in matches)
        {
            if (aspect == null)
                continue;

            //If we find an aspect that doesn't match our team, check to see if it is a known
            //enemy or the nearest enemy
            if (aspect.team != _teamElement.Team)
            {
                float distance = Vector3.Distance(ai.Body.transform.position, aspect.Position);
                if (distance < closestDistance)
                {
                    closestEnemy = aspect.Entity.Form;
                    closestDistance = distance;
                }
            }
        }

        //store the enemy so we prefer it next time
        _myEnemy = closestEnemy;

        //set the enemy variable in AI memory
        ai.WorkingMemory.SetItem<GameObject>(_enemyVariableName, _myEnemy);

        //fail if no enemy found
        if (_myEnemy == null)
            return ActionResult.FAILURE;

        return ActionResult.SUCCESS;
    }
}