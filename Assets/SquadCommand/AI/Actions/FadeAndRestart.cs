using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

[RAINAction]
public class FadeAndRestart : RAINAction
{
    /// <summary>
    /// Delay before the fade starts
    /// </summary>
    private float fadeDelay = 5f;

    /// <summary>
    /// Screen fader object attached to the AI
    /// </summary>
    private FadeToBlack fader;

    /// <summary>
    /// Reset the fade delay and grab the fader
    /// </summary>
    /// <param name="ai">The AI executing this action</param>
    public override void Start(AI ai)
    {
        base.Start(ai);

        fader = ai.Body.GetComponentInChildren<FadeToBlack>();
        fadeDelay = 5f;
    }

    /// <summary>
    /// After the fade delay, set the fadereset to true
    /// </summary>
    /// <param name="ai">The AI executing this action</param>
    /// <returns>RUNNING while waiting for fadeDelay, SUCCESS AFTER THAT</returns>
    public override ActionResult Execute(AI ai)
    {
        if (fadeDelay > 0)
        {
            fadeDelay -= ai.DeltaTime;
            return ActionResult.RUNNING;
        }

        if (fader != null)
            fader.fadeToReset = true;

        return ActionResult.SUCCESS;
    }
}