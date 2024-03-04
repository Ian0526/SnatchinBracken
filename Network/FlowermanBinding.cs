using GameNetcodeStuff;
using SnatchinBracken.Patches.data;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

        [ServerRpc(RequireOwnership = false)]
        public void ResetEntityStatesServerRpc(int playerId, ulong flowermanId)
        {
            ResetEntityStatesClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PrepForBindingServerRpc(int playerId, ulong flowermanId)
        {
            PrepForBindingClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DamagePlayerServerRpc(int playerId, int damage)
        {
            DamagePlayerClientRpc(playerId, damage);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateFavoriteSpotServerRpc(int playerId, ulong flowermanId)
        {
            UpdateFavoriteSpotClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MufflePlayerVoiceServerRpc(int playerId)
        {
            MufflePlayerVoiceClientRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnmufflePlayerVoiceServerRpc(int playerId)
        {
            UnmufflePlayerVoiceClientRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MakeInsaneServerRpc(int playerId, float targetInsanity)
        {
            MakeInsaneClientRpc(playerId, targetInsanity);
        }

        [ServerRpc(RequireOwnership = false)]
        public void GiveChillPillServerRpc(int playerId)
        {
            GiveChillPillClientRpc(playerId);
        }

        [ClientRpc]
        public void UpdateFavoriteSpotClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            Transform transform;
            if (SharedData.Instance.BrackenRoom && SharedData.Instance.BrackenRoomPosition != null)
            {
                transform = SharedData.Instance.BrackenRoomPosition;
            }
            else
            {
                transform = flowermanAI.ChooseFarthestNodeFromPosition(player.transform.position);
            }

            flowermanAI.favoriteSpot = transform;
        }

        [ClientRpc]
        public void GiveChillPillClientRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            if (player == StartOfRound.Instance.localPlayerController)
            {
                player.insanityLevel = 0.0f;
                StartOfRound.Instance.fearLevelIncreasing = false;
                StartOfRound.Instance.fearLevel = 0;
            }
        }

        [ClientRpc]
        public void MakeInsaneClientRpc(int playerId, float targetInsanity)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            if (player == StartOfRound.Instance.localPlayerController)
            {
                player.JumpToFearLevel(targetInsanity);
            }
        }

        [ClientRpc]
        public void MufflePlayerVoiceClientRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            if (player.currentVoiceChatAudioSource == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (player.currentVoiceChatAudioSource != null)
            {
                player.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = 5f;
                OccludeAudio component = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
                component.overridingLowPass = true;
                component.lowPassOverride = 500f;
                player.voiceMuffledByEnemy = true;
            }
        }


        [ClientRpc]
        public void UnmufflePlayerVoiceClientRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            if (player.currentVoiceChatAudioSource == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (player.currentVoiceChatAudioSource != null)
            {
                player.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = 1f;
                OccludeAudio component = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
                component.overridingLowPass = false;
                component.lowPassOverride = 20000f;
                player.voiceMuffledByEnemy = false;
            }
        }

        [ClientRpc]
        public void DamagePlayerClientRpc(int playerId, int damage)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            player.DamagePlayer(damage, true, true, CauseOfDeath.Suffocation);
        }

        [ClientRpc]
        public void ResetEntityStatesClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            player.inSpecialInteractAnimation = false;
            player.inAnimationWithEnemy = null;

            flowermanAI.carryingPlayerBody = false;
            flowermanAI.creatureAnimator.SetBool("killing", value: false);
            flowermanAI.creatureAnimator.SetBool("carryingBody", value: false);
            flowermanAI.stunnedByPlayer = null;
            flowermanAI.stunNormalizedTimer = 0f;
            flowermanAI.angerMeter = 0f;
            flowermanAI.isInAngerMode = false;
            flowermanAI.timesThreatened = 0;
            flowermanAI.inKillAnimation = false;
            flowermanAI.evadeStealthTimer = 0.1f;
            flowermanAI.inSpecialAnimationWithPlayer = null;
            flowermanAI.inSpecialAnimation = false;
            // little did i know this one is extremely important
            flowermanAI.SetClientCalculatingAI(false);
            flowermanAI.agent.enabled = true;
            flowermanAI.favoriteSpot = null;
            flowermanAI.FinishKillAnimation(false);
        }

        [ClientRpc]
        public void PrepForBindingClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            flowermanAI.creatureAnimator.SetBool("killing", value: false);
            flowermanAI.creatureAnimator.SetBool("carryingBody", value: true);
            flowermanAI.carryingPlayerBody = true;

            player.inSpecialInteractAnimation = true;

            flowermanAI.inKillAnimation = false;
            flowermanAI.targetPlayer = null;
        }

        [ClientRpc]
        public void AddBindingsClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

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
            SharedData.Instance.DroppedTimestamp[player] = Time.time;
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