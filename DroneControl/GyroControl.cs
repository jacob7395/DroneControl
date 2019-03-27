using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
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
using IngameScript.utility;

namespace IngameScript.gyro
{
    public class GyroControl
    {

        private List<IMyGyro> gyros = new List<IMyGyro>();
        private IMyGridTerminalSystem GridTerminalSystem;

        public GyroControl(IMyGridTerminalSystem GridTerminalSystem)
        {
            this.GridTerminalSystem = GridTerminalSystem;
            this.gyros = gyrosetup();   
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
        public bool OrientShip(Orientation direction,
                        Vector3D target,
                        IMyTerminalBlock orientation_block,
                        double gyro_power = 0.9,
                        float min_angle = 5.0f)
        {
            // get the position of the orientaion block
            Vector3D location = orientation_block.GetPosition();
            // return argument used to indicate the min_angle_rad has been met
            bool aligned = true;
            // convert form degress to rads
            double min_angle_rad = (Math.PI / 180) * min_angle;

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

            // applys maths that I dont quite understand but it works
            // creadit goes to link given above
            foreach (IMyGyro gyro in this.gyros)
            {
                gyro.Orientation.GetMatrix(out orientation_matrix);
                var localCurrent = Vector3D.Transform(down, MatrixD.Transpose(orientation_matrix));
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
        private List<IMyGyro> gyrosetup()
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
        public void gyrosOff(List<IMyGyro> gyros)
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
        private Vector3D BeamRider(Vector3D start, Vector3D end, IMyTerminalBlock orientation_block)
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
        private Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
        {
            if (Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }
    }
}
