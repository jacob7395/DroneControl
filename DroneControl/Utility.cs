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
using System.Text.RegularExpressions;

/// <summary>
/// Namespace to hold generic operators and enums.
/// </summary>
namespace IngameScript.Drone.utility
{
    /// <summary>
    /// Enum used to represent orientation in six directions
    /// </summary>
    public enum Orientation
    {
        None = 0,
        Up = -1,
        Down = 1,
        Backward = -2,
        Forward = 2,
        Right = 3,
        Left = -3
    }

    /// <summary>
    /// A static class holding operations for enum and generic methods.
    /// </summary>
    static class Utility
    {
        /// <summary>
        /// Returns the inverse direction e.g given forward will return backward.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns>The opposite direction</returns>
        public static Orientation inverse(this Orientation direction)
        {
            return (Orientation)(-(int)direction);
        }
        /// <summary>
        /// Extract the relative grid velocity for a given direction.
        /// Forward = -Z
        /// Backward = Z
        /// Right = X
        /// Left = -X
        /// Up = Y
        /// Down = -Y
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="velocity_vectore"></param>
        /// <returns>The ship velocity in a given direction</returns>
        public static double CalcVelocity(this Orientation direction, Vector3D velocity_vectore)
        {
            double directional_velocity = 0;
            switch (direction)
            {
                case Orientation.Up:
                    directional_velocity = velocity_vectore.Y;
                    break;
                case Orientation.Down:
                    directional_velocity = -velocity_vectore.Y;
                    break;
                case Orientation.Backward:
                    directional_velocity = velocity_vectore.Z;
                    break;
                case Orientation.Forward:
                    directional_velocity = -velocity_vectore.Z;
                    break;
                case Orientation.Right:
                    directional_velocity = velocity_vectore.X;
                    break;
                case Orientation.Left:
                    directional_velocity = -velocity_vectore.X;
                    break;
            }

            return directional_velocity;
        }
    }

    interface IAutoControl
    {
        void DisableAuto();
    }
}
