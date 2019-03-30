using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using IngameScript.DroneControl;
using IngameScript.DroneControl.utility.task;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        DroneControler drone;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            drone = new DroneControler(GridTerminalSystem);
        }
        double alive_count = 0;
        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Running " + alive_count.ToString());
            alive_count += 0.001;

            drone.run();


            Vector3D pos = new Vector3D(1, 2, 3);

            GoTo action = new GoTo(pos);

            Task task = new Task("");

            string s = action.Serialize();

            Vector3D new_pos = action.Deserialization(s);

            Echo(s);
            Echo(new_pos.ToString());

        }
    }
}