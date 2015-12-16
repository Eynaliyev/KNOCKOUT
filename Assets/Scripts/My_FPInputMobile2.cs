//Durovis Dive Head Tracking 
//copyright by Shoogee GmbH & Co. KG Refer to LICENCE.txt 
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class My_FPInputMobile2 : My_FPInput
 {

	//if true, rotation of a GameObject will be added
	//for example tilting the camera while going along a racetrack or rollercoaster
	public bool AddRotationGameobject = false;
	//e.g.: the head of the player, or a waggon of a rollercoaster
	public GameObject RotationGameobject;
	//Texture to display when no gyro is found
	public Texture NoGyroTexture;
	//if true, yaw will be rotated to 0 of a scene (e.g. when you load another scene)
	public bool correctCenterTransition = false;
	//script to learn about a device's default orientation, needed for axis correction on some tablets
	public NaturalOrientation no;

	private bool mbShowErrorMessage, mbUseGyro;
	private float q0, q1, q2, q3;
	private float m0, m1, m2;

	private Quaternion rot;
	private Quaternion centerTransition = Quaternion.identity;

	private int is_tablet;

    private bool gyroBool;
    private Gyroscope gyro;
    private Quaternion rotFix;
    private Vector3 initial = new Vector3(90, 180, 0);
    vp_FPPlayerEventHandler player;



    //input mobile variables

	protected vp_FPCamera m_FPCamera = null;
	public vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = transform.root.GetComponentInChildren<vp_FPCamera>();
			return m_FPCamera;
		}
	}


	/// <summary>
	/// 
	/// </summary>


	/// <summary>
	/// Handles interaction with the game world
	/// </summary>
	protected override void InputInteract()
	{
	
		if(!FPPlayer.CanInteract.Get())
			return;
			
		bool interact = vp_GlobalEventReturn<bool>.Send("SimulateTouchWithMouse") ? vp_Input.GetButtonAny("Interact") : vp_Input.GetButtonUp("Interact");
		
		if(interact)
			FPPlayer.Interact.TryStart();
		else
			FPPlayer.Interact.TryStop();
	
	}
	
	
	/// <summary>
	/// broadcasts a message to any listening components telling
	/// them to go into 'attack' mode. vp_FPShooter uses this to
	/// repeatedly fire the current weapon while the fire button
	/// is being pressed, but it could also be used by, for example,
	/// an animation script to make the player model loop an 'attack
	/// stance' animation.
	/// </summary>
	protected override void InputAttack()
	{

		// TIP: uncomment this to prevent player from attacking while running
		//if (FPPlayer.Run.Active)
		//	return;

		// if mouse cursor is visible, an extra click is needed
		// before we can attack
		
		if(FPPlayer.Reload.Active)
			return;
		
		if (vp_Input.GetButtonAny("Attack"))
			FPPlayer.Attack.TryStart();
		else
			FPPlayer.Attack.TryStop();
			
	}
	
	
	/// <summary>
	/// ask controller to jump when button is pressed (the current
	/// controller preset determines jump force).
	/// NOTE: if its 'MotorJumpForceHold' is non-zero, this
	/// also makes the controller accumulate jump force until
	/// button release.
	/// </summary>
	protected override void InputJump()
	{

		// TIP: to find out what determines if 'Jump.TryStart'
		// succeeds and where it is hooked up, search the project
		// for 'CanStart_Jump'

		if (vp_Input.GetButtonAny("Jump"))
			FPPlayer.Jump.TryStart();
		else
			FPPlayer.Jump.Stop();

	}
	
	
	/// <summary>
	/// when the reload button is pressed, broadcasts a message
	/// to any listening components asking them to reload
	/// NOTE: reload may not succeed due to ammo status etc.
	/// </summary>
	protected override void InputReload()
	{

		if (vp_Input.GetButtonAny("Reload"))
			FPPlayer.Reload.TryStart();
		
	}
	
	
	/// <summary>
	/// disallow zoom if these conditions are met
	/// </summary>
	protected virtual bool CanStart_Zoom()
	{
	
		if(!FPPlayer.CurrentWeaponWielded.Get())
			return false;
			
		if(FPPlayer.Reload.Active)
			return false;
		
		// we can only zoom with weapons that carry ammo
		if(FPPlayer.CurrentWeaponMaxAmmoCount.Get() == 0)
			return false;
			
		return true;
	
	}
	
	
	/// <summary>
	/// stop zooming if reload starts
	/// </summary>
	protected virtual void OnStart_Reload()
	{
	
		FPPlayer.Zoom.Stop();
	
	}
	
	
	/// <summary>
	/// zoom in using the zoom modifier key(s)
	/// </summary>
	protected override void InputCamera()
	{
	
		bool zoom = vp_GlobalEventReturn<bool>.Send("SimulateTouchWithMouse") ? vp_Input.GetButtonAny("Zoom") : vp_Input.GetButtonDown("Zoom");
		
		if(!zoom)
			return;
			
		if(Time.time < FPPlayer.Zoom.NextAllowedStartTime)
			return;
		
		if(FPPlayer.Zoom.Active)
			FPPlayer.Zoom.TryStop();
		else
			FPPlayer.Zoom.TryStart();

	}
	
	
	/// <summary>
	/// tell the player to enable or disable the 'Run' state.
	/// NOTE: since running is a state, it's not sent to the
	/// controller code (which doesn't know the state names).
	/// instead, the player class is responsible for feeding the
	/// 'Run' state to every affected component.
	/// </summary>
	protected override void InputRun()
	{

		if (vp_Input.GetButtonAny("Run"))
		{
			if(vp_GlobalEventReturn<bool>.Send("SimulateTouchWithMouse"))
				FPPlayer.InputMoveVector.Set(new Vector2(0, 1));
			else
				FPPlayer.InputMoveVector.Set(new Vector2(vp_Input.GetAxisRaw("Horizontal"), vp_Input.GetAxisRaw("Vertical")));
			FPPlayer.Run.TryStart();
		}
		else FPPlayer.Run.TryStop();

	}
	
	
	/// <summary>
	/// toggles the game's pause state on / off
	/// </summary>
	protected override void UpdatePause()
	{

		if(vp_Input.GetButtonAny("Pause"))
			FPPlayer.Pause.Set(!FPPlayer.Pause.Get());

	}
	
	
	/// <summary>
	/// this method handles toggling between mouse pointer and
	/// firing modes. it can be used to deal with screen regions
	/// for button menus, inventory panels et cetera.
	/// NOTE: if your game supports multiple screen resolutions,
	/// make sure your 'MouseCursorZones' are always adapted to
	/// the current resolution. see 'vp_FPSDemo1.Start' for one
	/// example of how to this
	/// </summary>
	protected override void UpdateCursorLock()
	{
	
		if(vp_GlobalEventReturn<bool>.Send("SimulateTouchWithMouse"))
			return;

		// store the current mouse position as GUI coordinates
		m_MousePos.x = Input.mousePosition.x;
		m_MousePos.y = (Screen.height - Input.mousePosition.y);

		// uncomment this line to print the current mouse position
		//Debug.Log("X: " + (int)m_MousePos.x + ", Y:" + (int)m_MousePos.y);

		// if 'ForceCursor' is active, the cursor will always be visible
		// across the whole screen and firing will be disabled
		if (MouseCursorForced)
		{
			Screen.lockCursor = false;
			return;
		}

		// see if any of the mouse buttons are being held down
		if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
		{

			// if we have defined mouse cursor zones, check to see if the
			// mouse cursor is inside any of them
			if (MouseCursorZones.Length > 0)
			{
				foreach (Rect r in MouseCursorZones)
				{
					if (r.Contains(m_MousePos))
					{
						// mouse is being held down inside a mouse cursor zone, so make
						// sure the cursor is not locked and don't lock it this frame
						Screen.lockCursor = false;
						goto DontLock;
					}
				}
			}

			// no zones prevent firing the current weapon. hide mouse cursor
			// and lock it at the center of the screen
			Screen.lockCursor = true;

		}

	DontLock:

		// if user presses 'ENTER', toggle mouse cursor on / off
		if (vp_Input.GetButtonUp("Accept1") || vp_Input.GetButtonUp("Accept2"))
			Screen.lockCursor = !Screen.lockCursor;

	}




