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
        Vector3D target;
        double alive_count;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            drone = new DroneControler(GridTerminalSystem);

            target = drone.current_location();
            target.X += 500;

            GoTo action = new GoTo(target);

            Task task = new Task();
            task.Add_Action(action);

            drone.set_task(task);

            alive_count = 0;
        }

        
        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Alive Count " + alive_count.ToString());
            alive_count += 0.001;

            //drone.run();

            Echo(drone.thrusters.velocity.ToString());

            Vector3D current_pos = drone.current_location();
            Vector3D target_pos = drone.get_local_space(target);

            //Echo(target_pos.ToString());

            //Echo(current_pos.ToString());
            //Echo(target.ToString());

        }
    }
}