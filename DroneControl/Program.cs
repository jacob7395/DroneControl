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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region Autogyro 
        // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461
        // This code has been refacted from the souce, the maths remains the same

        /// <summary>
        /// Enum used to represent orientation in six directions
        /// </summary>
        public enum Orientation
        {
            Up = 0,
            Down = 1,
            Backward = 2,
            Forward = 3,
            Right = 4,
            Left = 5
        }

        /// <summary>
        /// Try to align the ship/grid with the given vector. Returns true if the ship is within minAngleRad of being aligned
        /// </summary>
        /// <param name="direction">The direction to point. ("Up", "Down", "Forward"...)</param>
        /// <param name="target">The vector for the aim.</param>
        /// <param name="orientation_block">The terminal block to use for orientation</param>
        /// <param name="gyro_power">The power usage between 0..1 defaults to 0.9</param>
        /// <param name="min_angle">How tight to maintain aim in degress. Lower is tighter. Default is 5.0f</param>
        /// <returns>true if aligned. Meaning the angle of error is less than minAngleRad</returns>
        bool OrientShip(Orientation direction,
                        Vector3D target,
                        IMyTerminalBlock orientation_block,
                        double gyro_power = 0.9,
                        float min_angle = 5.0f)
        {
            // get the position of the orientaion block
            Vector3D location = orientation_block.GetPosition();
            // return argument used to indicate the min_angle_rad has been met
            bool aligned = true;
            // get all gyros being used for rotation
            List<IMyGyro> gyros = gyrosetup();
            // convert form degress to rads
            double min_angle_rad = (180 / Math.PI) * min_angle;

            Matrix orientation_matrix;
            orientation_block.Orientation.GetMatrix(out orientation_matrix);

            // switch the correction setting the down matrix, this is how the orientraion direction is controled
            Vector3D down;
            switch (direction)
            {
                case Orientation.Up:
                    down = orientation_matrix.Up;
                    break;
                case Orientation.Down:
                    down = orientation_matrix.Up;
                    break;
                case Orientation.Backward:
                    down = orientation_matrix.Backward;
                    break;
                case Orientation.Forward:
                    down = orientation_matrix.Forward;
                    break;
                case Orientation.Right:
                    down = orientation_matrix.Right;
                    break;
                case Orientation.Left:
                    down = orientation_matrix.Left;
                    break;
                default:
                    down = orientation_matrix.Down;
                    break;
            }

            // do some magic beam riding, not sure exacly what this does
            Vector3D beam = BeamRider(location, target, orientation_block);
            beam.Normalize();

            // calculated outside the for loop for efficincy
            var localCurrent = Vector3D.Transform(down, MatrixD.Transpose(orientation_matrix));

            // applys maths that I dont quite understand but it works
            // creadit goes to link given above
            foreach (IMyGyro gyro in gyros)
            {
                gyro.Orientation.GetMatrix(out orientation_matrix);

                var localTarget = Vector3D.Transform(beam, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                var rot = Vector3D.Cross(localCurrent, localTarget);
                double dot = Vector3D.Dot(localCurrent, localTarget);
                double ang = rot.Length();

                if (dot < 0)
                    ang = Math.PI - ang; // compensate for >+/-90
                else
                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));

                // check if the gycro is pointing at the target
                if (ang < min_angle_rad)
                {
                    gyro.GyroOverride = false;
                    continue;
                }

                float yaw_max = (float)(2 * Math.PI);

                double ctrl_vel = yaw_max * (ang / Math.PI) * gyro_power;

                ctrl_vel = Math.Min(yaw_max, ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel);
                rot.Normalize();
                rot *= ctrl_vel;

                gyro.Pitch = -(float)rot.X;
                gyro.Yaw = -(float)rot.Y;
                gyro.Roll = -(float)rot.Z;

                gyro.GyroOverride = true;

                aligned = false;
            }
            return aligned;
        }


        /// <summary>
        /// Initialize the gyro controls.
        /// </summary>
        /// <returns>String representing what was initialized</returns>
        List<IMyGyro> gyrosetup()
        {

            List<IMyGyro> gyros = new List<IMyGyro>();

            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);

            foreach (IMyGyro gyro in gyros)
            {
                gyro.Enabled = true;
            }

            return gyros;
        }

        /// <summary>
        /// Turns off all overrides on controlled Gyros
        /// </summary>
        void gyrosOff(List<IMyGyro> gyros)
        {
            if (gyros != null)
            {
                for (int i1 = 0; i1 < gyros.Count; ++i1)
                {
                    //gyros[i].SetValueBool("Override", false);
                    gyros[i1].GyroOverride = false;
                    gyros[i1].Enabled = true;
                }
            }
        }

        /// <summary>
        /// Draws a line between two vectors returning the corection required to align.
        /// </summary>
        /// <returns>The rider.</returns>
        /// <param name="start">V start.</param>
        /// <param name="end">V end.</param>
        /// <param name="orientation_block">Orientation block.</param>
        Vector3D BeamRider(Vector3D start, Vector3D end, IMyTerminalBlock orientation_block)
        {
            Vector3D bore_end = (end - start);
            Vector3D position;
            if (orientation_block is IMyShipController)
            {
                position = ((IMyShipController)orientation_block).CenterOfMass;
            }
            else
            {
                position = orientation_block.GetPosition();
            }
            Vector3D aim_end = (end - position);
            Vector3D reject_end = VectorRejection(bore_end, aim_end);

            Vector3D corrected_aim = (end - reject_end * 2) - position;

            return corrected_aim;
        }

        /// <summary>
        /// I don't know what this does, but assume its needed.
        /// Appears to correct for something.
        /// </summary>
        /// <returns>The rejection.</returns>
        Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
        {
            if (Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        #endregion

        #region ThrusterControl

        /// <summary>
        /// Setsup the thrusters returning a dict orderd into direction and thrusters
        /// </summary>
        /// <returns>Thrusters orderd into a dict using the orientaion enum as a key</returns>
        /// <param name="orientation_block">Orientation block.</param>
        private IDictionary<Orientation, List<IMyThrust>> SetupThruster(IMyTerminalBlock orientation_block)
        {

            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

            IDictionary<Orientation, List<IMyThrust>> ordered_thrusters = new Dictionary<Orientation, List<IMyThrust>>();
            // init list for all directions
            foreach (Orientation direction in Enum.GetValues(typeof(Orientation)))
                ordered_thrusters.Add(direction, new List<IMyThrust>());

            Matrix relative_matrix = new Matrix(), thruster_matrix = new Matrix();

            orientation_block.Orientation.GetMatrix(out relative_matrix);


            IDictionary<Orientation, Vector3I> orientation_lookup = new IDictionary<Orientation, Vector3I>
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
                        // sets the custom name for the thruster to the direction
                        thruster.CustomName = "Thruster "+lookup.Key.ToString();
                        // now match has been found break out of lookup
                        break;
                    }
                }
            }

            // call the enable all thrusers method this is done to prevent thruster being left disabled
            EnableAllThrusers(ref ordered_thrusters);

            return ordered_thrusters;
        }

        private void EnableThrusters(List<Orientation> directions, ref IDictionary<Orientation, List<IMyThrust>> thrusters)
        {
            foreach (Orientation direction in directions)
                foreach (IMyThrust thruster in thrusters[direction])
                    thruster.Enabled = true;
        }

        private void EnableAllThrusers(ref IDictionary<Orientation, List<IMyThrust>> thrusters)
        {
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                    thruster.Enabled = true;
        }

        private void DisableThrusters(List<Orientation> directions, ref IDictionary<Orientation, List<IMyThrust>> thrusters)
        {
            foreach (Orientation direction in directions)
                foreach (IMyThrust thruster in thrusters[direction])
                    thruster.Enabled = false;
        }

        private void DisableAllThrusers(ref IDictionary<Orientation, List<IMyThrust>> thrusters)
        {
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                    thruster.Enabled = false;
        }

        private void OverideThrusters(List<Orientation> directions, ref IDictionary<Orientation, List<IMyThrust>> thrusters, float percent = 0.0f)
        {
            foreach (Orientation direction in directions)
                foreach (IMyThrust thruster in thrusters[direction])
                    thruster.ThrustOverridePercentage = percent;
        }

        private void DisableOverideAllThrusters(ref IDictionary<Orientation, List<IMyThrust>> thrusters)
        {
            foreach (KeyValuePair<Orientation, List<IMyThrust>> thruster_list in thrusters)
                foreach (IMyThrust thruster in thruster_list.Value)
                    thruster.ThrustOverridePercentage = 0.0f;
        }

        /// <summary>
        /// Will apply a force eveanly over all the thrusters in a direction. Thrusers will be set to max output if force is to large.
        /// The method currently assumes all thruseters in one direction can apply the same force (they are the same size).
        /// </summary>
        /// TODO update method to account for diffrent thruster.
        /// <param name="force">Force in newtowns (N) to apply.</param>
        /// <param name="direction">Direction force should be applyed.</param>
        /// <param name="thrusters">Thruster groups</param>
        public void ApplyForce(float force, Orientation direction, ref IDictionary<Orientation, List<IMyThrust>> thrusters) 
        {
            // get the number of throusets
            number_of_thrusters = thrusters.Count();

            // claculate the max force output
            float max_force = 0;
            foreach (IMyThrust thruster in thrusters[direction])
                max_force += thruster.MaxThrust;

            // set the force to max if to large
            force = force <= 0 ? Math.Min(force, max_force) : max_force;

            // split the force over the number of thrusters
            force /= number_of_thrusters;

            // apply the force
            foreach (IMyThrust thruster in thrusters[direction])
                thruster.ThrustOverride(force);
        }

        public void ThrusterMain(Orientation direction, IMyTerminalBlock orientation_block, IMyShipController control_block)
        {
            IDictionary<Orientation, List<IMyThrust>> ordered_thrusters = SetupThruster(orientation_block);

            float velocity = control_block.GetShipVelocities();
            float mass = control_block.CalculateShipMass().TotalMass;

            if (velocity < 10)
                ApplyForce(-1, Orientation.Forward, ref ordered_thrusters);
            else
                DisableOverideAllThrusters(ref ordered_thrusters);
        }

        #endregion

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

                bool bAimed = OrientShip(Orientation.Forward, target, ship_connectors[0]);

                MyShipVelocities velocity = control.GetShipVelocities();

                Echo(velocity.LinearVelocity.ToString());
                Echo(velocity.AngularVelocity.ToString());

                ThrusterMain(Orientation.Forward, control, control);
                break;
            }
        }
    }
}