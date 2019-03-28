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
using IngameScript.gyro;
using IngameScript.utility;
using IngameScript.thruster;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461
        // This code has been refacted from the souce, the maths remains the same

        GyroControl GyroCon;
        ThrusterControl Thrusters;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            IMyRemoteControl controler = GridTerminalSystem.GetBlockWithName("Controler") as IMyRemoteControl;
            List<IMyRemoteControl> remote_controlers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remote_controlers);
            foreach (IMyRemoteControl control in remote_controlers)
            {
                controler = control;
                break;
            }

            GyroCon = new GyroControl(GridTerminalSystem);
            Thrusters = new ThrusterControl(GridTerminalSystem, GridTerminalSystem.GetBlockWithName("Control Unit"), controler);
        }

        Orientation direction = Orientation.Forward;
        double speed = 5;
        double approch = 10;
        bool stop = false;
        public void Main(string argument, UpdateType updateSource)
        {

            List<IMyRemoteControl> remote_controlers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remote_controlers);
            foreach (IMyRemoteControl control in remote_controlers)
            {
                Matrix orientation_matrix;
                control.Orientation.GetMatrix(out orientation_matrix);

                List<MyWaypointInfo> waypoint_info = new List<MyWaypointInfo>();
                control.GetWaypointInfo(waypoint_info);

                Vector3D target = Vector3D.Zero;
                foreach (MyWaypointInfo waypoint in waypoint_info)
                    target = waypoint.Coords;

                List<IMyShipConnector> ship_connectors = new List<IMyShipConnector>();
                GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ship_connectors);

                MatrixD world_matrix = control.WorldMatrix;
                Vector3D world_velocity = control.GetShipVelocities().LinearVelocity;

                Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

                bool bAimed = GyroCon.OrientShip(Orientation.Forward, target, ship_connectors[0], gyro_power:1 ,min_angle : 0.25f);

                double stopping_distance = Thrusters.stopping_distance(direction);

                Vector3D worldPosition = control.GetPosition();
                double distance_to_target = Vector3D.Distance(worldPosition, target);

                Echo(distance_to_target.ToString());
                
                if (distance_to_target < stopping_distance || distance_to_target < approch)
                {
                    Thrusters.all_stop();
                    Echo("Stopping");
                }
                else if (bAimed)
                {
                    Thrusters.SetVelocity(speed);
                    Echo("Approching");
                }
                else
                {
                    Thrusters.DisableAllThrusers();
                    Echo("Aiming");
                }
                    
                
                break;
            }
        }
    }
}