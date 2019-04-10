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
using IngameScript.DroneControl.Camera;
using IngameScript.DroneControl.Systems;

namespace IngameScript.DroneControl
{
    /// <summary>
    /// Class that implements control over a ship.
    /// </summary>
    public class DroneControler : IAutoControl
    {
        private ShipSystems systems;
        private IDictionary<Orientation, List<CameraAgent>> cameras;
        private GyroControl gyros;
        private ThrusterControl thrusters;

        private Task current_task = null;

        private IMyTerminalBlock orientation_block;

        private double max_speed = 400;

        /// <summary>
        /// Initialize the control groups and wait for a task to be given.
        /// </summary>
        /// <param name="GridTerminalSystem"></param>
        public DroneControler(IMyGridTerminalSystem GridTerminalSystem)
        {
            // get the main remote controller being used
            IMyRemoteControl controler = GridTerminalSystem.GetBlockWithName("Controler") as IMyRemoteControl;
            List<IMyRemoteControl> remote_controlers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remote_controlers);
            foreach (IMyRemoteControl control in remote_controlers)
            {
                controler = control;
                break;
            }

            // setup the ship systems
            this.systems = new ShipSystems(GridTerminalSystem, systems.controller);

            // setup the orientation block to a ship connector or a controller if one was not found
            List<IMyShipConnector> ship_connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ship_connectors);
            if (ship_connectors.Count > 0)
                this.orientation_block = ship_connectors[0];
            else
                this.orientation_block = systems.controller;

            this.gyros = new GyroControl(this.systems);
            this.thrusters = new ThrusterControl(this.systems, GridTerminalSystem.GetBlockWithName("Control Unit"));
           
            this.cameras = new Dictionary<Orientation, List<CameraAgent>>();
            // initialize list for all directions
            foreach (Orientation direction in Enum.GetValues(typeof(Orientation)))
                this.cameras.Add(direction, new List<CameraAgent>());

            // lookup the table to translate from orientation vector to orientation enum
            IDictionary<Orientation, Vector3I> orientation_lookup = new Dictionary<Orientation, Vector3I>
            {
                { Orientation.Up, new Vector3I(0, 1, 0) },
                { Orientation.Down, new Vector3I(0, -1, 0) },
                { Orientation.Left, new Vector3I(-1, 0, 0) },
                { Orientation.Right, new Vector3I(1, 0, 0) },
                { Orientation.Forward, new Vector3I(0, 0, -1) },
                { Orientation.Backward, new Vector3I(0, 0, 1) }
            };

            // setup camera agents
            List<IMyCameraBlock> cams = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cams);

            Matrix cam_matrix = new Matrix();
            foreach (IMyCameraBlock cam in cams)
            {
                // initialize the camera object
                CameraAgent temp_agent = new CameraAgent(cam, this.systems);

                cam.Orientation.GetMatrix(out cam_matrix);
                // loop through the lookup dictionary attempting to match the value
                foreach (KeyValuePair<Orientation, Vector3I> lookup in orientation_lookup)
                {
                    // if the value matches add to camera dictionary with the lookup key
                    if (cam_matrix.Forward == lookup.Value)
                    {
                        this.cameras[lookup.Key].Add(temp_agent);
                        break;
                    }
                }
            }
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
            return systems.controller.GetPosition();
        }

        /// <summary>
        /// uses world matrix to get the local ship velocity.
        /// Original from:
        /// https://forum.keenswh.com/threads/tutorial-how-to-do-vector-transformations-with-world-matricies.7399827/
        /// </summary>
        /// <returns>Ship velocity in local space</returns>
        public Vector3D get_local_velocity()
        {
            MatrixD world_matrix = systems.controller.WorldMatrix;
            Vector3D world_velocity = systems.controller.GetShipVelocities().LinearVelocity;

            Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

            return local_velocity;
        }

        /// <summary>
        /// Method to calculate the local position of a world position.
        /// 
        /// Currently seems a bit flawed.
        /// 
        /// TODO find a better solution
        /// </summary>
        /// <param name="wolrd_pos"></param>
        /// <returns></returns>
        public Vector3D get_local_space(Vector3D wolrd_pos)
        {
            //block.WorldMatrix.Translation is the same as block.GetPosition()
            Vector3D referenceWorldPosition = this.orientation_block.WorldMatrix.Translation;

            //Convert worldPosition into a world direction
            Vector3D worldDirection = wolrd_pos - referenceWorldPosition;

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

            target_speed_scaled = target_diff / target_diff.Length() * max_speed;

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
        /// Will idle if no task is available.
        /// </summary>
        public void run()
        {
            if (this.current_task != null)
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

                        //run the cameras
                        foreach (CameraAgent cam in this.cameras[Orientation.Forward])
                        {
                            cam.Run();

                            if (cam.safe_point_avalible)
                            {
                                goto_action.Add_Point(cam.GetSafePoint());
                            }
                        }
                        
                        if (this.GoTo(goto_action.Next_Point()) == true)
                        {
                            if (goto_action.Complete() == false)
                                this.current_task = null;
                        }
                        break;
                }
            }
        }
    }
}
