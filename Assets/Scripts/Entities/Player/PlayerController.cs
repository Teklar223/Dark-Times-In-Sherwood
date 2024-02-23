using System;
using System.Collections;
using UnityEngine;

namespace DTIS
{
    /*
        Responsible for all 'basic' aspects of entity management in unity like position and physics, acting as a template class for other classes to override and specialize.
        Important reference:
        - https://stackoverflow.com/questions/12662072/what-is-protected-virtual-new
    */
    public class PlayerController : PhysicsObject2D
    {
        private static PlayerController _Instance;
        public static PlayerController Instance
        {
            get
            {
                if (!_Instance)
                {
                    _Instance.name = _Instance.GetType().ToString(); // name it for easy recognition
                    DontDestroyOnLoad(_Instance.gameObject); // mark root as DontDestroyOnLoad();
                }
                return _Instance;
            }
        }

        [Header("Player Physics")]
        /*** WALK & RUN ***/
        [SerializeField] private float _walkSpeed;
        public float WalkSpeed{get{return _walkSpeed;}}
        [SerializeField] private float _runSpeedMult;
        public float RunSpeedMult{get{return _runSpeedMult;}}
        public Vector2 Velocity{get{return _velocity;}set{_velocity = value;}}
        
        /*** SLOPES ***/
        private bool _wasOnSlopePrevFrame = false;

        /*** JUMP & FALL ***/
        [SerializeField] private Transform _JumpHeight;
        [SerializeField] private Transform _JumpHorizontalMove;
        [SerializeField, Range(0f,1f)] private float _gravityMultAtPeak = 0.25f;
        [SerializeField] private float _jumpPeakHangThreshold;
        [SerializeField] private float _fallGravityMult = 1.5f;
        [SerializeField] private float _weakJumpGravityMult = 2f;
        private bool _isJumping = false;
        private bool _isFalling = false;
        private float _jumpForce;
        private float _timeToJumpPeak;
        private float _jumpGravity;
        private Vector2 _baseGravity;
        private Vector2 _currGravity;
        private Vector2 _fallGravity;
        private bool _isInPeakHang;
        public Vector2 CurrGravity{get{return _currGravity;}set{_currGravity = value;}}
        public float JumpPeakHangThreshold{get{return _jumpPeakHangThreshold;}}
        public float JumpPeakGravityMult{get{return _gravityMultAtPeak;}}
        public Vector2 FallGravity {get{return _fallGravity;} set{_fallGravity = value;}}
        public bool IsJumping { get{return _isJumping;}set{_isJumping = value;}}
        public bool IsFalling { get{return _isFalling;}set{_isFalling = value;}}
        public float JumpForce{get{return _jumpForce;}set{_jumpForce = value;}}
        public bool IsInPeakHang{get{return _isInPeakHang;}set{_isInPeakHang=value;}}

        [Header("Animation")]
        [SerializeField][Range(0,1)] private float _playbackSpeed = 1f;
        [Header("Player Attributes")]
        public int _jumpStaminaCost;
        public int _ghostedSanityCost;

        [Header("Environmentals Checkers")]
        // [SerializeField] private bool _airControl = true;
        [SerializeField] private Collider2D m_CrouchDisableCollider;                // A collider to be disabled on the 'crouch' player action.
        //[SerializeField] private Transform _ceilingCheck;                           // A position marking where to check for ceilings

        [Header("Shooting")]
        [SerializeField] private float ShootDelaySeconds;
        [SerializeField] private float ShootReloadSeconds;

        //Player related vars//
        private bool _facingRight = true;                         // A boolean marking the entity's orientation.
        public bool FacingRight { get { return _facingRight; } private set { _facingRight = value; } }

        /* *** ANIMATOR *** */
        private Animator _animator;
        public Animator Animator { get { return _animator; } }
        
        /* *** PARTICLES *** */
        private TrailRenderer _tr;

        /* *** FSM *** */
        private PlayerStateMachine _fsm;
        public PlayerStateMachine FSM { get { return _fsm; } internal set { _fsm = value; } } // TODO: refactor to remove this it makes no sense.

        /* *** GROUND CHECK*** */
        [SerializeField] private GroundCheck _gc;
        public bool IsGrounded { get { return _gc.Grounded(); } }

        /* *** STAMINA *** */
        private StaminaBar _staminabar;
        public StaminaBar StaminaBar { get { return _staminabar; } }

        /* *** SANITY *** */
        private SanityBar _sanityBar;
        public SanityBar SanityBar { get { return _sanityBar; } }

        private HpBarPlayer _hpBar;
        public HpBarPlayer HpBar { get { return _hpBar; } }

        /* *** GHOST MECHANIC *** */
        private PlayerGhostBehaviour _playerGhostBehaviour;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        /* *** SHOOTING */
        private ClickSpawn _clickSpawn; // class to spawn object by click.
        private bool isShooting = false;

        /* *** CAMERA*** */
        private Camera _mainCamera;

        /* *** PLATFORMS *** */
        private bool _passingThroughPlatform = false;
        private LayerMask _initialGroundLayerMask;
        public bool PassingThroughPlatform{get{return _passingThroughPlatform;}private set{_passingThroughPlatform=value;}}

