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
    /// <summary>
    /// A task object hold several actions and provides an interface to access the actions.
    /// 
    /// It also provides a serialization method to allow saving of tasks to strings.
    /// </summary>
    public class Task
    {
        List<DroneAction> actions = new List<DroneAction>();

        /// <summary>
        /// Empty contructor
        /// </summary>
        public Task() {}

        /// <summary>
        /// Get the next action if one is avalible else returns null.
        /// </summary>
        /// <returns>The next action</returns>
        public DroneAction Get_Next_Action()
        {
            DroneAction next_action = null;
            // check if the is an action for this task
            if (this.actions.Count > 0)
                next_action = this.actions[0];

            return next_action;
        }

        /// <summary>
        /// Called when the actions objectives have been met.
        /// </summary>
        /// <returns>True if actions are left</returns>
        public bool Action_Complete()
        {
            if (this.actions.Count > 0)
                this.actions.RemoveAt(0);

            return this.actions.Count > 0;
        }

        /// <summary>
        /// Add an action to the task que
        /// </summary>
        /// <param name="action"></param>
        public void Add_Action(DroneAction action)
        {
            actions.Add(action);
        }

        /// <summary>
        /// Attempts to deserialize an action then add it to the que
        /// </summary>
        /// <param name="action"></param>
        public void Add_Action(String action) {}
    }

    /// <summary>
    /// Type used to indecate what the current action is
    /// </summary>
    public enum action_tpye
    {
        GoTo
    }

    /// <summary>
    /// All action are stored as there abstract class this alows for a generic interface, when action are used they must
    /// be converted to the correct action class.
    /// </summary>
    public abstract class DroneAction
    {
        /// <summary>
        /// Used to determin what action this represents.
        /// </summary>
        /// <returns>An enumeration form action_type</returns>
        abstract public action_tpye get_type();

        /// <summary>
        /// Method to convert the actions into a string
        /// </summary>
        /// <returns></returns>
        abstract public string Serialize();

        /// <summary>
        /// Given a string this method will attemt to fill the objects properts with the data provided.
        /// </summary>
        /// <param name="objective"></param>
        abstract public bool Deserialization(string objective);
    }
    
    /// <summary>
    /// The GoTo action holds the infomation requiered to navigate to a point.
    /// 
    /// String format - "GoTo:x,y,z
    /// </summary>
    public class GoTo : DroneAction
    {
        Vector3D target;

        // this allows the drone to be off from the target by a set amount.
        // this is not currently serealizable
        float tolorance = 5;

        List<Vector3D> route;

        public GoTo(Vector3D target, float tolorance = 5)
        {
            this.tolorance = tolorance;
            this.route = new List<Vector3D>();
            this.route.Add(target);
        }

        public GoTo(string objective)
        {
            this.Deserialization(objective);
            this.route = new List<Vector3D>();
            this.route.Add(target);
        }

        public bool Complete()
        {
            if (this.route.Count > 0)
                this.route.RemoveAt(0);

            return this.route.Count > 0;
        }

        public Vector3D Next_Point()
        {
            return this.route[0];
        }

        public void Add_Point(Vector3D point)
        {
            if (this.route[0] != point)
                route.Insert(0, point);
        }

        public override string Serialize()
        {
            string actiong_name = "GoTo:";

            // add each point to the output, points are added so the last point is first in the string
            string points = "";
            foreach (Vector3D point in this.route)
            {
                // format the string with a devider
                points += String.Format("{0},{1},{2}:", point.X, point.Y, point.Z);
            }
            // remove the extra devided added
            points.Remove(points.Length - 1);

            // return the name and the points
            return actiong_name + points;
        }
        /// <summary>
        /// Given a string this method will fill the class data with infomation provided.
        /// The sting should be fomrated as follows:
        /// "GoTo:x1,y1,z1:x2,y2,z2...xn,yn,zn"
        /// Where the last ponts later in the route come last in the string.
        /// </summary>
        /// <param name="objective"></param>
        /// <returns>Returns true if a route was extracted</returns>
        public override bool Deserialization(string objective)
        {
            // regex to match the action name
            System.Text.RegularExpressions.Regex type_check = new System.Text.RegularExpressions.Regex("^GoTo:");
            System.Text.RegularExpressions.Match correct_type = type_check.Match(objective);
            // flag to indecate if a point was read
            bool success = false;

            if (correct_type.Success)
            {
                // if successfuly matched init a new route
                this.route = new List<Vector3D>();
                // split the string using the seperator character
                string[] split_objective = objective.Split(':');
                // loop from the second elemnt in the split
                // the first element contians only the action name
                for (int i = 1; i < split_objective.Length; i++)
                {
                    // for each elments parse the location data and add to the route
                    float x = float.Parse(split_objective[0]);
                    float y = float.Parse(split_objective[1]);
                    float z = float.Parse(split_objective[2]);

                    this.route.Add(new Vector3D(x, y, z));

                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Return the type for this action.
        /// </summary>
        /// <returns>Will always return action_tpye.GoTo</returns>
        public override action_tpye get_type()
        {
            return action_tpye.GoTo;
        }
    }
}
