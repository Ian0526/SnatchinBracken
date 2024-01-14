using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SnatchingBracken.Patches.test
{
    internal class TestCommands
    {

        private const string modGUID = "Ovchinikov.SnatchinBracken.TestCommands";
        private static PlayerControllerB hostRef;

        private static ManualLogSource mls;

        static TestCommands()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
        [HarmonyPrefix]
        private static void GameMasterCommands(HUDManager __instance)
        {
            if (!__instance.IsHost) { return; }
            string text = __instance.chatTextField.text;
            string text2 = "!";
            mls.LogInfo((object) text);
            if (text.ToLower().StartsWith(text2.ToLower()))
            {
                ProcessCommandInput(__instance.chatTextField.text);
            }
        }

        public static void ProcessCommandInput(String command)
        {
            switch (command)
            {
                case "!hit":
                {
                        FlowermanAI flowerman = SearchForCorrelatedFlowerman(hostRef);
                        if (flowerman == null)
                        {
                            mls.LogInfo("Flowerman or PlayerControllerB is null");
                            return;
                        }
                    ManuallyUnbindPlayer(flowerman, hostRef);
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPrefix]
        static void GetHost(ref PlayerControllerB __instance)
        {
            hostRef = __instance;
            mls.LogInfo("Successfully bound host.");
        }


        private static FlowermanAI SearchForCorrelatedFlowerman(PlayerControllerB player)
        {
            foreach (var entry in SharedData.Instance.BindedDrags)
            {
                if (entry.Value.actualClientId.Equals(player.actualClientId))
                {
                    return entry.Key;
                }
            }
            return null;
        }

        // Not sure why instances are differing here, but this is needed
        private static PlayerControllerB SearchForCorrelatedPlayerObject(FlowermanAI flowerman)
        {
            if (SharedData.Instance.BindedDrags.ContainsKey(flowerman))
            {
                return SharedData.Instance.BindedDrags.GetValueSafe(flowerman);
            }
            return null;
        }

        private static void ManuallyUnbindPlayer(FlowermanAI flowerman, PlayerControllerB player)
        {
            SharedData.Instance.BindedDrags.Remove(flowerman);
            SharedData.Instance.LastGrabbedTimeStamp[flowerman] = Time.time;
            if (SharedData.Instance.PlayerIDs.ContainsKey(player))
            { 
                int playerId = SharedData.Instance.PlayerIDs.GetValueSafe(player);

                player.inSpecialInteractAnimation = false;

                flowerman.carryingPlayerBody = false;
                flowerman.creatureAnimator.SetBool("killing", value: false);
                flowerman.creatureAnimator.SetBool("carryingBody", value: false);
                flowerman.inSpecialAnimation = false;
                flowerman.inKillAnimation = false;
                flowerman.FinishKillAnimation(false);
                // make angy again
                flowerman.SwitchToBehaviourState(2);
                flowerman.CancelKillAnimationClientRpc(playerId);
                flowerman.creatureAnimator.SetBool("carryingBody", value: false);
            }
        }
    }
}
