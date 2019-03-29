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

namespace IngameScript.DroneControl.utility
{
    /// <summary>
    /// Enum used to represent orientation in six directions
    /// </summary>
    public enum Orientation
    {
        Up = -1,
        Down = 1,
        Backward = -2,
        Forward = 2,
        Right = 3,
        Left = -3
    }

    static class Utility
    {
        public static Orientation inverse(this Orientation direction)
        {
            return (Orientation)(-(int)direction);
        }

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

    public class Task
    {
        List<Action> actions;

        public Task(String task)
        {
            this.actions = new List<Action>();
        }

        public Action Get_Next_Action()
        {
            return this.actions[0];
        }

        public bool Action_Complete()
        {
            this.actions.RemoveAt(0);

            return this.actions.Count > 0;
        }
    }

    enum ActionType
    {
        MoveTo
    }

    public abstract class Action<T>
    {
        abstract public bool Complete(T check);

        abstract public string Serialize();

        abstract public T Deserialization(string objective);
    }

    public class MoveTo : Action<Vector3D>
    {
        Vector3D target;
        float tolorance;

        List<Vector3D> route;


        public MoveTo(Vector3D target, float tolorance = 5)
        {
            this.target = target;
            this.tolorance = tolorance;
            this.route = new List<Vector3D>();
            this.route.Add(target);
        }

        public override bool Complete(Vector3D current_postion)
        {
            if (Vector3D.Distance(this.route[0], current_postion) <= tolorance)
                this.route.RemoveAt(0);

            return this.route.Count > 0;
        }

        public Vector3D Next_Point()
        {
            return this.route[0];
        }

        public void Add_Point(Vector3D point)
        {
            route.Add(point);
        }

        public override string Serialize()
        {
            return String.Format("GoTo:{0},{1},{2}", this.target.X, this.target.Y, this.target.Z);
        }

        public override Vector3D Deserialization(string objective)
        {
            Regex type_check = new Regex("^MoveTo:([^,]+),([^,]+),([^,]+)");
            Match correct_type = type_check.Match(objective);

            Vector3D out_val = Vector3D.NegativeInfinity;

            if (correct_type.Success)
            {
                float x = float.Parse(correct_type.Groups[0].Value);
                float y = float.Parse(correct_type.Groups[0].Value);
                float z = float.Parse(correct_type.Groups[0].Value);

                out_val = new Vector3D(x, y, z);
            }

            return out_val;
        }
    }
}
