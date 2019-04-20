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
using IngameScript.Drone;
using IngameScript.Drone.Controller;
using IngameScript.Drone.Coordinator;
using IngameScript.Drone.utility.task;
using IngameScript.Drone.utility;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        DroneBase droneSystem;
        Vector3D target1 = new Vector3D(37.64, 73.47, -186.15);
        Vector3D target2 = new Vector3D(-19.5, -105.5, -59.5);
        Vector3D target3 = new Vector3D(-28.68, -435.98, 829.41);
        Task task = new Task();

        IMyProgrammableBlock pb;

        bool initalized = false;

        public Program()
        {
            // run the program per tick
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            // check what program the ship is
            // counts to indicate the types found
            int droneController = 0;
            int droneCoordinator = 0;
            // list for the blocks
            List<IMyTerminalBlock> pbs = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(pbs);
            foreach (IMyProgrammableBlock pb in pbs)
            {
                if (pb.DisplayNameText == "DroneController")
                    droneController++;
                else if (pb.DisplayNameText == "DroneCoordinator")
                    droneCoordinator++;
            }

            if (droneCoordinator > 1 || droneController > 1 || (droneCoordinator >= 1 && droneController >= 1))
                Echo("Remove extra program blocks");
            else if (droneCoordinator == 0 && droneController == 0)
                Echo("Name program block \"DroneController\" or \"DroneCoordinator\"");
            else if (droneCoordinator == 1)
            {
                Echo("Initializing drone coordinator");
            }
            else if (droneController == 1)
            {
                Echo("Initializing drone controller");
                droneSystem = new DroneControler(GridTerminalSystem) as DroneBase;
                initalized = true;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (initalized == true)
            {
                droneSystem.run();
            }
        }
    }
}