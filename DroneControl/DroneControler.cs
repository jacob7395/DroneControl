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
using IngameScript.DroneControl.utility.task;
using IngameScript.DroneControl.thruster;

namespace IngameScript.DroneControl
{

    /// <summary>
    /// Class that implements control over a ship.
    /// </summary>
    public class DroneControler : IAutoControl
    {

        private GyroControl gyros;
        private ThrusterControl thrusters;

        private IMyGridTerminalSystem GridTerminalSystem;
        private IMyRemoteControl controller;

        private Task current_task = null;

        private IMyTerminalBlock orientation_block;

        private double max_speed = 400;

        /// <summary>
        /// Initalise the control groups and wait for a task to be given.
        /// </summary>
        /// <param name="GridTerminalSystem"></param>
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

            // setup the oriantation block to a ship connector or a controler if one was not found
            List<IMyShipConnector> ship_connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ship_connectors);
            if (ship_connectors.Count > 0)
                this.orientation_block = ship_connectors[0];
            else
                this.orientation_block = controller;
        }

        /// <summary>
        /// Sets the task to the given object.
        /// </summary>
        /// <param name="task"></param>
        public void set_task(Task task)
        {
            this.DisableAuto();

            this.current_task = task;
        }

        /// <summary>
        /// This disables all the ship auto control for the ship.
        /// </summary>
        public void DisableAuto()
        {
            this.gyros.DisableAuto();
            this.thrusters.DisableAuto();
        }

        /// <summary>
        /// Get the ships position
        /// </summary>
        /// <returns></returns>
        public Vector3D current_location()
        {
            return this.controller.GetPosition();
        }

        /// <summary>
        /// uses world matix to get the local ship velocity.
        /// This was taken from a forum but unfochantly I have lost the artical.
        /// </summary>
        /// <returns>Ship velocity in local space</returns>
        public Vector3D get_local_velocity()
        {
            MatrixD world_matrix = this.controller.WorldMatrix;
            Vector3D world_velocity = this.controller.GetShipVelocities().LinearVelocity;

            Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

            return local_velocity;
        }

        /// <summary>
        /// Execute a GoTo action.
        /// </summary>
        /// <param name="location"></param>
        private void GoTo(Vector3D location)
        {
            double approch = 2.5;
            double stopping_margin = 1.01;

            // aim the ship towards the objective
            bool bAimed = this.gyros.OrientShip(Orientation.Forward, location, this.orientation_block, gyro_power: 1, min_angle: 0.25f);

            double stopping_distance = this.thrusters.stopping_distance();

            // get the world position and the position to target
            Vector3D worldPosition = this.controller.GetPosition();
            double distance_to_target = Vector3D.Distance(worldPosition, location);

            // simple algerithm to go to a target location
            this.thrusters.DisableAuto();
            if (distance_to_target < (stopping_distance * stopping_margin) || distance_to_target < approch)
            {
                this.thrusters.all_stop();
                this.gyros.DisableAuto();
            }
            else if (bAimed)
            {
                this.thrusters.SetVelocity(this.max_speed);
            }
            else
            {
                this.thrusters.DisableAllThrusers();
            }
        }

        /// <summary>
        /// Attempts to execute the current task.
        /// 
        /// Will idle if no task is avalible.
        /// </summary>
        public void run()
        {
            
            if(this.current_task != null)
            {
                DroneAction current_action = this.current_task.Get_Next_Action();

                switch (current_action.get_type())
                {
                    case action_tpye.GoTo:
                        GoTo goto_action = current_action as GoTo;
                        Vector3D target = goto_action.Next_Point();
                        this.GoTo(target);
                        break;
                }
            }
 
        }
    }
}
