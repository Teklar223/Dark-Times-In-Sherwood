using System;
using System.Collections;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace DTIS
{
    public class JumpState : PlayerState
    {
        private const string ClimbLedgeAnimName = "crnr-clmb";
        private CeilingCheck CeilingCheck { get { return Controller.CeilingCheck; } }
        private readonly bool _airControl;
        private readonly float _noKeyInputJumpTime = 0.25f;
        private bool _keyPress = false;
        private bool IsInPeakHang{get{return Controller.IsInPeakHang;}set{Controller.IsInPeakHang=value;}}
        private bool WasRunning{get{return Controller.WasRunning;}set{Controller.WasRunning=value;}}
        public JumpState(ESP.States state,bool airControl, string name = "jump") 
        : base(state,name)
        {
           _airControl = airControl;
        }
        public override void Enter(PlayerController controller,PlayerStateMachine fsm)
        {
            base.Enter(controller,fsm); // Critical!
            SetAnimations();
            if(IsInPeakHang)
            {
                IsInPeakHang = false;
            }
            if(Controller.JumpWasBuffered)
                fsm.StartCoroutine(WaitForKeyBeforeFalling());
            Controller.JumpWasBuffered = false;
            Controller.Jump(); //sets jumping to true!
        }
        private protected override void SetAnimations()
        {
            if (HasAnimation)
            {
                try
                {
                    var animName = Name;
                    if(FSM.PrevState.Type == ESP.States.Jump)
                        animName = "smrslt";
                    if(FSM.PrevState.Type == ESP.States.LedgeGrabState)
                        animName = ClimbLedgeAnimName;
                    Controller.Animator.Play(animName);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
        }
        private IEnumerator WaitForKeyBeforeFalling()
        {
            yield return new WaitForSeconds(_noKeyInputJumpTime);
            if(!_keyPress)
                SetSubState(ESP.States.Fall);
        }

        public override void Exit(ESP.States State, ESP.States SubState)
        {
            base.Exit(State, SubState);
            Controller.IsJumping = false;
        }
        protected override void TryStateSwitch() //is called in Update
        {
            _keyPress = ActionMap.Jump.WasPerformedThisFrame();
            bool fall = false || ActionMap.Jump.WasReleasedThisFrame();
            if(Controller.Velocity.y < 0)
            { 
                fall = true;
            }
            if(fall)
            {
                SetSubState(ESP.States.Fall);
            }
            
        }
        protected override void PhysicsCalculation() // is called in FixedUpdate
        {
            if(Mathf.Abs(Controller.Velocity.y) < Controller.JumpPeakHangThreshold && !IsInPeakHang)
            {
                IsInPeakHang = true;
                Controller.CurrGravity *= Controller.JumpPeakGravityMult; 
            }
            if(_airControl)
            { 
                var direction = Controls.WalkingDirection;
                float mult = 1.0f;
                if(IsInPeakHang)
                {
                    mult *= 0.5f;
                }
                if(WasRunning)
                {
                    mult *= Controller.RunSpeedMult;
                }
                Controller.Move(new Vector2(mult*direction, 0f));
            }
        }
    }
}