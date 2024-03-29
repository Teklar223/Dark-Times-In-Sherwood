using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
public class PhysicsObject2D : MonoBehaviour
{
    // source - https://www.youtube.com/watch?v=wGI2e3Dzk_w&list=PLX2vGYjWbI0SUWwVPCERK88Qw8hpjEGd8&index=1&ab_channel=Unity
    public LayerMask WhatIsGround => _currFilter.layerMask;
    protected const float _minMoveDistance = 0.001f;
    protected const float _shellRadius = 0.01f;
    [Header("Base Physics")]
    [SerializeField] protected private float _gravityModifier = 1f;
    [SerializeField] protected private float _minGroundNormalY = 0.1f;
    [SerializeField] protected private LayerMask _whatIsGround;
    protected private ContactFilter2D _currFilter;
    protected Collider2D _collider;
    protected bool _grounded = false;
    protected bool _onSlope = false;
    protected Rigidbody2D _rb2d;
    protected Vector2 _velocity;
    protected private Vector2 _targetVelocity;
    //public Vector2 TargetVelocity{get{return _targetVelocity;}private set{_targetVelocity = value;}}
    protected Vector2 _groundNormal;
    protected RaycastHit2D[] _hitBuffer = new RaycastHit2D[16]; // todo - variable length?
    protected List<RaycastHit2D> _hitBufferList = new(16);
    protected virtual void Start()
    {
        _collider = GetComponent<Collider2D>();
    }
    protected virtual void OnEnable() {
        _rb2d = GetComponent<Rigidbody2D>();
        _currFilter.useTriggers = false;
        _currFilter.SetLayerMask(_whatIsGround);
        _currFilter.useLayerMask = true;
    }
    protected private virtual void Update()
    {
        _targetVelocity = Vector2.zero; // critical?
    }

    protected private virtual void FixedUpdate()
    {
        _velocity += Time.deltaTime * _gravityModifier * Physics2D.gravity; // apply gravity to the objects velocity
        _velocity.x = _targetVelocity.x;
        _grounded = false;
        _onSlope = false;
        Vector2 deltaPosition = _velocity * Time.deltaTime;
        Vector2 moveAlongGround = new(_groundNormal.y, -_groundNormal.x); //helps with slopes  

        Vector2 moveX = moveAlongGround * deltaPosition;
        Vector2 moveY = Vector2.up * deltaPosition.y;
        
        Movement(moveY, true); // vertical movement
        Movement(moveX, false); // horizontal movement
    }

    protected private virtual void Movement(Vector2 move, bool yMovement)
    {
        float distance = move.magnitude;

        if (distance > _minMoveDistance)
        {
            int count = _rb2d.Cast(move, _currFilter, _hitBuffer, distance + _shellRadius); // stores results into _hitBuffer and returns its length (can be discarded).
            _hitBufferList.Clear();
            for(int i = 0; i < count; ++i) // DO NOT Refactor this with foreach! it will iterate over empty spaces.
            {
                _hitBufferList.Add(_hitBuffer[i]);
            }
            foreach(var hit in _hitBufferList)
            {
                Vector2 currentNormal = hit.normal;
                if(currentNormal.y > _minGroundNormalY) // if the normal vectors angle is greater then the set value.
                {
                    _grounded = true;
                    _onSlope = currentNormal.y > 0 && currentNormal.y < 1;
                    if(yMovement)
                    {
                        _groundNormal = currentNormal;
                        currentNormal.x = 0;
                    }
                }
                float projection = Vector2.Dot(_velocity, currentNormal); // differnece between velocity and currentNormal to know how much to subtract if the player collides with a wall/ceiling
                if(projection < 0) 
                {
                    _velocity -= projection * currentNormal; // cancel out the velocity that would be lost on impact.
                }

                float modifiedDistance = hit.distance - _shellRadius; 
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }
        }
        //Debug.Log($"distance = {distance} |postion +={move.normalized * distance}");
        _rb2d.position += move.normalized * distance;
        // DEBUG:
        var _color = Color.white;
        if(!_grounded)
        {
            _color = Color.magenta;
            //Debug.Log($"y movement = {yMovement} && grounde == {_grounded}!");
        }
        var direction = _rb2d.velocity.x >= 0f ? Vector2.right : Vector2.left;
        Debug.DrawRay(transform.position,direction,_color,2.5f);
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.yellow;
        Vector2 moveAlongGround = new(_groundNormal.y, -_groundNormal.x);
        Gizmos.DrawRay(transform.position,moveAlongGround);

    }
}
