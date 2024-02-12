using DTIS;
using UnityEngine;

public class TakedownInteractable : Interactable,IStealthable
{
    [SerializeField] private GameObject _goToTakedown;
    [SerializeField] private bool _KillsEntity = false;
    [SerializeField] private float _timeToTakedownSeconds = 0.025f;
    private GameObject _takeDownOrigin;
    public GameObject GO => _goToTakedown;

    public override void OnClick(GameObject clickingEntity)
    {
        if (clickingEntity.CompareTag("Player"))
        {
            _takeDownOrigin = clickingEntity;
            ((IStealthable)this).Takedown(_KillsEntity);
        }
    }

    void IStealthable.Takedown(bool kill)
    {
        if(kill)
        {
            throw new System.NotImplementedException();
        }
        else
        {
            var playerController = _takeDownOrigin.GetComponent<PlayerController>();
            var currPos = playerController.transform.position;
            var targetPos = GO.transform.position;
            var newPos = Vector2.Lerp(currPos,targetPos,0.5f);
            playerController.transform.position = newPos;
            StartCoroutine(Util.DestroyGameObjectCountdown(_goToTakedown,_timeToTakedownSeconds));
        }
    }

    public float FoV => throw new System.NotImplementedException();

    public float Awareness => throw new System.NotImplementedException();
}