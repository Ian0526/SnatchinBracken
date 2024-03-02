using BepInEx.Logging;
using GameNetcodeStuff;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using System.Collections;
using UnityEngine;

namespace SnatchingBracken.Patches.tasks
{
    public class FlowermanLocationTask : MonoBehaviour
    {
        private Coroutine checkStuckCoroutine;

        private static ManualLogSource mls;

        static FlowermanLocationTask()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("Bracken Location Task");
        }

        public void StartCheckStuckCoroutine(FlowermanAI flowermanAI, PlayerControllerB player)
        {
            checkStuckCoroutine = StartCoroutine(CheckIfStuck(flowermanAI, player));
        }

        public void StopCheckStuckCoroutine()
        {
            if (checkStuckCoroutine != null)
            {
                StopCoroutine(checkStuckCoroutine);
                checkStuckCoroutine = null;
            }
        }

        private IEnumerator CheckIfStuck(FlowermanAI flowermanAI, PlayerControllerB player)
        {
            Vector3 lastPosition = flowermanAI.transform.position;

            while (flowermanAI != null)
            {
                yield return new WaitForSeconds(5);
                Vector3 currentPosition = flowermanAI.transform.position;
                if (Vector3.Distance(lastPosition, currentPosition) <= 1f)
                {
                    HandleStuckFlowerman(flowermanAI, player);
                }
                lastPosition = currentPosition;
            }
        }

        private void HandleStuckFlowerman(FlowermanAI flowermanAI, PlayerControllerB player)
        {
            Debug.Log("FlowermanAI is stuck, handling...");

            StopCheckStuckCoroutine();
            SharedData.UpdateTimestampNow(flowermanAI, player);
            int playerId = SharedData.Instance.PlayerIDs[player];
            flowermanAI.inSpecialAnimationWithPlayer = player;

            player.inSpecialInteractAnimation = true;
            player.gameObject.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(playerId);

            flowermanAI.KillPlayerAnimationClientRpc(playerId);
        }
    }
}