        public void SetPassingThroughPlatform(bool value)
        {
            if(value)
            {
                _contactFilter2d.SetLayerMask(_groundOnlyFilter.layerMask);
            }
            else
            {
                _contactFilter2d.SetLayerMask(_initialGroundLayerMask);
            }
            PassingThroughPlatform = true;
        }

        /* *** DASH *** */
        [Header("Dash Settings")]
        private bool _canDash = true;
        private bool _isDashing = false;
        [SerializeField, Range(0.001f, 5)] private float _dashDurationSeconds = 0.2f;
        [SerializeField] private Transform _dashLengthRef;
        private float _dashDistance = 5f;
        [SerializeField] private float _dashCooldown = 1f;
        //[SerializeField] private int _ConsecutiveDashes = 2;
        //[SerializeField] private float _ConsecutiveDashTimeframe = 0.5f;

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _tr = GetComponent<TrailRenderer>();
            _clickSpawn = GameObject.FindGameObjectWithTag("AttackPosRef").GetComponent<ClickSpawn>(); // TODO: fix magic strings
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            _gc = GetComponentInChildren<GroundCheck>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _staminabar = GetComponent<StaminaBar>();
            _sanityBar = GetComponent<SanityBar>();
            _hpBar = GetComponent<HpBarPlayer>();
            _playerGhostBehaviour = new PlayerGhostBehaviour(_spriteRenderer, _sanityBar, _ghostedSanityCost);
        }
        void Start()
        {
            _isInPeakHang = false;
            _baseGravity = Physics2D.gravity;
            _currGravity = _baseGravity;
            _fallGravity = _baseGravity * _fallGravityMult;
            var jumpHeight = Vector2.Distance(transform.position,_JumpHeight.position); // h
            var jumpHorizontalMove = Vector2.Distance(transform.position,_JumpHeight.position); // X_h
            var direction = _facingRight == true ? 1.0f:-1.0f;
            var Vx = direction * _walkSpeed;
            var Th = jumpHorizontalMove / Vx;
            _timeToJumpPeak = Th;
            _jumpForce = 2*jumpHeight / Th ;
            _jumpGravity = -2*jumpHeight / Th; /* Th);*/
            Debug.Log($"initial jump force = {_jumpForce} | jump gravity = {_jumpGravity}");
            _initialGroundLayerMask = _contactFilter2d.layerMask;
            _animator.speed = _playbackSpeed;
            if(_dashLengthRef != null) 
            {
                _dashDistance = Vector2.Distance(transform.position,_dashLengthRef.transform.position);
            }
        }
        protected private override void Update()
        {
            base.Update();
            _playerGhostBehaviour.TrySetGhostStatus();
            Flip();
        }

        /*Flips the chacater according to his velocity*/
        protected virtual void Flip(bool overrideMovement = false)
        {
            if (_velocity.x != 0) // if not idle
            {
                bool movingRight = _velocity.x > 0;
                bool movingLeft = _velocity.x < 0;
                if (FacingRight && movingLeft)
                {
                    FacingRight = !FacingRight;
                    _spriteRenderer.flipX = true; // flip to face Left
                }
                if (!FacingRight && movingRight)
                {
                    FacingRight = !FacingRight;
                    _spriteRenderer.flipX = false; // flip to face Right
                }
            }
        }
        private void FlipByCursorPos()
        {
            Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            bool isMouseRightToPlayer = mouseWorldPosition.x > transform.position.x;
            bool isMouseLeftToPlayer = mouseWorldPosition.x < transform.position.x;
            if (FacingRight && isMouseLeftToPlayer)
            {
                FacingRight = !FacingRight;
                _spriteRenderer.flipX = true; // flip to face Left
            }
            if (!FacingRight && isMouseRightToPlayer)
            {
                FacingRight = !FacingRight;
                _spriteRenderer.flipX = false; // flip to face Right
            }
        }

