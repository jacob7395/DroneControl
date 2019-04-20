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
using IngameScript.Drone.gyro;
using IngameScript.Drone.utility;
using IngameScript.Drone.utility.task;
using IngameScript.Drone.thruster;
using IngameScript.Drone.Camera;
using IngameScript.Drone.Systems;

namespace IngameScript.Drone.Controller
{
    /// <summary>
    /// Class that implements control over a ship.
    /// </summary>
    public class DroneControler : DroneBase, IAutoControl
    {
        private IDictionary<Orientation, List<CameraAgent>> cameras;
        private GyroControl gyros;
        private ThrusterControl thrusters;

        public Task current_task = null;

        /// <summary>
        /// Initialize the control groups and wait for a task to be given.
        /// </summary>
        /// <param name="GridTerminalSystem"></param>
        public DroneControler(IMyGridTerminalSystem GridTerminalSystem)
        {
            this.defaultInit(GridTerminalSystem);

            this.gyros = new GyroControl(this.systems);
            this.thrusters = new ThrusterControl(this.systems);
            this.cameras = new Dictionary<Orientation, List<CameraAgent>>();
            // initialize list for all directions
            foreach (Orientation direction in Enum.GetValues(typeof(Orientation)))
                this.cameras.Add(direction, new List<CameraAgent>());

            // setup camera agents
            List<IMyCameraBlock> cams = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cams);

            foreach (IMyCameraBlock cam in cams)
            {
                // initialize the camera object
                CameraAgent temp_agent = new CameraAgent(cam, this.systems);

                Orientation cam_orentation = systems.BlockOrentaion(cam);
                // if the value matches add to camera dictionary with the lookup key
                if (cam_orentation != Orientation.None)
                {
                    this.cameras[cam_orentation].Add(temp_agent);
                    break;
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
            Vector3D referenceWorldPosition = systems.orientation_block.WorldMatrix.Translation;

            //Convert worldPosition into a world direction
            Vector3D worldDirection = wolrd_pos - referenceWorldPosition;

            //Convert worldDirection into a local direction
            Vector3D bodyPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(systems.orientation_block.WorldMatrix));

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

            target_speed_scaled = target_diff / target_diff.Length() * systems.max_speed;


            target_speed.X = Math.Min(Math.Abs(target_diff.X), Math.Abs(target_speed_scaled.X)) * Math.Sign(target_diff.X);
            target_speed.Y = Math.Min(Math.Abs(target_diff.Y), Math.Abs(target_speed_scaled.Y)) * Math.Sign(target_diff.Y);
            target_speed.Z = Math.Min(Math.Abs(target_diff.Z), Math.Abs(target_speed_scaled.Z)) * Math.Sign(target_diff.Z);

            this.thrusters.velocity = target_speed;

            // aim the ship towards the objective
            if (target_diff.Length() > 10)
                this.gyros.target = Target;
            else
                this.gyros.DisableAuto();

            return target_diff.Length() < 1 && this.thrusters.velocity.Length() < 0.1;
        }

        /// <summary>
        /// Attempts to execute the current task.
        /// 
        /// Will idle if no task is available.
        /// </summary>
        public override void run()
        {
            this.thrusters.run();
            this.gyros.Run();

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
                        }

                        // ToDo integrate the task with the system object a controlled put and get with safe point
                        if (this.systems.safe_point != systems.DEFAULT_SAFE_POINT)
                            goto_action.Add_Point(this.systems.safe_point);

                        if (this.GoTo(goto_action.Next_Point()) == true)
                        {
                            // reset the safe point
                            this.systems.safe_point = systems.DEFAULT_SAFE_POINT;
                            // tell the task the action is complete, then check if the task is complete
                            if (goto_action.Complete() == false)
                                this.current_task = null;
                        }
                        break;
                }
            }
        }
    }
}
