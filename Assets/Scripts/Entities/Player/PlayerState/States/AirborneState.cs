using UnityEngine;
using UnityEngine.Diagnostics;

namespace DTIS
{
    public class AirborneState : PlayerState
    {
        private readonly int _maxActions = Util.Constants.MaxActionsMidAir;
        private int _actionsMidAir = 0;
        public AirborneState(string name = "Airborne") 
        : base(name,false){}
        public override void Enter(PlayerController controller,PlayerStateMachine fsm)
        {
            base.Enter(controller,fsm); // Critical!
            FSM.Grounded = false;
            _actionsMidAir = 1;
        }
        protected override void TryStateSwitch()
        {
            if(Controller.Velocity.y == 0)
            {
                SetStates(ESP.States.Grounded,ESP.States.Idle);
            }
            else if(_actionsMidAir < _maxActions)
            {
                if(ActionMap.Jump.WasPressedThisFrame())
                {
                    ++_actionsMidAir;
                    SetSubState(ESP.States.Jump);
                }
                /*
                else if(ActionMap.Dash.WasPressedThisFrame())
                {
                    ++_actionsMidAir;
                    SetSubState(ESP.States.Dash);
                }
                */ 
            }
            if(ActionMap.Walk.IsPressed())
            {
                SetSubState(ESP.States.Walk);
            }
        }
        protected override void PhysicsCalculation()
        {

        }
    }
}