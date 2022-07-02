using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayersManager : NetworkBehaviour
{
   private static PlayersManager _instance;

   public static PlayersManager Instance
   {
      get
      {
         if (_instance == null)
         {
            var objs = FindObjectsOfType(typeof(PlayersManager)) as PlayersManager[];
            if (objs.Length > 0)
               _instance = objs[0];
            if (objs.Length > 1)
            {
               Debug.LogError("There is more than one " + typeof(PlayersManager).Name + " in the scene.");
            }
            if (_instance == null)
            {
               GameObject obj = new GameObject();
               obj.name = string.Format("_{0}", typeof(PlayersManager).Name);
               _instance = obj.AddComponent<PlayersManager>();
            }
         }
         return _instance;
      }
   }


   public NetworkVariable<int> playersInGame = new NetworkVariable<int>();

   private void Start()
   {
      NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
      {
         if (NetworkManager.Singleton.IsServer)
         {
            Debug.Log($"{id} just connected...");
            playersInGame.Value++;
         }
      };

      NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
      {
         if (NetworkManager.Singleton.IsServer)
         {
            Debug.Log($"{id} just disconnected...");
            playersInGame.Value--;
         }
      };


   }
}
