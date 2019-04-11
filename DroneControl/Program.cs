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
        Vector3D target1 = new Vector3D(37.64, 73.47, -186.15);
        Vector3D target2 = new Vector3D(-19.5, -105.5, -59.5);
        Vector3D target3 = new Vector3D(-28.68, -435.98, 829.41);
        Task task = new Task();


        double alive_count;

        public Program()
        {
            // run the program per tick
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            drone = new DroneControler(GridTerminalSystem);

            // setup the GoTo action
            GoTo action = new GoTo(target3);
            action.Add_Point(target2);
            action.Add_Point(target1);

            // create the task with the action
            
            task.Add_Action(action);

            // pass the task to our drone
            drone.set_task(task);

            alive_count = 0;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Alive Count " + alive_count.ToString());
            alive_count += (0.0166666); // there are 60 ticks per simulation second

            drone.run();

            Vector3D next = drone.systems.safe_point;
            Echo(next.ToString());
            
            GoTo action = drone.current_task.Get_Next_Action() as GoTo;
            Echo(action.Next_Point().ToString());
        }
    }
}