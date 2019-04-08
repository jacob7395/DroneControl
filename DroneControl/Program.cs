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
        Vector3D current_target = new Vector3D();

        double alive_count;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            drone = new DroneControler(GridTerminalSystem);

            GoTo action = new GoTo(target1);
            action.Add_Point(target2);

            Task task = new Task();
            task.Add_Action(action);

            drone.set_task(task);

            alive_count = 0;

            current_target = target1;
        }

        MyDetectedEntityInfo collision_object;
        List<Vector3D> collision_corrners = new List<Vector3D>();
        Vector3D safe_coner = new Vector3D();
        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Alive Count " + alive_count.ToString());
            alive_count += 0.001;

            drone.run();

            IMyCameraBlock front_cam = GridTerminalSystem.GetBlockWithName("Front Camera") as IMyCameraBlock;

            front_cam.EnableRaycast = true;

            if (front_cam.CanScan(2000) && collision_object.IsEmpty())
            {
                collision_object = front_cam.Raycast(2000);

                if (!collision_object.IsEmpty())
                {
                    Vector3D[] cornners;
                    cornners = collision_object.BoundingBox.GetCorners();

                    foreach (Vector3D corner in cornners)
                        collision_corrners.Add(corner);
                }
            }
            
            if (collision_corrners.Count > 0)
                foreach (Vector3D corner in collision_corrners)
                {
                    if (front_cam.CanScan(corner))
                    {
                        MyDetectedEntityInfo corner_scan = front_cam.Raycast(corner);

                        if (corner_scan.IsEmpty())
                        {
                            safe_coner = corner;
                        }
                        else
                            collision_corrners.Remove(corner);
                        break;
                    }
                }
            

            Echo(collision_object.IsEmpty().ToString());
            Echo(safe_coner.ToString());
        }
    }
}