using IngameScript.DroneControl.utility;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace IngameScript.DroneControl.thruster
{
    public enum Thruster_State
    {
        Accelerating,
        Deccelerating,
        Holding
    }

    public class ThrusterControl : IAutoControl
    {
        /// <summary>
        /// dictionary holding lists of thrusters with the direction as a key
        /// </summary>
        private IDictionary<Orientation, List<IMyThrust>> thrusters;
        /// <summary>
        /// reference to grid terminal system
        /// </summary>
        private IMyGridTerminalSystem GridTerminalSystem;
        /// <summary>
        /// reference for the control block
        /// </summary>
        private IMyShipController control_block;

        /// <summary>
        /// the max speed for the thrusters, this may be possible to set using the grid
        /// </summary>
        public const float MAX_SPEED = 400;

        /// <summary>
        /// Velocity is represented in local space where -Z is forward.
        /// 
        /// The set method will tell the thrusters to match the given values.
        /// </summary>
        /// <value>A Vectore3D of local velocity</value>
        public Vector3D velocity
        {
            get
            {
                MatrixD world_matrix = control_block.WorldMatrix;
                Vector3D world_velocity = control_block.GetShipVelocities().LinearVelocity;
                // translate from world to local velocity
                Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

                return local_velocity;
            }
            set
            {
                this.SetVelocity(value.X, direction: Orientation.Right);
                this.SetVelocity(value.Y, direction: Orientation.Up);
                this.SetVelocity(value.Z, direction: Orientation.Backward);
            }
        }

        public Vector3D stopping_distances
        {
            get
            {
                Vector3D stopping = new Vector3D();

                stopping.Z = this.stopping_distance(direction: Orientation.Backward);
                stopping.Y = this.stopping_distance(direction: Orientation.Up);
                stopping.X = this.stopping_distance(direction: Orientation.Right);

                return stopping;
            }
        }

        /// <summary>
        /// Calculates the distance required to stop the ship in a given direction.
        /// Will return negative if the stopping distance is reversed.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private double stopping_distance(Orientation direction = Orientation.Forward)
        {
            // get the velocity in the given direction
            double directional_velocity = direction.CalcVelocity(this.velocity);

            double mass = this.control_block.CalculateShipMass().TotalMass;

            // if the velocity is negative reverse the direction
            if (directional_velocity > 0)
                direction = direction.inverse();

            // calculate the force needed to stop in the given direction
            double max_force_output = GetMaxDirectionalForce(direction);

            // calculates the stopping distance then apply the sign from the velocity
            return Math.Pow(directional_velocity, 2) / (2 * max_force_output / mass) * Math.Sign(directional_velocity);
        }

        public ThrusterControl(IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock orientation_block, IMyShipController control_block)
        {
            this.GridTerminalSystem = GridTerminalSystem;
            thrusters = SetupThrusters(orientation_block);
            this.control_block = control_block;

            // call the enable all thrusters method this is done to prevent thruster being left disabled
            EnableAllThrusers();
        }
        /// <summary>
        /// Setup the thrusters returning a dict sorted into direction and thrusters
        /// </summary>
        /// <returns>Thrusters ordered into a dict using the orientation enum as a key</returns>
        /// <param name="orientation_block">Orientation block.</param>
        private IDictionary<Orientation, List<IMyThrust>> SetupThrusters(IMyTerminalBlock orientation_block)
        {

            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

            IDictionary<Orientation, List<IMyThrust>> ordered_thrusters = new Dictionary<Orientation, List<IMyThrust>>();
            // init list for all directions
            foreach (Orientation direction in Enum.GetValues(typeof(Orientation)))
                ordered_thrusters.Add(direction, new List<IMyThrust>());

            Matrix relative_matrix = new Matrix(), thruster_matrix = new Matrix();

            orientation_block.Orientation.GetMatrix(out relative_matrix);

            // lookup the table to translate from orientation vector to orientation enum
            IDictionary<Orientation, Vector3I> orientation_lookup = new Dictionary<Orientation, Vector3I>
            {
                { Orientation.Up, new Vector3I(0, -1, 0) },
                { Orientation.Down, new Vector3I(0, 1, 0) },
                { Orientation.Left, new Vector3I(1, 0, 0) },
                { Orientation.Right, new Vector3I(-1, 0, 0) },
                { Orientation.Forward, new Vector3I(0, 0, 1) },
                { Orientation.Backward, new Vector3I(0, 0, -1) }
            };

            // group each thruster by orientation inside a dict
            foreach (IMyThrust thruster in thrusters)
            {
                // reset the thrust override
                thruster.ThrustOverridePercentage = 0.0f;
                // get the orientation matrix
                thruster.Orientation.GetMatrix(out thruster_matrix);
                // loop through the lookup dict attempting to match the value
                foreach (KeyValuePair<Orientation, Vector3I> lookup in orientation_lookup)
                {
                    // if the value matches add to thruster dict with the lookup key
                    if (thruster.GridThrustDirection == lookup.Value)
                    {
                        ordered_thrusters[lookup.Key].Add(thruster);
                        // now match has been found break out of lookup
                        break;
                    }
                }
            }

            return ordered_thrusters;
        }

        /// <summary>
        /// Will apply a force evenly over all the thrusters in a direction. Thrusters will be set to max output if force is to large.
        /// The method currently assumes all thrusters in one direction can apply the same force (they are the same size).
        /// </summary>
        /// TODO update method to account for different thruster.
        /// <param name="force">Force in newtons (N) to apply.</param>
        /// <param name="direction">Direction force should be applied.</param>
        private void ApplyForce(double force, Orientation direction = Orientation.Forward)
        {
            //List<Orientation> disable = new List<Orientation>();
            //Orientation inverce_direction = direction.inverse();
            //disable.Add(inverce_direction);
            //DisableThrusters(disable);

            OverideThrusters(0, direction);
            OverideThrusters(0, direction.inverse());

            if (force < 0)
            {
                direction = direction.inverse();
                force *= -1;
            }

            // get the number of thrusters
            float number_of_thrusters = thrusters[direction].Count();

            // calculate the max force output
            double max_force = GetMaxDirectionalForce(direction);

            // set the force to max if to large
            force = Math.Min(force, max_force);

            // split the force over the number of thrusters
            force /= number_of_thrusters;

            // apply the force
            foreach (IMyThrust thruster in thrusters[direction])
            {
                thruster.ThrustOverride = (float)force;
                thruster.Enabled = true;
            }
        }

        /// <summary>
        /// Caculates the force required to accelerate the ship at the given speed.
        /// </summary>
        /// <param name="acceleration">Default to fmax</param>
        /// <param name="direction">Default to foward</param>
        private void Apple_Acceleration(double acceleration = double.MaxValue, Orientation direction = Orientation.Forward)
        {
            double mass = this.control_block.CalculateShipMass().TotalMass;
            // call the force method
            ApplyForce(acceleration * mass, direction);
        }

        /// <summary>
        /// Apply the acceleration requred to set the speed as indicated.
        /// </summary>
        /// <param name="target">The speed target</param>
        /// <param name="direction">The direction to set velocity</param>
        private void SetVelocity(double target, Orientation direction = Orientation.Forward)
        {
            double directional_velocity = direction.CalcVelocity(this.velocity);
            double acceleration_required = target - directional_velocity;

            Apple_Acceleration(acceleration_required, direction);
        }

        /// <summary>
        /// Given a direction return the max force output for that direction
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private double GetMaxDirectionalForce(Orientation direction)
        {
            double max_force = 0;
            foreach (IMyThrust thruster in thrusters[direction])
                max_force += thruster.MaxThrust;

            return max_force;
        }

        /// <summary>
        /// Sets all thrusters to enabled
        /// </summary>
        private void EnableAllThrusers()
        {
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in this.thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                    thruster.Enabled = true;
        }

        /// <summary>
        /// Will set the thruster overide value to the percent given in the desired direction.
        /// </summary>
        /// <param name="percent">Overide percent</param>
        /// <param name="direction">Direction to overide</param>
        private void OverideThrusters(float percent = 0.0f, Orientation direction = Orientation.Forward)
        {
            foreach (IMyThrust thruster in thrusters[direction])
                thruster.ThrustOverridePercentage = percent;
        }
        
        /// <summary>
        /// Disables the thruster auto control, auto will be enabled again when a velocity is set
        /// </summary>
        public void DisableAuto()
        {
            control_block.DampenersOverride = true;
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in this.thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                {
                    thruster.Enabled = true;
                    thruster.ThrustOverride = 0;
                }
        }
    }


}
