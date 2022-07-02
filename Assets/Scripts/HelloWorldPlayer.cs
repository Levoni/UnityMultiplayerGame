using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class HelloWorldPlayer : NetworkBehaviour
{

   public NetworkVariable<NetworkString> PlayerName = new NetworkVariable<NetworkString>();
   public NetworkVariable<int> networkJumpSlide = new NetworkVariable<int>();
   private Vector3 CurrentVelocity = new Vector3();
   
   [SerializeField]
   private int speedMultiplier = 50;
   [SerializeField]
   private float rotateMultiplier = 1;

   [SerializeField]
   private Animator animator;
   [SerializeField]
   private Rigidbody rigidbody;

   public override void OnNetworkSpawn()
   {
      PlayerName.OnValueChanged += SetOverlay;
      if (NetworkManager.Singleton.IsServer)
      {
         var gameObject = GameObject.Find("NameInput");
         var component = gameObject.GetComponent<TMP_InputField>();
         PlayerName.Value = component.text;
      }
      else if (NetworkManager.Singleton.IsClient && IsOwner)
      {
         var component = GameObject.Find("NameInput").GetComponent<TMP_InputField>();
         var name = component.text;
         SetPlayerNameServerRpc(name);
      }
      var rigid = gameObject.GetComponent<Rigidbody>();
   }

   public override void OnNetworkDespawn()
   {
      PlayerName.OnValueChanged -= SetOverlay;
   }

   public void Move()
   {
      if (NetworkManager.Singleton.IsServer)
      {
         var randomPosition = GetRandomPositionOnPlane();
         transform.position = randomPosition;
      }
      else
      {
         SubmitPositionRequestServerRpc();
      }
   }
   [ServerRpc]
   void SubmitPositionRequestServerRpc()
   {
      transform.position = GetRandomPositionOnPlane();

   }
   [ServerRpc]
   void SetPlayerNameServerRpc(NetworkString name)
   {
      PlayerName.Value = name;
   }
   [ServerRpc]
   void UpdateClientPositionStateServerRpc(Vector3 move)
   {
      transform.position += move;
   }
   [ServerRpc]
   void UpdateClientAnimationStateServerRpc(int jumpSlide)
   {
      networkJumpSlide.Value = jumpSlide;
   }
   

   static Vector3 GetRandomPositionOnPlane()
   {
      return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
   }

   public void SetOverlay(NetworkString previous, NetworkString current)
   {
      var gui = gameObject.GetComponentInChildren<TextMeshPro>();
      gui.text = current;
   }

   public void SetAnimationView()
   {
      animator.SetInteger("slideJump", networkJumpSlide.Value);
   }

   public void SetCamera()
   {
      if (IsOwner)
      {
         var camera = GameObject.Find("Main Camera");
         camera.transform.position = gameObject.transform.position - (new Vector3(gameObject.transform.forward.x * 5, -5, gameObject.transform.forward.z * 5));
         camera.transform.LookAt(gameObject.transform);
      }
   }


   public void UpdatePlayerRotation(Vector2 inputVector)
   {
      gameObject.GetComponentInChildren<Transform>().Rotate(new Vector3(0, inputVector.y * rotateMultiplier * Time.deltaTime,0));
   }
   public Vector3 CalculateInternalForces(Vector2 InputVector, PlayerState state)
   {
      if (state.isGrounded || state.isSliding)
      {
         return new Vector3();
      }
      return new Vector3(transform.forward.x * InputVector.x, 0, transform.forward.z * InputVector.x);
   }
   public Vector3 ReduceVelocityTowardZero(Vector2 inputVector, Vector3 MoveSpeed)
   {
      if (inputVector.magnitude != 0)
         return MoveSpeed;

      float newX = 0;
      float newZ = 0;
      if(MoveSpeed.x > 0)
      {
         newX = Mathf.Max(MoveSpeed.x - .25f, 0 );
      } else if (MoveSpeed.x < 0)
      {
         newX = Mathf.Min(MoveSpeed.x + .25f, 0);
      }

      if (MoveSpeed.z > 0)
      {
         newZ = Mathf.Max(MoveSpeed.z - .25f, 0);
      }
      else if (MoveSpeed.z < 0)
      {
         newZ = Mathf.Min(MoveSpeed.z + .25f, 0);
      }

      return new Vector3(newX, 0 , newZ);
   }
   public Vector3 NormalizeCurrentVelocity(Vector3 MoveSpeed, PlayerState state)
   {
      float maxX = state.isSliding ? 90 : 60;
      float maxZ = state.isSliding ? 90 : 60;

      return new Vector3(Mathf.Clamp(MoveSpeed.x, -maxX, maxX), 0, Mathf.Clamp(MoveSpeed.z, -maxZ, maxZ));
   }


   // Update is called once per frame
   void Update()
   {
      if (IsOwner)
      {
         //input
         Vector2 inputVector = new Vector2();
         int slideJump = 0;
         if (Input.GetKey(KeyCode.W))
         {
            inputVector += new Vector2(1, 0);
         }
         if (Input.GetKey(KeyCode.S))
         {
            inputVector += new Vector2(-1, 0);
         }
         if (Input.GetKey(KeyCode.A))
         {
            inputVector += new Vector2(0, -1);
         }
         if (Input.GetKey(KeyCode.D))
         {
            inputVector += new Vector2(0, 1);
         }
         if (Input.GetKeyDown(KeyCode.Space))
         {
            slideJump = -1;
         }
         if (Input.GetKeyUp(KeyCode.Space))
         {
            slideJump = 1;
         }
         var isTurning = inputVector.y != 0;

         UpdatePlayerRotation(inputVector);
         //Get All forces affecting player movement
         Vector3 internalForces = CalculateInternalForces(inputVector, new PlayerState(false, false, isTurning));
         Vector3 externalForces = new Vector3();
         Vector3 inpulseForces = new Vector3();
         Vector3 combinedForces = internalForces + externalForces + inpulseForces;
         //Finalize total movement
         CurrentVelocity = Vector3.Project(CurrentVelocity, gameObject.transform.forward);
         CurrentVelocity += combinedForces;
         //Normalize movement
         Vector3 reducedVelocity = ReduceVelocityTowardZero(inputVector,CurrentVelocity);
         Vector3 FinalVelocity = NormalizeCurrentVelocity(reducedVelocity, new PlayerState(false, false, isTurning));
         CurrentVelocity = FinalVelocity;

         if (NetworkManager.Singleton.IsServer)
         {
            if (CurrentVelocity.magnitude != 0)
            {
               rigidbody.velocity = CurrentVelocity * speedMultiplier * Time.deltaTime;
            }
            if (slideJump != 0)
            {
               networkJumpSlide.Value = slideJump;
            }
         }
         else if (NetworkManager.Singleton.IsClient)
         {
            if (inputVector.magnitude != 0 || slideJump != 0)
            {
               UpdateClientPositionStateServerRpc(CurrentVelocity * speedMultiplier * Time.deltaTime);
               
            }
            if (slideJump != 0)
            {
               UpdateClientAnimationStateServerRpc(slideJump);
            }
         }
      }

      SetAnimationView();
      SetCamera();
   }

   public struct MoveInformation: INetworkSerializable
   {
      public Vector2 move;
      public int jumpSlide; // -1 slide, 0 neutral,mp

      public MoveInformation(Vector3 Move, int JumpSlide) : this()
      {
         move = new Vector2(Move.x,Move.z);
         jumpSlide = JumpSlide;
      }
      void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
      {
         serializer.SerializeValue(ref move);
         serializer.SerializeValue(ref jumpSlide);
      }
   }

   public struct PlayerState: INetworkSerializable
   {
      public bool isGrounded;
      public bool isSliding;
      public bool isTurning;
      public PlayerState(bool IsGrounded, bool IsSliding, bool Isturning) : this()
      {
         isGrounded = IsGrounded;
         isSliding = IsSliding;
         isTurning = Isturning;
      }
      void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
      {
         serializer.SerializeValue(ref isGrounded);
         serializer.SerializeValue(ref isSliding);
         serializer.SerializeValue(ref isTurning);
      }
   }
}