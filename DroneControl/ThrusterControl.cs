using IngameScript.DroneControl.utility;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace IngameScript.DroneControl.thruster
{
    public enum velocity_state
    {
        Accelerating,
        Deccelerating,
        Holding
    }

    public class ThrusterControl : IAutoControl
    {
        public IDictionary<Orientation, List<IMyThrust>> thrusters;
        private IMyGridTerminalSystem GridTerminalSystem;
        public IMyShipController control_block;

        private const float MAX_FORCE = 5e20f;
        public const float MAX_SPEED = 400;

        public Vector3D velocity
        {
            get
            {
                //TODO remove duplicate code with droneControler
                MatrixD world_matrix = control_block.WorldMatrix;
                Vector3D world_velocity = control_block.GetShipVelocities().LinearVelocity;

                Vector3D local_velocity = Vector3D.TransformNormal(world_velocity, MatrixD.Transpose(world_matrix));

                return local_velocity;
            }
            set
            {
                this.SetVelocity(value.X, direction : Orientation.Right);
                this.SetVelocity(value.Y, direction : Orientation.Up);
                this.SetVelocity(value.Z, direction : Orientation.Backward);
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

        public double stopping_distance(Orientation direction = Orientation.Forward)
        {
            bool invert = false;

            double directional_velocity = direction.CalcVelocity(this.velocity);

            double mass = this.control_block.CalculateShipMass().TotalMass;

            if (directional_velocity > 0)
            {
                direction = direction.inverse();
            }

            double max_force_output = GetMaxDirectionalForce(direction);

            double distance;

            distance = Math.Pow(directional_velocity, 2) / (2 * max_force_output / mass) * Math.Sign(directional_velocity);

            return distance;
        }

        public ThrusterControl(IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock orientation_block, IMyShipController control_block)
        {
            this.GridTerminalSystem = GridTerminalSystem;
            thrusters = SetupThrusters(orientation_block);
            this.control_block = control_block;

            // call the enable all thrusers method this is done to prevent thruster being left disabled
            EnableAllThrusers();
        }
        /// <summary>
        /// Setsup the thrusters returning a dict orderd into direction and thrusters
        /// </summary>
        /// <returns>Thrusters orderd into a dict using the orientaion enum as a key</returns>
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

            // lookup the table to translate from orientation vecter to orientation enum
            IDictionary<Orientation, Vector3I> orientation_lookup = new Dictionary<Orientation, Vector3I>
            {
                { Orientation.Up, new Vector3I(0, -1, 0) },
                { Orientation.Down, new Vector3I(0, 1, 0) },
                { Orientation.Left, new Vector3I(1, 0, 0) },
                { Orientation.Right, new Vector3I(-1, 0, 0) },
                { Orientation.Forward, new Vector3I(0, 0, 1) },
                { Orientation.Backward, new Vector3I(0, 0, -1) }
            };

            // group each thruster by orientaion inside a dict
            foreach (IMyThrust thruster in thrusters)
            {
                // reset the thruset overide
                thruster.ThrustOverridePercentage = 0.0f;
                // get the orientation matrix
                thruster.Orientation.GetMatrix(out thruster_matrix);
                // loop through the looup dict attmpting to match the value
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

        private void EnableAllThrusers()
        {
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in this.thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                    thruster.Enabled = true;
        }


        private void OverideThrusters(float percent = 0.0f , Orientation direction = Orientation.Forward)
        {
            foreach (IMyThrust thruster in thrusters[direction])
                thruster.ThrustOverridePercentage = percent;
        }

        /// <summary>
        /// Will apply a force eveanly over all the thrusters in a direction. Thrusers will be set to max output if force is to large.
        /// The method currently assumes all thruseters in one direction can apply the same force (they are the same size).
        /// </summary>
        /// TODO update method to account for diffrent thruster.
        /// <param name="force">Force in newtowns (N) to apply.</param>
        /// <param name="direction">Direction force should be applyed.</param>
        public void ApplyForce(double force, Orientation direction = Orientation.Forward)
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

            // get the number of throusets
            float number_of_thrusters = thrusters[direction].Count();

            // claculate the max force output
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

        public void Apple_Acceleration(double acceleration = double.MaxValue, Orientation direction = Orientation.Forward)
        {
            double mass = this.control_block.CalculateShipMass().TotalMass;

            ApplyForce(acceleration * mass, direction);
        }

        public void SetVelocity(double target, Orientation direction = Orientation.Forward)
        {
            double directional_velocity = direction.CalcVelocity(this.velocity);
            double acceleration_required = target - directional_velocity;

            Apple_Acceleration(acceleration_required, direction);
        }

        public double GetMaxDirectionalForce(Orientation direction)
        {
            double max_force = 0;
            foreach (IMyThrust thruster in thrusters[direction])
                max_force += thruster.MaxThrust;

            return max_force;
        }

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
