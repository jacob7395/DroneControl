using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using IngameScript.DroneControl.utility.task;
using IngameScript.DroneControl.utility;

namespace IngameScript.DroneControl.Systems
{

    public class ShipSystems
    {
        public Vector3D stopping_distance;
        public Vector3D velocity;
        public Task current_task = null;
        public IMyGridTerminalSystem GridTerminalSystem;
        public IMyShipController controller;
        public double max_speed = 400;

        public IMyTerminalBlock orientation_block;
        public double min_angle = 0.5;

        /// <summary>
        /// Constant the determines the amount of ticks till the ships acceleration will be caped to 0.01%
        /// </summary>
        private const int MIN_FORCE_TICKS = 600;
        /// <summary>
        /// Used by the thrusters to calculate the maximum force that can be applied.
        /// The value is controlled calculated proportionally to gyro_not_aligned_count.
        /// This percent was implemented to prevent a death spin bug where the ship would be unable to
        /// align with a target as it kept accelerating from the target.
        /// </summary>
        public double max_thruster_force_percent
        {
            get
            {
                double calc = (MIN_FORCE_TICKS - gyro_not_aligned_count) / MIN_FORCE_TICKS;
                calc = Math.Max(0.001, calc);
                return calc;
            }
        }
            

        /// <summary>
        /// The amount of ticks the gyros have not been aligned for.
        /// </summary>
        public double gyro_not_aligned_count = 0;

        // collision data used by all camera agents
        public Vector3D DEFAULT_SAFE_POINT
        {
            get
            {
                return Vector3D.PositiveInfinity;
            }
        }

        public MyDetectedEntityInfo collision_object = new MyDetectedEntityInfo();
        public Vector3D safe_point = Vector3D.PositiveInfinity;
        public List<Vector3D> collision_corrners = new List<Vector3D>();


        public ShipSystems(IMyGridTerminalSystem gridTerminalSystem, IMyShipController controller)
        {
            this.GridTerminalSystem = gridTerminalSystem;
            this.controller = controller;
        }

        /// <summary>
        /// Utility function used to get a block orientation.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public Orientation BlockOrentaion(IMyTerminalBlock block)
        {
            Orientation block_orientation = Orientation.None;

            Matrix block_matrix = new Matrix();
            block.Orientation.GetMatrix(out block_matrix);

            // lookup the table to translate from orientation vector to orientation enum
            // declaring this else ware may save process time
            IDictionary<Orientation, Vector3I> orientation_lookup = new Dictionary<Orientation, Vector3I>
            {
                { Orientation.Up, new Vector3I(0, 1, 0) },
                { Orientation.Down, new Vector3I(0, -1, 0) },
                { Orientation.Left, new Vector3I(-1, 0, 0) },
                { Orientation.Right, new Vector3I(1, 0, 0) },
                { Orientation.Forward, new Vector3I(0, 0, -1) },
                { Orientation.Backward, new Vector3I(0, 0, 1) }
            };

            foreach (KeyValuePair<Orientation, Vector3I> lookup in orientation_lookup)
            {
                // if the value matches add to camera dictionary with the lookup key
                if (block_matrix.Forward == lookup.Value)
                {
                    block_orientation = lookup.Key;
                    break;
                }
            }

            return block_orientation;
        }
    }
}
