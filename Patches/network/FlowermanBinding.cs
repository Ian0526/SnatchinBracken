using GameNetcodeStuff;
using SnatchinBracken.Patches.data;
using Unity.Netcode;
using UnityEngine;

namespace SnatchingBracken.Patches.network
{
    public class FlowermanBinding : NetworkBehaviour
    {

        [ServerRpc(RequireOwnership = false)]
        public void BindPlayerServerRpc(int playerId, ulong flowermanId)
        {
            AddBindingsClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnbindPlayerServerRpc(int playerId, ulong flowermanId)
        {
            RemoveBindingsClientRpc(playerId, flowermanId);
        }

        [ClientRpc]
        public void AddBindingsClientRpc(int playerId, ulong flowermanID)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanID];

            SharedData.Instance.BindedDrags[flowermanAI] = player;
            SharedData.Instance.PlayerIDs[player] = playerId;
            SharedData.Instance.IDsToPlayerController[playerId] = player;
            SharedData.Instance.LastGrabbedTimeStamp[flowermanAI] = Time.time;
        }

        [ClientRpc]
        public void RemoveBindingsClientRpc(int playerId, ulong flowermanID)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanID];

            SharedData.Instance.BindedDrags.Remove(flowermanAI);
            SharedData.Instance.LastGrabbedTimeStamp[flowermanAI] = Time.time;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
    }
}