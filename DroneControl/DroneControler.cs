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
        /// <param name="location"></param>
        private void GoTo(Vector3D location)
        {
            double approch = 2.5;
            double stopping_margin = 1.01;

            // aim the ship towards the objective
            bool bAimed = this.gyros.OrientShip(Orientation.Forward, location, this.orientation_block, gyro_power: 1, min_angle: 0.25f);

            Vector3D stopping_distances = this.thrusters.stopping_distances;

            // get the world position and the position to target
            Vector3D target_grid = this.get_local_space(location);


            if (target_grid.X > 0)
            {

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