#if UNITY_EDITOR
#elif UNITY_ANDROID
	private static AndroidJavaObject javadiveplugininstance;

	[DllImport("divesensor")]
	private static extern int dive_set_path(string path);

	[DllImport("divesensor")]
	private static extern void initialize_sensors();

	[DllImport("divesensor")]
	private static extern int get_q(ref float q0, ref float q1, ref float q2, ref float q3);

	[DllImport("divesensor")]
	private static extern int process();

	[DllImport("divesensor")]
	private static extern void set_application_name(string name);

	[DllImport("divesensor")]
	private static extern int get_m(ref float m0,ref float m1,ref float m2);

	[DllImport("divesensor")]
	private static extern void use_udp(int switchon);

	[DllImport("divesensor")]
	private static extern void get_version(string msg, int maxlength);

	[DllImport("divesensor")]
	private static extern int get_error();
	
	[DllImport("divesensor")]   
	private static extern void dive_command(string command);

#elif UNITY_IPHONE
	[DllImport("__Internal")]
	private static extern void initialize_sensors();

	[DllImport("__Internal")]
	private static extern void stop_sensors();

	[DllImport("__Internal")]	
	private static extern float get_q0();

	[DllImport("__Internal")]	
	private static extern float get_q1();

	[DllImport("__Internal")]	
	private static extern float get_q2();

	[DllImport("__Internal")]	
	private static extern float get_q3();

	[DllImport("__Internal")]	
	private static extern void DiveUpdateGyroData();

	[DllImport("__Internal")]	
	private static extern int get_q(ref float q0,ref float q1,ref float q2,ref float q3);

	[DllImport("__Internal")]	
	private static extern int get_m(ref float m0,ref float m1,ref float m2);
