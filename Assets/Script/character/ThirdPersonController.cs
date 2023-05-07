 using System;
 using System.Collections;
 using Cinemachine;
 using UnityEngine;
 using UnityEngine.EventSystems;
 using UnityEngine.UIElements;
 using Random = UnityEngine.Random;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

 
namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        public float CrouchSpeed = 1.5f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;
        
        
        
        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        public float _speed;
        public float _animationBlend;
        public float _targetRotation = 0.0f;
        public float _rotationVelocity;
        public float _verticalVelocity;
        public float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        
        

        // animation IDs
        public int _animIDSpeed;
        public int _animIDGrounded;
        public int _animIDJump;
        public int _animIDFreeFall;
        public int _animIDMotionSpeed;
        public int _animIDDance;
        public int _animIDCrouch;
        public int _animIDAim;
        public int _animIDAttackingNow;
        
        // public enum State 
        // {
        //     None,
        //     Idle,
        //     Move,
        //     Sprint,
        //     Dance,
        //     Jump,
        //     Crouch,
        //     ReadyToFight
        // }
        //
        // public State _currentState = State.None;
        
        [Header("Fight system")]
        [SerializeField] private CinemachineVirtualCamera _aimCam;
        [SerializeField] private LayerMask aimColliderLayerMask;
        [SerializeField] private float normalSensitivity = 2f;
        [SerializeField] private float aimSensitivity = 1f;
        [SerializeField] private Transform debugTransform;
        public GameObject aimDot;
        
        private bool _rotateOnMove = true;
        public float sensitivity = 1f;

        