        public virtual void Ghost()
        {
            GameManager.IsPlayerGhosted = !GameManager.IsPlayerGhosted;
        }
        public virtual void Shoot()
        {
            if (isShooting) return;
            {
                isShooting = true;
                StartCoroutine(DelayArrow());

                IEnumerator DelayArrow() // delays the user from shooting every 'ShootDelay' seconds.
                {
                    //Debug.Log("Arrow is loading...");
                    yield return new WaitForSeconds(ShootDelaySeconds);
                    _clickSpawn.spawnObject();
                    isShooting = false;
                }
            }
        }
        //this method should check if a certain animation is still playing (like shooting, if so DO NOT SHOOT)
        public bool isPlaying(string stateName)
        {
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
                    _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
                return true;
            else
                return false;
        }
        public void Move(Vector2 move)
        {
            _targetVelocity = move * _walkSpeed;
        }
        /// <summary>
        /// Sets _isJumping=true and gravity to jumping gravity.
        /// </summary> <summary>
        /// 
        /// </summary>
        public void Jump()
        {
            _isJumping = true;
            CurrGravity = new(0f,_jumpGravity);
            _velocity.y = _jumpForce;
        }
        internal void AccelarateFall()
        {
            CurrGravity = new(0f,_jumpGravity*_fallGravityMult);
            //_velocity += (_fallGravityMult - 1) * Time.deltaTime * CurrGravity * Vector2.up; //TODO: clamp to some max value.
        }
        public void Dash()
        {
            if(_canDash)
            {
                var direction = _facingRight == true ? Vector2.right:Vector2.left;
                var hit = Physics2D.Raycast(transform.position,direction,_dashDistance,_contactFilter2d.layerMask);
                var distance = Vector2.Distance(transform.position,hit.point);
                if(distance < _dashDistance)
                {
                    Debug.Log($"hit dist = {distance}");
                    _dashDistance = distance;
                }
                StartCoroutine(StartDash(_gravityModifier));
            }
        }
        private IEnumerator StartDash(float OriginalGravityModifier)
        {
            _canDash = false;
            _isDashing = true;
            _tr.emitting = true;
            yield return new WaitForSeconds(_dashDurationSeconds);
            _tr.emitting = false;
            _gravityModifier = OriginalGravityModifier;
            _isDashing = false;
            yield return new WaitForSeconds(_dashCooldown);
            _canDash = true;
            //_velocity.x = 0f;
        }
        protected private override void FixedUpdate()
        {
            if(_isDashing)
            {
                _velocity = new(0f,0f);
                var direction = _facingRight ? 1.0f : -1.0f;
                var velocity = _dashDistance / _dashDurationSeconds;// S = V * T --> S/T = V
                _gravityModifier = 0f; //re applied by a corutine!
                _targetVelocity = new(direction * velocity,0f);
            }

            _wasOnSlopePrevFrame = _onSlope;
            _grounded = false;
            _onSlope = false;
            var acc = Time.deltaTime * _gravityModifier * CurrGravity;

            _velocity += acc; // apply gravity to the objects velocity
            _velocity.x = _targetVelocity.x;
            Vector2 deltaPosition = _velocity * Time.deltaTime;
            Vector2 moveAlongGround = new(_groundNormal.y, -_groundNormal.x); //helps with slopes  

            Vector2 moveX = moveAlongGround * deltaPosition;
            Vector2 moveY = Vector2.up * deltaPosition.y;
            
            Movement(moveY, true); // vertical movement
            Movement(moveX, false); // horizontal movement

            // predict future position using a simplified euler integration (~0.5 pixel error rate, resets when landing so it does not accumulate)
            var futurePos = _velocity * Time.deltaTime + 0.5f * Time.deltaTime * acc; // pos = velocity*deltaTime +0.5*accelaration*(deltaTime^2)
            var futureVel = acc; // vel = accelaration * deltaTime
            futurePos = (Vector2)transform.position+futurePos; //TODO:
            //Debug.Log($"future position = {futurePos} | future velocity = {futureVel}");
        }

        protected private override void Movement(Vector2 move, bool yMovement)
        {
            float distance = move.magnitude;
            bool setOnce = false;

            if (distance > _minMoveDistance)
            {
                int count = _rb2d.Cast(move, _contactFilter2d, _hitBuffer, distance + _shellRadius); // stores results into _hitBuffer and returns its length (can be discarded).
                _hitBufferList.Clear();
                float collisionDist;
                Vector2 closestCollision;
                if(count > 0)
                {
                    collisionDist = _hitBuffer[0].distance;
                    closestCollision = _rb2d.ClosestPoint(_hitBuffer[0].point);
                    _hitBufferList.Add(_hitBuffer[0]);
                }
                foreach(var hit in _hitBufferList)
                {
                    Vector2 currentNormal = hit.normal;
                    if(currentNormal.y > _minGroundNormalY) // if the normal vectors angle is greater then the set value.
                    {
                        if(!setOnce)
                        {
                            _grounded = true;
                            _onSlope = currentNormal.y > 0 && currentNormal.y < 1;
                            setOnce = true;
                        }
                        if(yMovement)
                        {
                            _groundNormal = currentNormal;
                            if(!_onSlope)
                                currentNormal.x = 0;
                        }
                    }
                    float projection = Vector2.Dot(_velocity,currentNormal);
                    if(projection < 0 ) 
                    {
                        //Debug.Log($"vel before = {_velocity} | ymove = {yMovement} | current normal = {currentNormal} || projection = {projection}");
                        _velocity -= projection * currentNormal; // cancel out the velocity that would be lost on impact.
                        //Debug.Log($"new vel = {_velocity} | ymove = {yMovement} | move projection = {projection * currentNormal}");
                    }

                    float modifiedDistance = hit.distance - _shellRadius; 
                    distance = modifiedDistance < distance ? modifiedDistance : distance;
                }
            }
            _rb2d.position += move.normalized * distance;
            // DEBUG:
            var _color = Color.white;
            if(!_grounded)
            {
                _color = Color.magenta;
                //Debug.Log($"grounded = {_grounded} | on slope prev frame = {_wasOnSlopePrevFrame} | on slope = {_onSlope} | rb vel = {_rb2d.velocity} | y movement = {yMovement}");
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
}


