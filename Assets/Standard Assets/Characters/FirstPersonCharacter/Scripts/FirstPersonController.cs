using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;


		enum JumpDirection
		{
			Forward,Backward,Left,Right
		}
		bool dodging;
		JumpDirection jDir;
		public float hAxis,vAxis,m_DodgeSpeed=5f;
		float tempwalkspeed;
		Animator anim;

		int jumpCount;
		public bool aim;
		public bool toggle;

		//temp reload code
		public bool reload;
		//temp reload end

		public Camera cam;
		public float zoomSpeed = 30f;
		public float minZoomFOV = 45f;
		public float currentFOV;
		// Update is called once per frame
	


        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);


			tempwalkspeed=m_WalkSpeed;
			jDir=new JumpDirection();
			anim=GetComponentInChildren<Animator>();
			currentFOV=cam.fieldOfView;
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {

                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
				hAxis=CrossPlatformInputManager.GetAxis("Horizontal");
				vAxis=CrossPlatformInputManager.GetAxis("Vertical");
				//m_WalkSpeed=tempwalkspeed;

				//get direction
				if(hAxis>0)
				{
					jDir=JumpDirection.Right;
				}
				else if (hAxis<0)
				{
					jDir=JumpDirection.Left;
				}
				else if(vAxis>0)
				{
					jDir=JumpDirection.Forward;
				}
				else if(vAxis<0)
				{
					jDir=JumpDirection.Backward;
				}
				dodging=false;
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
				jDir=JumpDirection.Forward;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

	
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			//m_MoveDir.x = desiredMove.x*speed;
			//m_MoveDir.z = desiredMove.z*speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;
				jumpCount=0;
				if(!dodging)
				{
				m_WalkSpeed=tempwalkspeed;
				}
				if (m_Jump)
                {
					//new code::::DODGE
					jumpCount++;
					switch (jDir) {
					case JumpDirection.Forward:
						m_MoveDir.y = m_JumpSpeed;
					break;
					case JumpDirection.Backward:
						dodging=true;
						m_WalkSpeed=m_DodgeSpeed;
						m_MoveDir.y =2f;
						m_MoveDir.z = -m_DodgeSpeed;
						break;
					case JumpDirection.Left:
						dodging=true;
						m_WalkSpeed=m_DodgeSpeed;
						m_MoveDir.y =2f;
						m_MoveDir.x = -m_DodgeSpeed;
						break;
					case JumpDirection.Right:
						dodging=true;
						m_WalkSpeed=m_DodgeSpeed;
						m_MoveDir.y =2f;
						m_MoveDir.x = m_DodgeSpeed;
						break;
					}
                  
					jDir=JumpDirection.Forward;

					//new code end
					PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;

                }


            }
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
				if(m_Jump && Input.GetAxis("Vertical")>0 && jumpCount <2)
				{
					//jetpack

					jumpCount++;
					m_MoveDir.y=15f;
					m_MoveDir.z++;
				}
				m_Jump=false;
            }

			m_MoveDir.x = desiredMove.x*speed;
			m_MoveDir.z = desiredMove.z*speed;
		
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running

#endif
            // set the desired speed to be walking or running

            //player code
			aim=Input.GetButton("Fire2");

			//temp reload code
			//reload=Input.GetButtonDown("Reload");
			//temp reload end

			if(aim)
			{
				m_IsWalking=true;
				anim.SetBool("Sprint",!m_IsWalking);
				StopAllCoroutines();
				cam.fieldOfView -= zoomSpeed/8;
				if (cam.fieldOfView < minZoomFOV)
				{
					cam.fieldOfView = minZoomFOV;
				}
			}
			else
			{
				m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
				anim.SetBool("Sprint",!m_IsWalking);
				cam.fieldOfView += zoomSpeed/8;
				if (cam.fieldOfView > currentFOV)
				{
					cam.fieldOfView=currentFOV;
					
				}

				// handle speed change to give an fov kick
				// only if the player is going to a run, is running and the fovkick is to be used
				if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
				{
					StopAllCoroutines();
					StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
				}
			}

			//temp reload code
			if(reload)
			{
				anim.SetBool("Reload",reload);
				anim.SetBool("Aim",false);
				reload=false;
			}
			//temp reload end
			speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
			if(dodging)
			{
				speed=m_DodgeSpeed;
			}

            m_Input = new Vector2(horizontal, vertical);

			anim.SetBool("Aim",aim);


			//player code end

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
