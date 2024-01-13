using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
