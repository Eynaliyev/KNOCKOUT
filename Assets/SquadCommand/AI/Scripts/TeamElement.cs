using RAIN.Core;
using RAIN.Entities;
using RAIN.Serialization;
using UnityEngine;

/// <summary>
/// TeamElement is a CustomAIElement that appears in the Custom tab of a RAIN AI Rig
/// This Element is responsible for managing team setup, including assigning a team aspect
/// and setting up team-based communication
/// </summary>
[RAINSerializableClass, RAINElement("Team")]
public class TeamElement : CustomAIElement
{
    /// <summary>
    /// The default prefix of the teamm communication channel
    /// </summary>
    public const string cnstDefaultTeamCommPrefix = "teamcomm";

    /// <summary>
    /// The default variable assigned into AI memory indicating its team
    /// </summary>
    public const string cnstDefaultTeamVariable = "team";

    /// <summary>
    /// The default variable assigned into AI memory indicating the team communication channel
    /// </summary>
    public const string cnstDefaultTeamCommVariable = "teamComm";

    /// <summary>
    /// A string representing the team for this AI
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Team")]
    private string team;

    /// <summary>
    /// The prefix used to create a comm channel for the team.  The team name is appended to
    /// this to create the full channel name
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Team Comm Channel prefix")]
    private string teamCommPrefix = cnstDefaultTeamCommPrefix;

    /// <summary>
    /// A variable used to store the team in AI memory
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Memory variable to assign team to")]
    private string teamVariable = cnstDefaultTeamVariable;

    /// <summary>
    /// A variable used to store the team communication channel in AI memory
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Memory variable to assign team comm to")]
    private string teamCommVariable = cnstDefaultTeamCommVariable;

    /// <summary>
    /// The mount point for creating a team aspect on the AI body
    /// </summary>
    [RAINSerializableField(Visibility = FieldVisibility.Show, ToolTip = "Mount Point for Team Aspect")]
    private GameObject aspectMountPoint = null;

    /// <summary>
    /// Store the local entity in order to update the team aspect as necessary
    /// </summary>
    private EntityRig localEntity;

    /// <summary>
    /// Store the team Aspect in order to update it
    /// </summary>
    private TeamAspect teamAspect;

    /// <summary>
    /// Store the communication element for quick access when sending messages
    /// </summary>
    private CommunicationElement commElement;

    /// <summary>
    /// Accessor for the Team.  Updates aspects and comm channels on Set
    /// </summary>
    public string Team
    {
        get { return team; }
        set
        {
            SetTeam(value);
        }
    }

    /// <summary>
    /// Grab the Entity on the AI, or create one if none exists.  Grab the team Aspect on the AI, or create
    /// one if none exists.  Grab the communication element on the AI, or create one if none exists.  Then
    /// set up the team.
    /// </summary>
    public override void Start()
    {
        base.Start();

        localEntity = AI.Body.GetComponentInChildren<EntityRig>();
        if (localEntity == null)
            localEntity = EntityRig.AddRig(AI.Body);

        teamAspect = localEntity.Entity.GetAspect("team") as TeamAspect;
        if (teamAspect == null)
        {
            teamAspect = new TeamAspect() { AspectName = "team" };
            if (aspectMountPoint != null)
                teamAspect.MountPoint = aspectMountPoint.transform;
            else
                teamAspect.Position += Vector3.up; //move it up off the ground

            localEntity.Entity.AddAspect(teamAspect);
        }

        commElement = AI.GetCustomElement<CommunicationElement>();
        if (commElement == null)
        {
            commElement = new CommunicationElement();
            AI.AddCustomElement(commElement);
        }

        if (string.IsNullOrEmpty(teamCommPrefix))
            teamCommPrefix = cnstDefaultTeamCommPrefix;

        SetTeam(team, true);
    }

    /// <summary>
    /// SetTeam does the work of setting up comm channels and aspects, and creating variables in AI memory
    /// </summary>
    /// <param name="aNewTeam">The name of the team to set up</param>
    /// <param name="aForce">When true, force the setup to happen even if we are setting the same team the AI is already on.</param>
    private void SetTeam(string aNewTeam, bool aForce = false)
    {
        //If we already have the team set, do nothing unless we are forcing it
        if (!aForce && (team == aNewTeam))
            return;

        //Set aspect and team
        teamAspect.team = aNewTeam;
        team = aNewTeam;

        //Set up the team var in memory
        if (!string.IsNullOrEmpty(teamVariable))
            AI.WorkingMemory.SetItem<string>(teamVariable, team);

        //Unsubscribe from existing team comm
        for (int i = commElement.Channels.Count - 1; i >= 0; i--)
        {
            if (commElement.Channels[i].StartsWith(teamCommPrefix))
                commElement.UnsubscribeFrom(commElement.Channels[i]);
        }

        //Subscribe to team comm and set the variable in memory
        string teamComm = null;
        if (!string.IsNullOrEmpty(team))
        {
            teamComm = teamCommPrefix + team;
            commElement.SubscribeTo(teamComm);
        }
        if (!string.IsNullOrEmpty(teamCommVariable))
            AI.WorkingMemory.SetItem<string>(teamCommVariable, teamComm);
    }
}
