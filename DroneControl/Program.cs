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
        Vector3D test_target = new Vector3D(37.64, 73.47, -186.15);
        double alive_count;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            drone = new DroneControler(GridTerminalSystem);

            GoTo action = new GoTo(test_target);

            Task task = new Task();
            task.Add_Action(action);

            drone.set_task(task);

            alive_count = 0;
        }

        
        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Alive Count " + alive_count.ToString());
            alive_count += 0.001;

            drone.run();

            //Echo(drone.thrusters.velocity.ToString());

            Vector3D current_pos = drone.current_location();
            Vector3D target_pos = drone.get_local_space(test_target);

            Vector3D target_grid = drone.get_local_space(test_target);

            Vector3D local_target = drone.get_local_space(test_target);

            double vectore_len = local_target.Length();
            Vector3D target_speed = new Vector3D();
            target_speed.X = local_target.X / vectore_len * 400;
            target_speed.Y = local_target.Y / vectore_len * 400;
            target_speed.Z = local_target.Z / vectore_len * 400;

            //Echo(target_pos.ToString());
            Echo(drone.thrusters.stopping_distances.ToString());
            //Echo(target_speed.ToString());
            //Echo((target_pos.Z - drone.thrusters.stopping_distances.Z).ToString());


            //drone.thrusters.SetVelocity(-10);
            //Echo(target.ToString());
        }
    }
}