using RAIN.Core;
using RAIN.Serialization;
using UnityEngine;

/// <summary>
/// PlayerInputElement is a CustomAIElement that appears in the Custom tab of a RAIN AI Rig
/// This Element is responsible for overriding AI control with player control
/// </summary>
[RAINSerializableClass, RAINElement("Player Input")]
public class PlayerInputElement : CustomAIElement
{
    /// <summary>
    /// Mouse sensitivity - higher numbers = greater sensitivity
    /// </summary>
    [RAINSerializableField]
    private float _mouseSensitivity = 100;

    /// <summary>
    /// The game manager, which controls camera view
    /// </summary>
    private GameManager _gameManager = null;

    /// <summary>
    /// Cache our AimAndFireElement
    /// </summary>
    private AimAndFireElement _aimElement;

    /// <summary>
    /// Track our aim tilt, since we need to override the default AI aiming
    /// </summary>
    private float _aimTilt = 0;

    /// <summary>
    /// Keep track of the last command for showing in the UI
    /// </summary>
    private string _lastCommand = "";

    /// <summary>
    /// Timer for UI when showing commands
    /// </summary>
    private float _lastCommandTimer = 0f;

    private FormationHarnessElement _formationHarnessElement = null;

    /// <summary>
    /// On Start, grab the game manager and AimAndFireElement
    /// </summary>
    public override void Start()
    {
        base.Start();

        // Grab the game manager from the scene
        _gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();

        // Set our accuracy to 0 (0 error that is) and our muzzle tip to our camera so we fire straight
        _aimElement = AI.GetCustomElement<AimAndFireElement>();
        _aimElement.Accuracy = 0;

        _formationHarnessElement = AI.GetCustomElement<FormationHarnessElement>();
    }

    /// <summary>
    /// Pre is called before Think or Act on AI
    /// Used here to set aim and move targets prior to AI Act
    /// This is als where we'll handle most of our input
    /// </summary>
    public override void Pre()
    {
        base.Pre();

        if (_gameManager.CurrentGameMode == GameManager.GameMode.NOT_STARTED)
            return;

        // We'll do turning by setting our face target
        float tTurnLeftRight = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;

        // We'll amp the turn speed way up so it's super responsive
        Vector3 tFaceDirection = AI.Body.transform.TransformDirection(Quaternion.Euler(new Vector3(0f, tTurnLeftRight, 0f)) * Vector3.forward);
        AI.WorkingMemory.SetItem<Vector3>("faceTarget", AI.Body.transform.position + tFaceDirection);
        AI.WorkingMemory.SetItem<float>("turnSpeed", 21600);

        // Tilt is handled by an element (and animation)
        float tTurnUpDown = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.deltaTime;

        // Add to our aim tilting (the clamp is based on our controller plus some magic)
        _aimTilt = Mathf.Clamp(_aimTilt + tTurnUpDown, -87, 75);

        // And set our aim target based on where we are going to be after we apply our turn and tilt
        Vector3 tAimDirection = AI.Body.transform.TransformDirection(Quaternion.Euler(new Vector3(-_aimTilt, 0, 0)) * Vector3.forward);
        _aimElement.SetAimTarget(GetActualTarget(_gameManager.CurrentCameraMount.position + tAimDirection * _aimElement.Controller.fireRange));
        _gameManager.CurrentCameraAim.position = _gameManager.CurrentCameraMount.position + tAimDirection * _aimElement.Controller.fireRange;

        // Set the direction of movement based on our WASD keys (or whatever is set)
        Vector3 tMoveDirection = Vector3.zero;
        float tForward = Input.GetAxis("Vertical");
        if (!Mathf.Approximately(tForward, 0))
            tMoveDirection += AI.Body.transform.forward * Mathf.Sign(tForward);
        float tStrafe = Input.GetAxis("Horizontal");
        if (!Mathf.Approximately(tStrafe, 0))
            tMoveDirection += AI.Body.transform.right * Mathf.Sign(tStrafe);

        //Set our move target based on our movement
        AI.WorkingMemory.SetItem<Vector3>("moveTarget", AI.Body.transform.position + tMoveDirection);

        // Our walking or running
        bool tSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (tSprint)
            AI.WorkingMemory.SetItem<float>("moveSpeed", AI.WorkingMemory.GetItem<float>("sprintSpeed"));
        else
            AI.WorkingMemory.SetItem<float>("moveSpeed", AI.WorkingMemory.GetItem<float>("walkSpeed"));

        // Firing or not
        AI.WorkingMemory.SetItem<bool>("firing", Input.GetButton("Fire1"));

        // Firing or not
        AI.WorkingMemory.SetItem<bool>("reload", Input.GetKeyDown(KeyCode.R));

        // When we aren't moving, allow the player to turn without the formation harness turning too.
        if (_formationHarnessElement.ActiveHarness != null)
        {
            if (Mathf.Approximately(tForward, 0) && Mathf.Approximately(tStrafe, 0))
                _formationHarnessElement.ActiveHarness.rotatesWithObject = false;
            else
                _formationHarnessElement.ActiveHarness.rotatesWithObject = true;
        }

        // This is our last command timer
        if (_lastCommandTimer <= 0f)
            _lastCommand = "";
        else
            _lastCommandTimer -= Time.deltaTime;

        // We'll throw reload in here (it could have been an action)
        if (Input.GetKeyUp(KeyCode.R))
            _aimElement.Reload();

        // Allow the player to issue commands to the squad
        if (Input.GetKeyDown("1"))
            SendCommand("form up");
        else if (Input.GetKeyDown("2"))
            SendCommand("attack");
        else if (Input.GetKeyDown("3"))
            SendCommand("cover me");
        else if (Input.GetKeyDown("4"))
            SendCommand("take cover");
        else if (Input.GetKeyDown("5"))
            SendCommand("flank");

        //Allow the player to switch squad formations
        if (Input.GetKeyDown(KeyCode.F1))
            SetPlayerFormation("wedge");
        else if (Input.GetKeyDown(KeyCode.F2))
            SetPlayerFormation("column");
        else if (Input.GetKeyDown(KeyCode.F3))
            SetPlayerFormation("skirmish left");
        else if (Input.GetKeyDown(KeyCode.F4))
            SetPlayerFormation("skirmish right");
        else if (Input.GetKeyDown(KeyCode.F5))
            SetPlayerFormation("echelon left");
        else if (Input.GetKeyDown(KeyCode.F6))
            SetPlayerFormation("echelon right");
    }