#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        public Animator animator;
        public CharacterController controller;
        public StarterAssetsInputs input;
        public GameObject mainCamera;

        private const float _threshold = 0.01f;

        public StateMachine stateMachine;

        public bool _hasAnimator;
        float targetSpeed;
        public float AnimationBlenderSpeed = 9f; // Скорость смешивания анимаций (_animationBlend работает не так как нужно в данной ситуации)
        public int _xVelHash; // Значение движения по горизонтали 
        public int _yVelHash; // Значение движения по вертикали 
        public int _crouchHash; // Значение присяда
        public Vector2 _currentVelocity; // Для аниматора

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        

        private void Awake()
        {
            // get a reference to our main camera
            if (mainCamera == null)
            {
                mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            
            
        }

        private void Start()
        {
            stateMachine = new StateMachine();
            stateMachine.Initialize(new IdleState());
            
            // _currentState = State.Idle;
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            
            _hasAnimator = TryGetComponent(out animator);
            controller = GetComponent<CharacterController>();
            input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();
            
      
            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            stateMachine.currentState.Tick();
            // switch (_currentState)
            // {
            //     case State.None:
            //         break;
            //     
            //     case State.Idle:
            //         Stopper();
            //         break;
            //     
            //     case State.Move:
            //         break;
            //     
            //     case State.Sprint:
            //         break;
            //     
            //     case State.Dance:
            //         Dance();
            //         break;
            //     
            //     case State.Jump:
            //         Stopper();
            //         break;
            //     
            //     case State.Crouch:
            //         Crouch();
            //         break;
            //     
            //     case State.ReadyToFight:
            //         break;
            //     
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }
            
            // Debug.LogError("_currentState is " + _currentState);
            
            _hasAnimator = TryGetComponent(out animator);

            // FightSystem();
            GroundedCheck();
            // Move();
            // Dance();
            JumpAndGravity();
            if (input.dance && !animator.GetBool(_animIDJump))
            {
                stateMachine.ChangeState(new DanceState(this));
            }
            if (input.move != Vector2.zero)
            {
                // if (input.crouch)
                // {
                //     Debug.Log("Кродёться");
                // }
                // else
                // {
                    Debug.Log("Идёт");
                    stateMachine.ChangeState(new MoveState(this));
                // }
            } 
            
            Debug.Log(stateMachine.currentState);
            
            // if (input.jump)
            // {                
            //     _currentState = State.Jump;
            // }

            // if (input.sprint)
            // {
            //     _currentState = State.Sprint;
            // }

        }

        // private void Stopper()
        // {
        //     animator.SetBool(_animIDCrouch, false);
        //     animator.SetBool(_animIDDance, false);
        // }

        private void FixedUpdate()
        {
            // if (input.crouch)
            // {
            //     _currentState = State.Crouch;
            // }
            Crouch();

        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDDance = Animator.StringToHash("Dance");
            _xVelHash   = Animator.StringToHash("MoveX");
            _yVelHash   = Animator.StringToHash("MoveY");
            _animIDCrouch = Animator.StringToHash("Crouch");
            _animIDAim = Animator.StringToHash("Aim");
            _animIDAttackingNow = Animator.StringToHash("AttackingNow");

        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += input.look.x * deltaTimeMultiplier * sensitivity;
                _cinemachineTargetPitch += input.look.y * deltaTimeMultiplier * sensitivity;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        // private void Move()
        // {
        //     // set target speed based on move speed, sprint speed and if sprint is pressed
        //     // targetSpeed = input.sprint ? SprintSpeed : MoveSpeed;
        //     // проверяем, нажата ли кнопка для бега
        //     if (input.crouch)
        //     {
        //         targetSpeed = CrouchSpeed;
        //     }
        //     else if (input.sprint && !input.isAim)
        //     {
        //         targetSpeed = SprintSpeed;
        //     }
        //     else
        //     {
        //         targetSpeed = MoveSpeed;
        //     }
        //     // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon
        //
        //     // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //     // if there is no input, set the target speed to 0
        //     if (input.move == Vector2.zero) targetSpeed = 0.0f;
        //     
        //     
        //
        //     // a reference to the players current horizontal velocity
        //     float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
        //
        //     float speedOffset = 0.1f;
        //     float inputMagnitude = input.analogMovement ? input.move.magnitude : 1f;
        //
        //     // accelerate or decelerate to target speed
        //     if (currentHorizontalSpeed < targetSpeed - speedOffset ||
        //         currentHorizontalSpeed > targetSpeed + speedOffset)
        //     {
        //         // creates curved result rather than a linear one giving a more organic speed change
        //         // note T in Lerp is clamped, so we don't need to clamp our speed
        //         _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
        //             Time.deltaTime * SpeedChangeRate);
        //
        //         // round speed to 3 decimal places
        //         _speed = Mathf.Round(_speed * 1000f) / 1000f;
        //     }
        //     else
        //     {
        //         _speed = targetSpeed;
        //     }
        //
        //     _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        //     if (_animationBlend < 0.01f) _animationBlend = 0f;
        //
        //     // normalise input direction
        //     Vector3 inputDirection = new Vector3(input.move.x, 0.0f, input.move.y).normalized;
        //
        //     // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //     // if there is a move input rotate player when the player is moving
        //     if (input.move != Vector2.zero)
        //     {
        //         _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
        //                           mainCamera.transform.eulerAngles.y;
        //         float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
        //             RotationSmoothTime);
        //     
        //         // rotate to face input direction relative to camera position
        //         transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        //     }
        //
        //     _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, input.move.x * targetSpeed, AnimationBlenderSpeed * Time.fixedDeltaTime);
        //     _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, input.move.y * targetSpeed, AnimationBlenderSpeed * Time.fixedDeltaTime);
        //
        //     Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        //
        //     // move the player
        //     controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
        //                      new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        //
        //     // update animator if using character
        //     if (_hasAnimator)
        //     {
        //         animator.SetFloat(_animIDSpeed, _animationBlend);
        //         animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        //         // Debug.Log("x: " + _currentVelocity.x);
        //         animator.SetFloat(_xVelHash, _currentVelocity.x);
        //         animator.SetFloat(_yVelHash, _currentVelocity.y);
        //         
        //     }
        // }

        private void Crouch()
        {
            if (input.crouch)
            {
                animator.SetBool(_animIDCrouch, true);
            }
            else if (input.sprint)
            {
                animator.SetBool(_animIDCrouch, false);
                // _currentState = State.Move;
            }
            else
            {
                animator.SetBool(_animIDCrouch, false);
                // _currentState = State.Idle;
            }

        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;
                // Stopper();

                // update animator if using character
                if (_hasAnimator)
                {
                    animator.SetBool(_animIDJump, false);
                    animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
                // else
                // {
                //     _currentState = State.Idle;
                // }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(controller.center), FootstepAudioVolume);
            }
        }

        // private void Dance()
        // {
        //     if (input.dance)
        //     {
        //         if (animator.GetBool(_animIDDance))
        //         {
        //             animator.SetBool(_animIDDance, false);
        //             // _currentState = State.Idle;
        //         }
        //         else
        //         {
        //             animator.SetBool(_animIDDance, true);
        //         }
        //
        //         input.dance = false;
        //     }
        // }
    }

        // public void SetSensitivity(float newSensitivity)
        // {
        //     sensitivity = newSensitivity;
        // }
        // public void SetRotateOnMove(bool newRotateOnMove)
        // {
        //     _rotateOnMove = newRotateOnMove;
        // }

        
        
        // private void FightSystem()
        // {
            // var mouseWorldPosition = Vector3.zero;
            // // var hitPoint = Vector3.zero;
            // var screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            // var ray = Camera.main.ScreenPointToRay(screenCenterPoint);
            //
            // // Transform hitTransform = null;
            //
            // if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
            // {
            //     // debugTransform.position = raycastHit.point;
            //     mouseWorldPosition = raycastHit.point;
            //     // hitPoint = raycastHit.point;
            //     // hitTransform = raycastHit.transform;
            // }
            //
            // if (input.isAim)
            // {
            //     _aimCam.gameObject.SetActive(true);
            //     SetSensitivity(aimSensitivity);
            //     SetRotateOnMove(false);
            //     animator.SetBool(_animIDAim, true);
            //     aimDot.SetActive(true);
            //     
            //     animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));
            //     
            //     Vector3 worldAimTarget = mouseWorldPosition;
            //     worldAimTarget.y = transform.position.y;
            //     Vector3 aimDirection = (worldAimTarget - transform.position).normalized;
            //
            //     transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
            //     
            // }
            // else
            // {
            //     _aimCam.gameObject.SetActive(false);
            //     SetSensitivity(normalSensitivity);
            //     SetRotateOnMove(true);
            //     animator.SetBool(_animIDAim, false);
            //     aimDot.SetActive(false);
            //
            //     // animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
            // }
            //
            // if (input.isFire && !input.isAim)
            // {
            //     animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));
            //
            //     if (!animator.GetBool(_animIDAttackingNow))
            //     {
            //         animator.SetTrigger("Attack trigger");
            //         Debug.Log("Боньк");
            //     }
            //     
            //     
            //     AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);
            //     // StartCoroutine(FitnessCoroutine(stateInfo.length));
            // }
            // else
            // {
            // }
        // }


        // private IEnumerator FitnessCoroutine(float delay)
        // {
        //     yield return new WaitForSeconds(delay);
        //     animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
        //     // Debug.Log("Вот и всё, ребята");
        // }
    }
 

