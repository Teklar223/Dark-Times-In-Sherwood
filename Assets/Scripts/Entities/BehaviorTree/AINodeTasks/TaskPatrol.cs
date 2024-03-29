using System;
using System.Collections.Generic;
using BehaviorTree;
using DTIS;
using UnityEngine;

namespace DTIS
{
    public class TaskPatrol : Node
    {
        private readonly EntityController _AIcontroller;
        private float _prevX;
        private int stuckCounter = 0;
        private readonly Transform[] _waypoints;
        private int _currentWaypointIndex = 0;
        private float _waitTime = 1f; // in seconds
        private float _waitCounter = 0f;
        private bool _waiting = false;
        public TaskPatrol(Transform[] waypoints, EntityController controller)
        {
            _AIcontroller = controller;
            _prevX = _AIcontroller.transform.position.x;
            _waypoints = waypoints;
        }

        public override NodeState Evaluate()
        {
            if (_waypoints != null)
            {
                if (_waypoints.Length != 0)
                {
                    if (_waiting)
                    {
                        _waitCounter += Time.deltaTime;
                        if (_waitCounter >= _waitTime)
                        {
                            _waiting = false;
                            _AIcontroller.Animator.SetInteger("AnimState", 2);
                        }
                    }
                    else
                    {
                        Transform wp = _waypoints[_currentWaypointIndex];
                        if (wp != null)
                        {

                            _AIcontroller.Flip(wp.position.x); // flips the entity according the the position x of target.

                            if (Math.Abs(_AIcontroller.transform.position.x - wp.position.x) < 0.01f)
                            {
                                _AIcontroller.transform.position = new Vector2(wp.position.x, _AIcontroller.transform.position.y);
                                _waitCounter = 0f;
                                _waiting = true;

                                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                                _AIcontroller.Animator.SetInteger("AnimState", 0);
                            }
                            else
                            {
                                float direction = _AIcontroller.transform.position.x < wp.position.x ? 1.0f : -1.0f;
                                _AIcontroller.Move(new Vector2(direction, 0f));

                                if (Math.Abs(_AIcontroller.transform.position.x - _prevX) < 0.01f)
                                {
                                    stuckCounter += 1;
                                    if (stuckCounter > 2)
                                    {
                                        Nudge(new Vector2(direction, 0.05f));
                                        stuckCounter = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _prevX = _AIcontroller.transform.position.x;
            _state = NodeState.RUNNING;
            return _state;
        }

        private void Nudge(Vector2 direction)
        {
            Vector2 newPos = _AIcontroller.transform.position;
            newPos.x += direction.x * Time.deltaTime;
            newPos.y += direction.y * Time.deltaTime;
            _AIcontroller.transform.position = newPos;
        }
    }
}