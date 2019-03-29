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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using IngameScript.DroneControl.gyro;
using IngameScript.DroneControl.utility;
using IngameScript.DroneControl.thruster;

namespace IngameScript.DroneControl
{

    public class DroneControler : IAutoControl
    {

        private GyroControl gyros;
        private ThrusterControl thrusters;

        private IMyGridTerminalSystem GridTerminalSystem;
        private IMyRemoteControl controller;

        private Task current_task;

        public DroneControler(IMyGridTerminalSystem GridTerminalSystem)
        {
            IMyRemoteControl controler = GridTerminalSystem.GetBlockWithName("Controler") as IMyRemoteControl;
            List<IMyRemoteControl> remote_controlers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remote_controlers);
            foreach (IMyRemoteControl control in remote_controlers)
            {
                controler = control;
                break;
            }

            this.controller = controler;
            this.gyros = new GyroControl(GridTerminalSystem);
            this.thrusters = new ThrusterControl(GridTerminalSystem, GridTerminalSystem.GetBlockWithName("Control Unit"), controler);
            this.GridTerminalSystem = GridTerminalSystem;
        }

        public void add_task(string task)
        {
            this.DisableAuto();

            this.current_task = new Task(task);

        }

        public void DisableAuto()
        {
            this.gyros.DisableAuto();
            this.thrusters.DisableAuto();
        }

        public void run()
        {
            double speed = 400;
            double approch = 2.5;
            double stopping_margin = 1.01;

            Matrix orientation_matrix;
            this.controller.Orientation.GetMatrix(out orientation_matrix);

            List<MyWaypointInfo> waypoint_info = new List<MyWaypointInfo>();
            this.controller.GetWaypointInfo(waypoint_info);

            Vector3D target = Vector3D.Zero;
            foreach (MyWaypointInfo waypoint in waypoint_info)
                target = waypoint.Coords;

            List<IMyShipConnector> ship_connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ship_connectors);

            MatrixD world_matrix = this.controller.WorldMatrix;
            Vector3D world_velocity = this.controller.GetShipVelocities().LinearVelocity;

            Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

            bool bAimed = this.gyros.OrientShip(Orientation.Forward, target, ship_connectors[0], gyro_power: 1, min_angle: 0.25f);

            double stopping_distance = this.thrusters.stopping_distance();

            Vector3D worldPosition = this.controller.GetPosition();
            double distance_to_target = Vector3D.Distance(worldPosition, target);

            this.thrusters.DisableAuto();
            if (distance_to_target < (stopping_distance * stopping_margin) || distance_to_target < approch)
            {
                this.thrusters.all_stop();
                this.gyros.DisableAuto();
            }
            else if (bAimed)
            {
                this.thrusters.SetVelocity(speed);
            }
            else
            {
                this.thrusters.DisableAllThrusers();
            }
        }
    }
}
