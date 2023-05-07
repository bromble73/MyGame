using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class MoveState : State
{
    private ThirdPersonController _tpc;
    private float _speedMovement;
    public MoveState(ThirdPersonController tpc)
    {
        _tpc = tpc;
    }
    
    public override void Enter()
    {
        base.Enter();
        Debug.Log("Я начинаю идти или бежать");
    }

    public override void Exit()
    {
        base.Exit();        
        Debug.Log("Я больше не иду и не бегу");

    }

    public override void Tick()
    {
        base.Tick();
        
        Debug.Log("Я сейчас иду или бегу");
        if (_tpc.input.sprint)
        {
            _speedMovement = _tpc.SprintSpeed;
        }
        else
        {
            _speedMovement = _tpc.MoveSpeed;
        }
        Move(_speedMovement);
    }
    
    private void Move(float movementSpeed)
        {
            
            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_tpc.input.move == Vector2.zero) {movementSpeed = 0; }
            
            

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_tpc.controller.velocity.x, 0.0f, _tpc.controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _tpc.input.analogMovement ? _tpc.input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < movementSpeed - speedOffset ||
                currentHorizontalSpeed > movementSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _tpc._speed = Mathf.Lerp(currentHorizontalSpeed, movementSpeed * inputMagnitude,
                    Time.deltaTime * _tpc.SpeedChangeRate);

                // round speed to 3 decimal places
                _tpc._speed = Mathf.Round(_tpc._speed * 1000f) / 1000f;
            }
            else
            {
                _tpc._speed = movementSpeed;
            }

            _tpc._animationBlend = Mathf.Lerp(_tpc._animationBlend, movementSpeed, Time.deltaTime * _tpc.SpeedChangeRate);
            if (_tpc._animationBlend < 0.01f) _tpc._animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_tpc.input.move.x, 0.0f, _tpc.input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_tpc.input.move != Vector2.zero)
            {
                _tpc._targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                       _tpc.mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(_tpc.transform.eulerAngles.y, _tpc._targetRotation, ref _tpc._rotationVelocity,
                    _tpc.RotationSmoothTime);
            
                // rotate to face input direction relative to camera position
                _tpc.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            _tpc._currentVelocity.x = Mathf.Lerp(_tpc._currentVelocity.x, _tpc.input.move.x * movementSpeed, _tpc.AnimationBlenderSpeed * Time.fixedDeltaTime);
            _tpc._currentVelocity.y = Mathf.Lerp(_tpc._currentVelocity.y, _tpc.input.move.y * movementSpeed, _tpc.AnimationBlenderSpeed * Time.fixedDeltaTime);

            Vector3 targetDirection = Quaternion.Euler(0.0f, _tpc._targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _tpc.controller.Move(targetDirection.normalized * (_tpc._speed * Time.deltaTime) +
                                 new Vector3(0.0f, _tpc._verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_tpc._hasAnimator)
            {
                _tpc.animator.SetFloat(_tpc._animIDSpeed, _tpc._animationBlend);
                _tpc.animator.SetFloat(_tpc._animIDMotionSpeed, inputMagnitude);
                // Debug.Log("x: " + _currentVelocity.x);
                _tpc.animator.SetFloat(_tpc._xVelHash, _tpc._currentVelocity.x);
                _tpc.animator.SetFloat(_tpc._yVelHash, _tpc._currentVelocity.y);
                
            }

            // if (_tpc.input.move == Vector2.zero)
            // {
            //     _tpc._currentVelocity.x = Mathf.Lerp(_tpc._currentVelocity.x, 0, Time.deltaTime);
            // }
        }
}
