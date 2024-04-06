using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using SnatchingBracken.Patches.network;

namespace SnatchinBracken.Patches.data
{
    internal class SharedData
    {
        private static SharedData _instance;
        public static SharedData Instance => _instance ?? (_instance = new SharedData());

        private static System.Random _random = new System.Random();
        public static System.Random RandomInstance => _random;

        // Bracken to Player
        public Dictionary<FlowermanAI, PlayerControllerB> BindedDrags { get; } = new Dictionary<FlowermanAI, PlayerControllerB>();
        public Dictionary<ulong, FlowermanAI> FlowermanIDs { get; } = new Dictionary<ulong, FlowermanAI>();
        public Dictionary<PlayerControllerB, int> PlayerIDs { get; } = new Dictionary<PlayerControllerB, int>();
        public Dictionary<int, PlayerControllerB> IDsToPlayerController { get; } = new Dictionary<int, PlayerControllerB>();
        public Dictionary<FlowermanAI, float> LastGrabbedTimeStamp { get; } = new Dictionary<FlowermanAI, float>();
        public Dictionary<FlowermanAI, bool> GradualDamageCoroutineStarted = new Dictionary<FlowermanAI, bool>();
        public Dictionary<FlowermanAI, bool> InsanityCoroutineStarted = new Dictionary<FlowermanAI, bool>();
        public Dictionary<PlayerControllerB, float> DroppedTimestamp = new Dictionary<PlayerControllerB, float>();

        public bool DropItems { get; set; }
        public bool IgnoreTurrets { get; set; }
        public bool InstantKillIfAlone { get; set; }
        public bool IgnoreMines { get; set; }
        public bool AllowTeleports { get; set; }
        public bool DoDamageOnInterval { get; set; }
        public bool StuckForceKill { get; set; }
        public bool MonstersIgnorePlayers { get; set; }
        public bool BrackenRoom { get; set; }
        public bool KillBasedOffOfTime { get; set; }
        public float KillAtTime { get; set; }
        public float SecondsBeforeNextAttempt { get; set; }
        public int DamageDealtAtInterval { get; set; }
        public int PercentChanceForInsta { get; set; }
        public bool KillBasedOffOfDistance { get; set; }
        public float DistanceFromFavorite { get; set; }
        public Transform BrackenRoomPosition { get; set; }

        public static void UpdateTimestampNow(FlowermanAI flowermanAI, PlayerControllerB player)
        {
            SharedData.Instance.LastGrabbedTimeStamp[flowermanAI] = Time.time;
            SharedData.Instance.DroppedTimestamp[player] = Time.time;
        }

        public static void FlushDictionaries()
        {
            SharedData.Instance.BindedDrags.Clear();
            SharedData.Instance.FlowermanIDs.Clear();
            SharedData.Instance.LastGrabbedTimeStamp.Clear();
            SharedData.Instance.GradualDamageCoroutineStarted.Clear();
            SharedData.Instance.DroppedTimestamp.Clear();
            // we can keep player stuff
        }

        public static void GiveChillPillToAll()
        {
            foreach (var entry in SharedData.Instance.BindedDrags)
            {
                int id = SharedData.Instance.PlayerIDs[entry.Value];
                entry.Value.gameObject.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(id);
            }
        }
    }
}
