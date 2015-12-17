using UnityEngine;
using System.Collections;
using RAIN.Navigation.Targets;

public class MissionTextDisplay : MonoBehaviour
{
    public Transform playerCamera;
    public Vector3 positionOffset;
    public bool _missionAccomplished = false;
    private TextMesh _mesh;
    private NavigationTarget _target;

    public void Start()
    {
        _mesh = GetComponent<TextMesh>();
        NavigationTargetRig tRig = transform.parent.GetComponent<NavigationTargetRig>();
        _target = tRig.Target;
        _missionAccomplished = false;
    }

    public void Update()
    {
        transform.position = _target.Position + positionOffset;
        float distance = Vector3.Distance(playerCamera.position, transform.position);

        _mesh.fontSize = (int) Mathf.Max(Mathf.Sqrt(distance) * 2f, 10f);
        if (_missionAccomplished || (distance < 5f))
        {
            _mesh.fontSize = 6;
            _mesh.text = "Mission\nAccomplished";
            _missionAccomplished = true;
        }
        else
        {
            _mesh.text = string.Format("{0:N0}m", distance);
        }

        Vector3 lookAt = playerCamera.position;
        lookAt.y = transform.position.y;
        transform.LookAt(lookAt);
    }
}