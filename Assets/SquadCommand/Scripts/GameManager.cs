using UnityEngine;

/// <summary>
/// Game Manager is a skeleton framework for managing game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// The various game states
    /// </summary>
    public enum GameMode
    {
        RUN_ONCE,
        NOT_STARTED,
        PLAYING,
        WIN,
        DEAD
    }

    /// <summary>
    /// Game manager manages camera views too
    /// </summary>
    public enum CameraView
    {
        FIRST = 0,
        THIRD = 1,
        COUNT = 2
    }

    /// <summary>
    /// The camera used for everything.  It needs to have a Follow Camera script on it
    /// </summary>
    [SerializeField]
    private Camera _followCamera = null;

    /// <summary>
    /// The renderer for the player.  This is managed when switch views to set up the appropriate player body
    /// </summary>
    [SerializeField]
    private Renderer _playerRender = null;

    /// <summary>
    /// Camera mount for first person view
    /// </summary>
    [SerializeField]
    private GameObject _fpsCameraMount = null;

    /// <summary>
    /// Camera aim point for first person view
    /// </summary>
    [SerializeField]
    private GameObject _fpsAim = null;

    /// <summary>
    /// Camera mount for third person view
    /// </summary>
    [SerializeField]
    private GameObject _tpsCameraMount = null;

    /// <summary>
    /// Camera aim point for third person view
    /// </summary>
    [SerializeField]
    private GameObject _tpsAim = null;

    /// <summary>
    /// Camera mount for following defenders before the game starts
    /// </summary>
    [SerializeField]
    private GameObject _defenderCameraMount = null;

    [SerializeField]
    private MissionTextDisplay _missionTextDisplay = null;

    /// <summary>
    /// Texture for the targeting reticule
    /// </summary>
    [SerializeField]
    private Texture2D _targetReticle = null;

    /// <summary>
    /// Texture for intro logo
    /// </summary>
    [SerializeField]
    private Texture _rtLogo = null;

    /// <summary>
    /// Materials for setting up the renderer when switching views
    /// </summary>
    private Material[] _playerMaterials;

    /// <summary>
    /// The follow script on the camera
    /// </summary>
    private SmoothFollow _followScript;

    /// <summary>
    /// Current game mode
    /// </summary>
    private GameMode _gameMode = GameMode.RUN_ONCE;

    /// <summary>
    /// Current camera view
    /// </summary>
    private CameraView _cameraView = CameraView.THIRD;

    /// <summary>
    /// Should the target reticule and player UI be displayed
    /// </summary>
    private bool _showUI = false;

    /// <summary>
    /// Flag to force a level restart
    /// </summary>
    private bool _restart = false;

    /// <summary>
    /// Time before fading the RT logo
    /// </summary>
    private float _introTimer = 2f;

    /// <summary>
    /// A state timer for general use when timing state events
    /// </summary>
    private float _stateTimer = 0f;

    /// <summary>
    /// Alpha fade for the intro logo
    /// </summary>
    private float _alphaFadeValue = 1f;

    /// <summary>
    /// Game mode accessor
    /// </summary>
    public GameMode CurrentGameMode
    {
        get { return _gameMode; }
    }

    /// <summary>
    /// Current camera view accessor
    /// </summary>
    public CameraView CurrentCameraView
    {
        get { return _cameraView; }
    }

    /// <summary>
    /// Current camera mount accessor.  This switches based on camera view
    /// </summary>
    public Transform CurrentCameraMount
    {
        get
        {
            switch (_cameraView)
            {
                case CameraView.FIRST:
                    return _fpsCameraMount.transform;

                case CameraView.THIRD:
                    return _tpsCameraMount.transform;
            }

            return null;
        }
    }

    /// <summary>
    /// Current camera aim accessor  This switches based on camera view
    /// </summary>
    public Transform CurrentCameraAim
    {
        get
        {
            switch (_cameraView)
            {
                case CameraView.FIRST:
                    return _fpsAim.transform;

                case CameraView.THIRD:
                    return _tpsAim.transform;
            }

            return null;
        }
    }

    /// <summary>
    /// Set up the player materials and camera.  Transition to NOTSTARTED mode
    /// </summary>
    private void Start()
    {
        _playerMaterials = _playerRender.materials;

        _followScript = _followCamera.GetComponent<SmoothFollow>();

        _showUI = false;

        //set up the camera for our logo shot
        if (_defenderCameraMount != null)
        {
            _followScript.target = _defenderCameraMount.transform;
            _followScript.distance = 6f;
            _followScript.height = 12f;
            _followScript.heightDamping = 2.0f;
            _followScript.rotationDamping = 3.0f;
        }
        else
        {
            //simulate first person
            _followScript.distance = 0.1f;
            _followScript.height = 0f;
            _followScript.heightDamping = 100f;
            _followScript.rotationDamping = 100f;
            _followScript.target = _fpsCameraMount.transform;
            _followScript.lookAt = _fpsAim.transform;

            _playerRender.materials = new Material[] { };
        }
    }

    /// <summary>
    /// Switch between our different game modes and call the appropriate update method
    /// </summary>
    private void Update()
    {
        switch (_gameMode)
        {
            case GameMode.RUN_ONCE:
                DoRunOnce();
                break;

            case GameMode.NOT_STARTED:
                DoNotStarted();
                break;

            case GameMode.PLAYING:
                DoPlaying();
                break;

            case GameMode.WIN:
                DoWin();
                break;

            case GameMode.DEAD:
                DoDead();
                break;
        }
    }

    /// <summary>
    /// Display the appropriate UI based on game mode
    /// </summary>
    private void OnGUI()
    {
        switch (_gameMode)
        {
            case GameMode.RUN_ONCE:
                GuiRunOnce();
                break;

            case GameMode.NOT_STARTED:
                GuiNotStarted();
                break;

            case GameMode.PLAYING:
                GuiPlaying();
                break;

            case GameMode.WIN:
                GuiWin();
                break;

            case GameMode.DEAD:
                GuiDead();
                break;
        }

        if (_restart)
        {
            _restart = true;
            Application.LoadLevel(Application.loadedLevel);
            DoTransitionToNotStarted();
        }
    }

    /// <summary>
    /// Set up a follow camera for defender following when not started
    /// If there is no defender camera mount, just go to playing mode
    /// </summary>
    private void DoTransitionToNotStarted()
    {
        _gameMode = GameMode.NOT_STARTED;

        if (_defenderCameraMount == null)
        {
            DoTransitionToPlaying();
        }
        else
        {
            _followScript.target = _defenderCameraMount.transform;
            _followScript.distance = 6f;
            _followScript.height = 12f;
            _followScript.heightDamping = 2.0f;
            _followScript.rotationDamping = 3.0f;

            if (_missionTextDisplay != null)
                _missionTextDisplay.gameObject.SetActive(false);

            _stateTimer = 5f; //5 second delay before switching to first person mode
        }
    }

    /// <summary>
    /// RunOnce is used to display the opening screen logo
    /// </summary>
    private void DoRunOnce()
    {
        _introTimer -= Time.deltaTime;
        if (_introTimer > 0f)
            return;

        _alphaFadeValue -= Mathf.Clamp01(Time.deltaTime / 2.5f);

        if (_introTimer <= -2.5f)
            DoTransitionToNotStarted();
    }

    /// <summary>
    /// GuiRunOnce actually draws the opening logo
    /// </summary>
    private void GuiRunOnce()
    {
        GUI.color = new Color(255, 255, 255, _alphaFadeValue);

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _rtLogo);
    }

    /// <summary>
    /// In the Not Started state, just wait for a space bar to switch to Playing
    /// </summary>
    private void DoNotStarted()
    {
        if (Input.GetKeyDown(KeyCode.Space) || (_stateTimer < 0f))
            DoTransitionToPlaying();

        _stateTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Show instructions to hit Space to start
    /// </summary>
    private void GuiNotStarted()
    {
        GUI.Label(new Rect(5, 5, 300, 20), "Space - Start");

        if (Input.GetKeyDown("0"))
            _restart = true;
    }

    /// <summary>
    /// When transitioning, switch the camera view, which will put the camera onto the player in FPS mode
    /// </summary>
    private void DoTransitionToPlaying()
    {
        DoSwitchCameraView();
        _gameMode = GameMode.PLAYING;

        if (_missionTextDisplay != null)
            _missionTextDisplay.gameObject.SetActive(true);
    }

    /// <summary>
    /// In playing mode, handle camera switching on the player and set up the screen cursor appropriately
    /// </summary>
    private void DoPlaying()
    {
        if (Input.GetKeyDown("0"))
            _restart = true;

        if (Input.GetKeyDown("v"))
            DoSwitchCameraView();
        if (Input.GetKeyDown(KeyCode.Space))
            _showUI = !_showUI;

        if (_cameraView == CameraView.FIRST || _cameraView == CameraView.THIRD)
        {
#if UNITY_5_0
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
#else
            Screen.lockCursor = true;
#endif
        }
        else
        {
#if UNITY_5_0
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#else
            Screen.lockCursor = false;
#endif
        }

        if (_missionTextDisplay != null && _missionTextDisplay._missionAccomplished)
            DoTransitionToWin();
    }

    /// <summary>
    /// In playing mode, toggle our player instructions on and off
    /// Draw the target reticule depeinding on camera view
    /// </summary>
    private void GuiPlaying()
    {
        if (!_showUI)
            GUI.Label(new Rect(5, 5, 300, 20), "Space - Show Instructions");
        else
        {
            GUI.Label(new Rect(5, 5, 300, 20), "Space - Hide Instructions");
            GUI.Label(new Rect(5, 25, 300, 20), "0 - Restart");
            GUI.Label(new Rect(5, 45, 300, 20), "V - Toggle FPS/3PS");
            GUI.Label(new Rect(5, 65, 300, 20), "W,A,S,D - Movement");
            GUI.Label(new Rect(5, 85, 300, 20), "1 - Squad Form Up");
            GUI.Label(new Rect(5, 105, 300, 20), "2 - Squad Attack");
            GUI.Label(new Rect(5, 125, 300, 20), "3 - Squad Cover Me");
            GUI.Label(new Rect(5, 145, 300, 20), "4 - Squad Take Cover");
            GUI.Label(new Rect(5, 165, 300, 20), "5 - Squad Flank");
            GUI.Label(new Rect(5, 185, 300, 20), "F1 - Wedge Formation");
            GUI.Label(new Rect(5, 205, 300, 20), "F2 - Column Formation");
            GUI.Label(new Rect(5, 225, 300, 20), "F3 - Skirmisher Left Formation");
            GUI.Label(new Rect(5, 245, 300, 20), "F4 - Skirmisher Right Formation");
            GUI.Label(new Rect(5, 265, 300, 20), "F5 - Echelon Left Formation");
            GUI.Label(new Rect(5, 285, 300, 20), "F6 - Echelon Right Formation");
        }

        if (_cameraView == CameraView.FIRST || _cameraView == CameraView.THIRD)
        {
            Rect tReticle = new Rect((Screen.width - _targetReticle.width) / 2f,
                                     (Screen.height - _targetReticle.height) / 2f,
                                     _targetReticle.width,
                                     _targetReticle.height);
            GUI.DrawTexture(tReticle, _targetReticle);
        }
    }

    private void DoTransitionToWin()
    {
        _gameMode = GameMode.WIN;

        if (_missionTextDisplay != null)
        {
            AudioSource tTextAudio = _missionTextDisplay.GetComponent<AudioSource>();
            if (tTextAudio != null)
                tTextAudio.Play();
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    private void DoWin()
    {
        if (Input.GetKeyDown("0"))
            _restart = true;
    }

    /// <summary>
    /// TBD
    /// </summary>
    private void GuiWin()
    {
        GUI.Label(new Rect(5, 5, 300, 20), "0 - Restart");
    }

    /// <summary>
    /// TBD
    /// </summary>
    private void DoDead()
    {
        if (Input.GetKeyDown("0"))
            _restart = true;
    }

    /// <summary>
    /// TBD
    /// </summary>
    private void GuiDead()
    {
        GUI.Label(new Rect(5, 5, 300, 20), "0 - Restart");
    }

    /// <summary>
    /// Cycle the camera view on the player.  Set up defaults for follow cam placement depending on the view.
    /// </summary>
    private void DoSwitchCameraView()
    {
        _cameraView = (CameraView)((((int)_cameraView) + 1) % ((int)CameraView.COUNT));

        switch (_cameraView)
        {
            case CameraView.FIRST:
                _followScript.distance = 0.1f;
                _followScript.height = 0f;
                _followScript.heightDamping = 100f;
                _followScript.rotationDamping = 100f;
                _followScript.target = _fpsCameraMount.transform;
                _followScript.lookAt = _fpsAim.transform;

                _playerRender.materials = new Material[] { };
                break;

            case CameraView.THIRD:
                _followScript.distance = 0.1f;
                _followScript.height = 0f;
                _followScript.heightDamping = 100f;
                _followScript.rotationDamping = 100f;
                _followScript.target = _tpsCameraMount.transform;
                _followScript.lookAt = _tpsAim.transform;

                _playerRender.materials = _playerMaterials;
                break;
        }
    }
}