#endif
	void Start()
	{
		/// mobile input stuff

		FPCamera.SetRotation(FPCamera.Transform.eulerAngles, false, true);
		FPPlayer.Zoom.MinPause = .25f;
		FPPlayer.Zoom.MinDuration = .25f;
		
		MouseCursorBlocksMouseLook = false;

		////////////-durovis stuff
		mbShowErrorMessage = true;
		mbUseGyro = false;

		//load some settings from PlayerPrefs
		DInput2.load ();

		rot = Quaternion.identity;

		//Disable screen dimming
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		//set Frame Rate hint to 60 FPS
		Application.targetFrameRate = 60;
#if UNITY_EDITOR
#elif UNITY_ANDROID
		// Java part
		DiveJava.init ();
		dive_set_path(Application.persistentDataPath);
		Network.logLevel = NetworkLogLevel.Full;
		use_udp(1);
		initialize_sensors();
		int err = get_error();
		if(err == 0)
		{
			mbShowErrorMessage = false;
			mbUseGyro = true;
			if(correctCenterTransition){
				get_q(ref q0, ref q1, ref q2, ref q3);
				rot.x = -q2;
				rot.y = q3;
				rot.z = -q1;
				rot.w = q0;
				Quaternion temp = Quaternion.identity;
				temp.eulerAngles = new Vector3(0,rot.eulerAngles.y,0);
				this.centerTransition = Quaternion.identity * Quaternion.Inverse(temp);
			}
	
			if (no.GetDeviceDefaultOrientation() == NaturalOrientation.LANDSCAPE){
				is_tablet=1;
				Debug.Log("Dive Unity Tablet Mode activated");
			}
			else{
				Debug.Log("Dive Phone Mode activated");
			}
		}
		else
		{
			mbShowErrorMessage = true;
			mbUseGyro = false;
		}
#elif UNITY_IPHONE
		initialize_sensors();
		mbShowErrorMessage = false;
		mbUseGyro = true;
#endif
	}

	
	void Update()
	{
#if UNITY_EDITOR
#elif UNITY_ANDROID
		process();
		get_q(ref q0, ref q1, ref q2, ref q3);
		rot.x = -q2;
		rot.y = q3;
		rot.z = -q1;
		rot.w = q0;

#elif UNITY_IPHONE
		DiveUpdateGyroData();
		get_q(ref q0,ref q1,ref q2,ref q3);
		rot.x=-q2;
		rot.y=q3;
		rot.z=-q1;
		rot.w=q0;

		get_m(ref m0,ref m1,ref m2);
#endif

		if(mbUseGyro)
		if(Time.timeSinceLevelLoad > 0.1f)
			if(correctCenterTransition)
			{
				if(AddRotationGameobject){
					if (is_tablet==1){
						transform.rotation = RotationGameobject.transform.rotation * (centerTransition * rot)* Quaternion.AngleAxis(90,Vector3.forward);
					}else{
						transform.rotation = RotationGameobject.transform.rotation * (centerTransition * rot);
					}
				}else{
					if (is_tablet==1){
						transform.rotation = centerTransition * rot * Quaternion.AngleAxis(90,Vector3.forward);
					}else{
						transform.rotation = centerTransition * rot;
					}
				}
			}
		else
		{
			if(AddRotationGameobject)
			if (is_tablet==1){
				transform.rotation= RotationGameobject.transform.rotation * rot * Quaternion.AngleAxis(90,Vector3.forward);
			} else transform.rotation = RotationGameobject.transform.rotation * rot;
			else if (is_tablet==1){
				transform.rotation= rot * Quaternion.AngleAxis(90,Vector3.forward);
			} else transform.rotation = rot;
		}


	}

	void OnGUI()
	{
		if(mbShowErrorMessage){
			if(GUI.Button(new Rect(0, 0, Screen.width, Screen.height), "button"))
				mbShowErrorMessage = false;

			if(NoGyroTexture != null){
				int liHeight = (int)(Screen.height * 0.9);
				GUI.DrawTexture(new Rect((Screen.width - liHeight) / 2, (Screen.height - liHeight) / 2, liHeight, liHeight), NoGyroTexture, ScaleMode.StretchToFill, true, 0);
			}
		}
	}

	void OnApplicationQuit(){
#if UNITY_EDITOR
#elif UNITY_IOS
		stop_sensors();
#endif
	}

}

