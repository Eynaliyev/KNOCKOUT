using UnityEngine;
using System.Collections;

public class FadeToBlack : MonoBehaviour 
{
    public bool fadeToReset = false;
    public float fadeTime = 5f;
    public Texture blackTexture;
    private float alphaFadeValue = 0f;

	// Update is called once per frame
	void OnGUI () 
    {
        if (fadeToReset)
        {
            alphaFadeValue += Mathf.Clamp01(Time.deltaTime / fadeTime);

            GUI.color = new Color(0, 0, 0, alphaFadeValue);

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
            if (alphaFadeValue >= 1f)
                Application.LoadLevel(Application.loadedLevel);
        }
    }
}
