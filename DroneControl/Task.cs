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

namespace IngameScript.DroneControl.utility.task
{
    public class Task
    {
        List<DroneAction> actions = new List<DroneAction>();

        public Task() {}

        public DroneAction Get_Next_Action()
        {
            DroneAction next_action = null;
            if (this.actions.Count > 0)
                next_action = this.actions[0];

            return next_action;
        }

        public bool Action_Complete()
        {
            this.actions.RemoveAt(0);

            return this.actions.Count > 0;
        }

        public void Add_Action(DroneAction action)
        {
            actions.Add(action);
        }

        public void Add_Action(String action)
        {
            
        }
    }

    public enum action_tpye
    {
        GoTo
    }

    public abstract class DroneAction
    {
        abstract public action_tpye get_type();

        abstract public string Serialize();
    }

    public class GoTo : DroneAction
    {
        Vector3D target;
        float tolorance = 5;

        List<Vector3D> route;

        public const action_tpye type = action_tpye.GoTo;


        public GoTo(Vector3D target, float tolorance = 5)
        {
            this.target = target;
            this.tolorance = tolorance;
            this.route = new List<Vector3D>();
            this.route.Add(target);
        }

        public GoTo(string objective)
        {
            Vector3D target = this.Deserialization(objective);
            this.target = target;
            this.route = new List<Vector3D>();
            this.route.Add(target);
        }

        public bool Complete(Vector3D current_postion)
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

        public Vector3D Deserialization(string objective)
        {
            System.Text.RegularExpressions.Regex type_check = new System.Text.RegularExpressions.Regex("^GoTo:([^,]+),([^,]+),([^,]+)");
            System.Text.RegularExpressions.Match correct_type = type_check.Match(objective);

            Vector3D out_val = Vector3D.NegativeInfinity;

            if (correct_type.Success)
            {
                float x = float.Parse(correct_type.Groups[1].Value);
                float y = float.Parse(correct_type.Groups[2].Value);
                float z = float.Parse(correct_type.Groups[3].Value);

                out_val = new Vector3D(x, y, z);
            }

            return out_val;
        }

        public override action_tpye get_type()
        {
            return action_tpye.GoTo;
        }
    }
}
