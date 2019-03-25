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
        // read INI defaults from CustomData.  If you don't want to use my INI, remove or modify the following routine
        string sGyroIgnore = "!NAV";

        #region Autogyro 
        // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461 

        // NOTE: uses: shipOrientationBlock from other code as the designated remote or ship controller

        /// <summary>
        /// GYRO:How much power to use 0 to 1.0
        /// </summary>
        double CTRL_COEFF = 0.9;

        /// <summary>
        /// GYRO:how tight to maintain aim. Lower is tighter. Default is 0.01f
        /// </summary>
        float minAngleRad = 0.01f;


        /// <summary>
        /// Enum representing the orientation with respect to the block.
        /// </summary>
        public enum Orientation
        {
            up = 0,
            down = 1,
            backward = 2,
            forward = 3,
            right = 4,
            left = 5
        }

        /// <summary>
        /// Try to align the ship/grid with the given vector. Returns true if the ship is within minAngleRad of being aligned
        /// </summary>
        /// <param name="Orientation">The direction to point. "backward", "up","forward"</param>
        /// <param name="vDirection">the vector for the aim.</param>
        /// <param name="orientation_block">the terminal block to use for orientation</param>
        /// <returns>true if aligned. Meaning the angle of error is less than minAngleRad</returns>
        bool OrientShip(Orientation O, Vector3D target, IMyTerminalBlock orientation_block)
        {

            Vector3D location = orientation_block.GetPosition();

            Vector3D vDirection = BeamRider(location, target, orientation_block);

            bool bAligned = true;

            List<IMyGyro> gyros = gyrosetup();

            Matrix orientation_matrix;
            orientation_block.Orientation.GetMatrix(out orientation_matrix);

            Vector3D down;
            switch (O)
            {
                case Orientation.up:
                    down = orientation_matrix.Up;
                    break;
                case Orientation.down:
                    down = orientation_matrix.Up;
                    break;
                case Orientation.backward:
                    down = orientation_matrix.Backward;
                    break;
                case Orientation.forward:
                    down = orientation_matrix.Forward;
                    break;
                case Orientation.right:
                    down = orientation_matrix.Right;
                    break;
                case Orientation.left:
                    down = orientation_matrix.Left;
                    break;

                default:
                    down = orientation_matrix.Down;
                    break;
            }

            vDirection.Normalize();

            foreach (IMyGyro gyro in gyros)
            {
                gyro.Orientation.GetMatrix(out orientation_matrix);

                var localCurrent = Vector3D.Transform(down, MatrixD.Transpose(orientation_matrix));
                var localTarget = Vector3D.Transform(vDirection, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                var rot = Vector3D.Cross(localCurrent, localTarget);
                double dot2 = Vector3D.Dot(localCurrent, localTarget);
                double ang = rot.Length();

                if (dot2 < 0)
                    ang = Math.PI - ang; // compensate for >+/-90
                else
                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));

                // check if the gycro is pointing at the target
                if (ang < minAngleRad)
                {
                    gyro.GyroOverride = false;
                    continue;
                }

                float yawMax = (float)(2 * Math.PI);

                double ctrl_vel = yawMax * (ang / Math.PI) * CTRL_COEFF;

                ctrl_vel = Math.Min(yawMax, ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel);
                rot.Normalize();
                rot *= ctrl_vel;

                float pitch = -(float)rot.X;
                gyro.Pitch = pitch;

                float yaw = -(float)rot.Y;
                gyro.Yaw = yaw;

                float roll = -(float)rot.Z;
                gyro.Roll = roll;

                //		g.SetValueFloat("Power", 1.0f); 
                gyro.GyroOverride = true;

                bAligned = false;
            }
            return bAligned;
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
        

        Vector3D BeamRider(Vector3D vStart, Vector3D vEnd, IMyTerminalBlock orientation_block)
        {
            // 'BeamRider' routine that takes start,end and tries to stay on that beam.
            Vector3D vBoreEnd = (vEnd - vStart);
            Vector3D vPosition;
            if (orientation_block is IMyShipController)
            {
                vPosition = ((IMyShipController)orientation_block).CenterOfMass;
            }
            else
            {
                vPosition = orientation_block.GetPosition();
            }
            Vector3D vAimEnd = (vEnd - vPosition);
            Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);

            Vector3D vCorrectedAim = (vEnd - vRejectEnd * 2) - vPosition;

            return vCorrectedAim;
        }

        // From Whip. on discord
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

            // TODO refacter code
            Vector3I UP = new Vector3I(0, -1, 0);
            Vector3I DOWN = new Vector3I(0, 1, 0);
            Vector3I LEFT = new Vector3I(1, 0, 0);
            Vector3I RIGHT = new Vector3I(-1, 0, 0);
            Vector3I FOWARD = new Vector3I(0, 0, 1);
            Vector3I BACKWARD = new Vector3I(0, 0, -1);

            foreach (IMyThrust thruster in thrusters)
            {

                //Echo(thruster.CustomName);
                //Echo(thruster.GridThrustDirection.ToString());

                thruster.ThrustOverridePercentage = 0.0f;

                thruster.Orientation.GetMatrix(out thruster_matrix);


                if (thruster.GridThrustDirection == UP)
                {
                    ordered_thrusters[Orientation.up].Add(thruster);
                    thruster.CustomName = "Thruster Up";
                }

                else if (thruster.GridThrustDirection == DOWN)
                {
                    ordered_thrusters[Orientation.down].Add(thruster);
                    thruster.CustomName = "Thruster Down";
                }

                else if (thruster.GridThrustDirection == RIGHT)
                {
                    ordered_thrusters[Orientation.right].Add(thruster);
                    thruster.CustomName = "Thruster Right";
                }

                else if (thruster.GridThrustDirection == LEFT)
                {
                    ordered_thrusters[Orientation.left].Add(thruster);
                    thruster.CustomName = "Thruster Left";
                }

                else if (thruster.GridThrustDirection == FOWARD)
                {
                    ordered_thrusters[Orientation.forward].Add(thruster);
                    thruster.CustomName = "Thruster Forward";
                }

                else if (thruster.GridThrustDirection == BACKWARD)
                {
                    ordered_thrusters[Orientation.backward].Add(thruster);
                    thruster.CustomName = "Thruster Backward";
                }
            }

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

        public void ThrusterMain(Orientation direction, IMyTerminalBlock orientation_block, IMyShipController control_block)
        {
            float ship_mass = control_block.CalculateShipMass().TotalMass;

            IDictionary<Orientation, List<IMyThrust>> ordered_thrusters = SetupThruster(orientation_block);

        }

        public void GetDirectonalVelocity(Orientation direction)
        {

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

                bool bAimed = OrientShip(Orientation.forward, target, ship_connectors[0]);

                MyShipVelocities velocity = control.GetShipVelocities();

                Echo(velocity.LinearVelocity.ToString());
                Echo(velocity.AngularVelocity.ToString());

                ThrusterMain(Orientation.forward, control, control);
                break;
            }

            
        }
    }
}