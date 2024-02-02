using System.Collections.Generic;
using DTIS;
using UnityEngine;
public class PlayerInteractor : MonoBehaviour
{
    //public Transform Referencepoint;
    private readonly IDictionary<int,Transform> _objectsInRange = new Dictionary<int,Transform>();
    [SerializeField] private PlayerController controller;
    private GameObject closestObject;
    private Vector3 _fixedPos;

    public void Interact()
    {
        if(closestObject != null)
        {
            closestObject.GetComponent<Interactable>().OnClick(controller.gameObject);
        }
    }
    void Start()
    {
        _fixedPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        SetClosestObject();
        MimicEntityMovement();
    }
    private void SetClosestObject()
    {
        if(closestObject != null)
                closestObject.GetComponent<Interactable>().SetGUI(false);
        if(_objectsInRange.Count == 0)
        {
            closestObject = null;
        }
        else
        {
            closestObject = Util.NearestNTransforms(_objectsInRange,transform.position)[0].gameObject; //returns array with one object.
            if(closestObject != null)
                closestObject.GetComponent<Interactable>().SetGUI(true);
        }
    }
    private void MimicEntityMovement()
    {
       transform.position = controller.transform.position + _fixedPos;
    }
    private void OnTriggerEnter2D(Collider2D other) {
        //Debug.Log(other.gameObject.name);
        if(other.CompareTag("Interactable"))
        {
            Debug.Log(other.gameObject.name);
            if(!_objectsInRange.ContainsKey(other.gameObject.GetHashCode()))
            {  
                _objectsInRange.Add(other.gameObject.GetHashCode(),other.transform);
                // NOTE - USING GetHashCode() AS A UNIQUE KEY IS BAD PRACTICE OUTSIDE OF UNITY!
                // it just happens to be unique in this engine.
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other) {
        if(other.CompareTag("Interactable"))
        {
            _objectsInRange.Remove(other.gameObject.GetHashCode());
        }
    }
}
   