using IngameScript.DroneControl.utility;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using IngameScript.DroneControl.Systems;

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
        /// ship information is used withing the ship systems
        /// </summary>
        private ShipSystems systems;

        /// <summary>
        /// The thrusters will attempt to reach
        /// </summary>
        private Vector3D target_velocity = new Vector3D(0,0,0);

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
                MatrixD world_matrix = systems.controller.WorldMatrix;
                Vector3D world_velocity = systems.controller.GetShipVelocities().LinearVelocity;
                // translate from world to local velocity
                Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

                return local_velocity;
            }
            set
            {
                this.target_velocity = value;
            }
        }

        /// <summary>
        /// Calculates the stopping distance for every axis.
        /// </summary>
        public Vector3D stopping_distances
        {
            get
            {
                Vector3D stopping = new Vector3D();

                stopping.Z = this.StoppingDistance(direction: Orientation.Backward);
                stopping.Y = this.StoppingDistance(direction: Orientation.Up);
                stopping.X = this.StoppingDistance(direction: Orientation.Right);

                return stopping;
            }
        }

        public ThrusterControl(ShipSystems systems)
        {
            this.systems = systems;

            this.thrusters = SetupThrusters();
            // call the enable all thrusters method this is done to prevent thruster being left disabled
            EnableAllThrusers();
        }
        /// <summary>
        /// Setup the thrusters returning a dictionary sorted into direction and thrusters
        /// </summary>
        /// <returns>Thrusters ordered into a dictionary using the orientation enum as a key</returns>
        private IDictionary<Orientation, List<IMyThrust>> SetupThrusters()
        {
            List<IMyThrust> thrusters = new List<IMyThrust>();
            systems.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

            IDictionary<Orientation, List<IMyThrust>> ordered_thrusters = new Dictionary<Orientation, List<IMyThrust>>();
            // initialize list for all directions
            foreach (Orientation direction in Enum.GetValues(typeof(Orientation)))
                ordered_thrusters.Add(direction, new List<IMyThrust>());
            
            // group each thruster by orientation inside a dictionary
            foreach (IMyThrust thruster in thrusters)
            {
                // reset the thrust override
                thruster.ThrustOverridePercentage = 0.0f;

                Orientation thruster_orentation = systems.BlockOrentaion(thruster);
                // if the value matches add to thruster dictionary with the lookup key
                if (thruster_orentation != Orientation.None)
                {
                    thruster_orentation = thruster_orentation.inverse();
                    ordered_thrusters[thruster_orentation].Add(thruster);
                } 
            }
            return ordered_thrusters;
        }

        /// <summary>
        /// Calculates the distance required to stop the ship in a given direction.
        /// Will return negative if the stopping distance is reversed.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private double StoppingDistance(Orientation direction = Orientation.Forward)
        {
            // get the velocity in the given direction
            double directional_velocity = direction.CalcVelocity(this.velocity);

            double mass = systems.controller.CalculateShipMass().TotalMass;

            // if the velocity is negative reverse the direction
            if (directional_velocity > 0)
                direction = direction.inverse();

            // calculate the force needed to stop in the given direction
            double max_force_output = GetMaxDirectionalForce(direction);

            double distance = 0;
            if (max_force_output <= 0)
                distance = double.NegativeInfinity;
            else
                distance = Math.Pow(directional_velocity, 2) / (2 * max_force_output / mass) * Math.Sign(directional_velocity);

            // calculates the stopping distance then apply the sign from the velocity
            return distance;
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
        /// Calculates the force required to accelerate the ship at the given speed.
        /// </summary>
        /// <param name="acceleration">Default to FMax</param>
        /// <param name="direction">Default to forward</param>
        private void Apple_Acceleration(double acceleration = double.MaxValue, Orientation direction = Orientation.Forward)
        {
            double mass = systems.controller.CalculateShipMass().TotalMass;
            // call the force method
            ApplyForce(acceleration * mass, direction);
        }

        /// <summary>
        /// Apply the acceleration required to set the speed as indicated.
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

            return max_force * this.systems.max_thruster_force_percent;
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
        /// Will set the thruster override value to the percent given in the desired direction.
        /// </summary>
        /// <param name="percent">Override percent</param>
        /// <param name="direction">Direction to override</param>
        private void OverideThrusters(float percent = 0.0f, Orientation direction = Orientation.Forward)
        {
            foreach (IMyThrust thruster in thrusters[direction])
                thruster.ThrustOverridePercentage = percent;
        }

        public void run()
        {
            // update the ships systems
            this.systems.velocity = this.velocity;
            this.systems.stopping_distance = this.stopping_distances;


            this.SetVelocity(this.target_velocity.X, direction: Orientation.Right);
            this.SetVelocity(this.target_velocity.Y, direction: Orientation.Up);
            this.SetVelocity(this.target_velocity.Z, direction: Orientation.Backward);
        }
        
        /// <summary>
        /// Disables the thruster auto control, auto will be enabled again when a velocity is set
        /// </summary>
        public void DisableAuto()
        {
            systems.controller.DampenersOverride = true;
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in this.thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                {
                    thruster.Enabled = true;
                    thruster.ThrustOverride = 0;
                }
        }
    }
}
