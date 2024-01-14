using GameNetcodeStuff;
using System.Collections.Generic;

namespace SnatchinBracken.Patches.data
{
    internal class SharedData
    {
        private static SharedData _instance;
        public static SharedData Instance => _instance ?? (_instance = new SharedData());

        // Bracken to Player
        public Dictionary<FlowermanAI, PlayerControllerB> BindedDrags { get; } = new Dictionary<FlowermanAI, PlayerControllerB>(); 
        public Dictionary<PlayerControllerB, int> PlayerIDs { get; } = new Dictionary<PlayerControllerB, int>();
        public Dictionary<FlowermanAI, float> LastGrabbedTimeStamp { get; } = new Dictionary<FlowermanAI, float>();

        public bool DropItems { get; set; }
        public bool IgnoreTurrets { get; set; }
        public bool IgnoreMines { get; set; }
        public float KillAtTime { get; set; }
    }
}