public static class DInput2
{
	public static bool use_cardboard_trigger = false;
	public static bool use_analog_value = false;
	public static bool invert = false;
	public static bool use_IPD_Correction;
	public static float IPDCorrectionValue = 0;
	
	public static void save()
	{
		PlayerPrefs.SetInt("dive_use_cardboard_trigger", (use_cardboard_trigger ? 1 : 0));
		PlayerPrefs.SetInt("dive_use_cardboard_analog_value", (use_analog_value ? 1 : 0));
		PlayerPrefs.SetInt("dive_invert_axis", (invert ? 1 : 0));
		PlayerPrefs.SetInt("dive_use_ipd_correction", (use_IPD_Correction ? 1 : 0));
		PlayerPrefs.SetFloat("dive_ipd_correction_value", IPDCorrectionValue);
	}
	
	public static void load()
	{
		use_cardboard_trigger = (PlayerPrefs.GetInt("dive_use_cardboard_trigger") != 0);
		use_analog_value = (PlayerPrefs.GetInt("dive_use_cardboard_analog_value") != 0);
		invert = (PlayerPrefs.GetInt("dive_invert_axis") != 0);
		use_IPD_Correction = (PlayerPrefs.GetInt("dive_use_ipd_correction") != 0);
		IPDCorrectionValue = (PlayerPrefs.GetFloat("dive_ipd_correction_value"));
	}
}
