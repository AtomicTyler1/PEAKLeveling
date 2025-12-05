using Photon.Pun;
using Photon.Realtime;
using System;
using System.Security.Cryptography;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace Leveling.Misc
{
    public class Netcode : MonoBehaviourPun
    {
        private static Netcode _instance = null!;
        private PhotonView _photonView = null!;

        public static Netcode Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("LevelingNetcode");
                    _instance = singleton.AddComponent<Netcode>();
                    DontDestroyOnLoad(singleton);
                }
                return _instance;
            }
        }

        public static void EnsureInitialized()
        {
            _ = Instance;
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            _photonView = GetComponent<PhotonView>();
            if (_photonView == null)
            {
                _photonView = gameObject.AddComponent<PhotonView>();
                _photonView.ViewID = 8437;
            }

            DontDestroyOnLoad(gameObject);
        }

        public void SendLevelUpdateRPC(int newLevel)
        {
            if (PhotonNetwork.InRoom)
            {
                _photonView.RPC(nameof(RPC_ReceiveLevelUpdate), RpcTarget.All, newLevel);
                Plugin.Log.LogInfo($"Broadcasted level update: Lvl {newLevel}");
            }
        }

        public void RequestAllPlayerLevels()
        {
            if (PhotonNetwork.InRoom)
            {
                _photonView.RPC(nameof(RPC_RequestPlayerLevels), RpcTarget.MasterClient);
                Plugin.Log.LogInfo("Requested all existing player levels from Master Client.");
            }
        }

        [PunRPC]
        public void RPC_RequestPlayerLevels(PhotonMessageInfo info)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(RPC_RespondWithMyLevel), info.Sender, LevelingAPI.Level);
                Plugin.Log.LogInfo($"Master Client responding to {info.Sender.NickName}'s level request with own level: Lvl {LevelingAPI.Level}");
            }
        }

        [PunRPC]
        public void RPC_RespondWithMyLevel(int level, PhotonMessageInfo info)
        {
            PhotonPlayer sender = info.Sender;

            if (sender == null || sender.IsLocal) return;

            LevelingAPI.SetRemotePlayerLevel(sender, level);
            Plugin.Log.LogInfo($"Received level sync from {sender.NickName}: Lvl {level}");
        }

        [PunRPC]
        public void RPC_ReceiveLevelUpdate(int level, PhotonMessageInfo info)
        {
            PhotonPlayer sender = info.Sender;

            if (sender == null) return;

            if (!sender.IsLocal)
            {
                LevelingAPI.SetRemotePlayerLevel(sender, level);
                Plugin.Log.LogInfo($"Received level update from {sender.NickName}: Lvl {level}");
            }
        }
    }
}