    /// <summary>
    /// OnGUI is used to display the last command in the UI temporarily
    /// </summary>
    private void OnGUI()
    {
        if (_lastCommandTimer > 0)
            GUI.Label(new Rect(Screen.width - 200, 5, 200, 20), _lastCommand);
    }

    /// <summary>
    /// Use raycasting to determine where our target is for firing.  This helps to adjust the offset between
    /// our reticule and our gun barrel
    /// </summary>
    /// <param name="aDesiredTarget"></param>
    /// <returns></returns>
    private Vector3 GetActualTarget(Vector3 aDesiredTarget)
    {
        RaycastHit tRaycastHit;
        if (Physics.Raycast(_gameManager.CurrentCameraMount.position,
                            aDesiredTarget - _gameManager.CurrentCameraMount.position,
                            out tRaycastHit,
                            _aimElement.Controller.fireRange,
                            _aimElement.Controller.hitLayers))
        {
            return tRaycastHit.point;
        }

        return aDesiredTarget;
    }

    /// <summary>
    /// Send a command to your squad through the communication system.  This also sets last command
    /// for displaying on the UI
    /// </summary>
    /// <param name="aCommand">The command to send</param>
    private void SendCommand(string aCommand)
    {
        string tChannel = AI.WorkingMemory.GetItem<string>("teamComm");
        CommunicationManager.Instance.Broadcast(tChannel, "command", aCommand);

        _lastCommand = "Team command: " + aCommand;
        _lastCommandTimer = 3f;
    }

    /// <summary>
    /// Set the player formation via the FormationHarnessElement.  This also sets the last command
    /// for displayin in the UI
    /// </summary>
    /// <param name="aFormation">The name of the formation to use.  This translates to Formation Mode.</param>
    private void SetPlayerFormation(string aFormation)
    {
        FormationHarnessElement tElement = AI.GetCustomElement<FormationHarnessElement>();
        if (tElement != null)
            tElement.FormationMode = aFormation;

        _lastCommand = "Squad formation: " + aFormation;
        _lastCommandTimer = 3f;
    }
}