using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSync : MonoBehaviour
{

    [SerializeField] private Camera _cameraToSyncWith;
    public Camera CameraToSyncWith { get { return _cameraToSyncWith; } set { _cameraToSyncWith = value; } }

    private Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }
    void Update()
    {
        _camera.fieldOfView = _cameraToSyncWith.fieldOfView;
        _camera.transform.position = _cameraToSyncWith.transform.position;
        _camera.transform.rotation = _cameraToSyncWith.transform.rotation;
    }
}
