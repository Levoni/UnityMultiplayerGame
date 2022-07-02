
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace HelloWorld
{
   public class HelloWorldManager : MonoBehaviour
   {

      [SerializeField]
      private Button startServerButton;

      [SerializeField]
      private Button startHostButton;

      [SerializeField]
      private Button startClientButton;

      [SerializeField]
      private Button Move;

      [SerializeField]
      private TextMeshProUGUI playersInGameText;

      private void Awake()
      {
         Cursor.visible = true;
      }

      private void Update()
      {
         playersInGameText.text = $"Players in game {PlayersManager.Instance.playersInGame.Value}";
      }

      private void Start()
      {
         startServerButton.onClick.AddListener(() =>
         {
            if (NetworkManager.Singleton.StartHost())
            {
               Debug.Log("Server started...");
            }
            else
            {
               Debug.Log("Server could not be started...");
            }

         });

         startHostButton.onClick.AddListener(() =>
         {
            if (NetworkManager.Singleton.StartServer())
            {
               Debug.Log("Host started...");
            }
            else
            {
               Debug.Log("Host could not be started...");
            }
         });

         startClientButton.onClick.AddListener(() =>
         {
            if (NetworkManager.Singleton.StartClient())
            {
               Debug.Log("Client started...");
            }
            else
            {
               Debug.Log("Client could not be started...");
            }
         });

         Move.onClick.AddListener(() =>
         {
            SubmitNewPosition();
         });
      }
      void SubmitNewPosition()
      {
         if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
         {
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
               NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<HelloWorldPlayer>().Move();
         }
         else
         {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<HelloWorldPlayer>();
            player.Move();
         }
      }
   }
}