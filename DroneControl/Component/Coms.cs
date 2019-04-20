using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript.Drone.comms
{
    class Comms
    {
        IMyRadioAntenna antenna;

        public Comms(IMyGridTerminalSystem GridTerminalSystem)
        {
            /*
            antenna = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;
            pb = GridTerminalSystem.GetBlockWithName("Programmable block") as IMyProgrammableBlock;
            // Connect the PB to the antenna. This can also be done from the grid terminal.
            antenna.AttachedProgrammableBlock = pb.EntityId;

            if (antenna != null)
            {
                Echo("Setup complete.");
                setupcomplete = true;
            }
            else
            {
                Echo("Setup failed. No antenna found.");
            }

            // To create a listener, we use IGC to access the relevant method. 
            // We pass the same tag argument we used for our message. 
            IGC.RegisterBroadcastListener("SWG");
            */
        }
    }
}
