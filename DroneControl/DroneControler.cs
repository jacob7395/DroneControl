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
        public ThrusterControl thrusters;

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
        /// Originaly from:
        /// https://forum.keenswh.com/threads/tutorial-how-to-do-vector-transformations-with-world-matricies.7399827/
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
        /// Method to calcualte the local position of a world pos.
        /// 
        /// Currently seems a bit flawed.
        /// 
        /// TODO find a better solution
        /// </summary>
        /// <param name="wolrd_pos"></param>
        /// <returns></returns>
        public Vector3D get_local_space(Vector3D wolrd_pos)
        {
            Vector3D referenceWorldPosition = this.orientation_block.WorldMatrix.Translation; //block.WorldMatrix.Translation is the same as block.GetPosition() btw

            //Convert worldPosition into a world direction
            Vector3D worldDirection = wolrd_pos - referenceWorldPosition; //this is a vector starting at the reference block pointing at your desired position

            //Convert worldDirection into a local direction
            Vector3D bodyPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(this.orientation_block.WorldMatrix));

            return bodyPosition;
        }

        /// <summary>
        /// Execute a GoTo action.
        /// </summary>
        /// <param name="Target"></param>
        private bool GoTo(Vector3D Target)
        {

            Vector3D target = this.get_local_space(Target);
            Vector3D stopping_distances = this.thrusters.stopping_distances;

            Vector3D target_diff = new Vector3D();
            Vector3D target_speed = new Vector3D();
            Vector3D target_speed_scaled = new Vector3D();

            target_diff = target - stopping_distances;

            target_speed_scaled = target_diff / target_diff.Length() * 400;

            target_speed.X = Math.Min(Math.Abs(target_diff.X), Math.Abs(target_speed_scaled.X)) * Math.Sign(target_diff.X);
            target_speed.Y = Math.Min(Math.Abs(target_diff.Y), Math.Abs(target_speed_scaled.Y)) * Math.Sign(target_diff.Y);
            target_speed.Z = Math.Min(Math.Abs(target_diff.Z), Math.Abs(target_speed_scaled.Z)) * Math.Sign(target_diff.Z);

            this.thrusters.velocity = target_speed;

            // aim the ship towards the objective
            if (target_diff.Length() > 10)
                this.gyros.OrientShip(Orientation.Forward, Target, this.orientation_block, gyro_power: 1, min_angle: 0.01f);
            else
                this.gyros.DisableAuto();

            return target_diff.Length() < 1 && this.thrusters.velocity.Length() < 0.1;
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
                DroneAction current_action;
                if (this.current_task != null)
                    current_action = this.current_task.Get_Next_Action();
                else
                    current_action = null;

                switch (current_action.get_type())
                {
                    case action_tpye.GoTo:
                        GoTo goto_action = current_action as GoTo;
                        Vector3D target = goto_action.Next_Point();

                        if (this.GoTo(target) == true)
                        {
                            if (goto_action.Complete() == false)
                                current_action = null;
                        }
                        break;
                }
            }
 
        }
    }
}
