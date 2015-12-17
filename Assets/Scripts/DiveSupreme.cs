//Durovis Dive Head Tracking 
//copyright by Shoogee GmbH & Co. KG Refer to LICENCE.txt 
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class DiveSupreme : MonoBehaviour
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
	//event handler
	vp_FPPlayerEventHandler player;
	private Quaternion raw;



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
		//eventhandler
		player = GameObject.FindObjectOfType<vp_FPPlayerEventHandler>();

		mbShowErrorMessage = true;
		mbUseGyro = false;

		//load some settings from PlayerPrefs
		DInput.load ();

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
//disable this is mouselook is activated?
		if(mbUseGyro)
		if(Time.timeSinceLevelLoad > 0.1f)
			if(correctCenterTransition)
			{
				if(AddRotationGameobject){
					if (is_tablet==1){
						raw = RotationGameobject.transform.rotation * (centerTransition * rot)* Quaternion.AngleAxis(90,Vector3.forward);
						player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
					}else{
						raw = RotationGameobject.transform.rotation * (centerTransition * rot);
						player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
					}
				}else{
					if (is_tablet==1){
						raw = centerTransition * rot * Quaternion.AngleAxis(90,Vector3.forward);
						player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
					}else{
						raw = centerTransition * rot;
						player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
					}
				}
			}
		else
		{
			if(AddRotationGameobject)
			if (is_tablet==1){
				raw = RotationGameobject.transform.rotation * rot * Quaternion.AngleAxis(90,Vector3.forward);
				player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
			} else {
				raw = RotationGameobject.transform.rotation * rot;
				player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
			}
			else if (is_tablet==1){
				raw = rot * Quaternion.AngleAxis(90,Vector3.forward);
				player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
			} else {
				raw = rot;
				player.Rotation.Set(new Vector2 (raw.eulerAngles.x, raw.eulerAngles.y));
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

public static class DInput
{
	public static bool use_cardboard_trigger = true;
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
