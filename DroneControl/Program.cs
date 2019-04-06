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
using IngameScript.DroneControl.utility;

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

            //drone.run();

            //Echo(drone.thrusters.velocity.ToString());

            Vector3D local_target = drone.get_local_space(test_target);
            Vector3D stopping_distances = drone.thrusters.stopping_distances;
            Vector3D target_diff = new Vector3D();
            Vector3D target_speed = new Vector3D();
            Vector3D target_speed_nomolized = new Vector3D();


            target_diff.X = local_target.X - stopping_distances.X;
            target_diff.Y = local_target.Y - stopping_distances.Y;
            target_diff.Z = local_target.Z - stopping_distances.Z;

            target_speed_nomolized = target_diff / target_diff.Length() * 400;

            target_diff.X = Math.Min(Math.Abs(target_diff.X), Math.Abs(target_speed_nomolized.X)) * Math.Sign(target_diff.X);
            target_diff.Y = Math.Min(Math.Abs(target_diff.Y), Math.Abs(target_speed_nomolized.Y)) * Math.Sign(target_diff.Y);
            target_diff.Z = Math.Min(Math.Abs(target_diff.Z), Math.Abs(target_speed_nomolized.Z)) * Math.Sign(target_diff.Z);

            drone.thrusters.velocity = target_diff;


            Echo(target_speed_nomolized.ToString());
            Echo(target_diff.ToString());
            Echo(stopping_distances.ToString());

        }
    }
}