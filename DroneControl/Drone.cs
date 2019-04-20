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
using IngameScript.Drone.Systems;
using IngameScript.Drone.comms;

namespace IngameScript.Drone
{
    public struct DroneData
    {
        public string type;
        public string ID = 0;
    }

    public abstract class DroneBase
    {
        public DroneData data;
        public ShipSystems systems;

        private Comms coms_system;

        protected void defaultInit(IMyGridTerminalSystem GridTerminalSystem)
        {
            // get the main remote controller being used
            IMyRemoteControl controller = GridTerminalSystem.GetBlockWithName("Controler") as IMyRemoteControl;
            List<IMyRemoteControl> remote_controlers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remote_controlers);
            foreach (IMyRemoteControl control in remote_controlers)
            {
                controller = control;
                break;
            }

            // setup the ship systems
            this.systems = new ShipSystems(GridTerminalSystem, controller);

            // setup the orientation block to a ship connector or a controller if one was not found
            List<IMyShipConnector> ship_connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ship_connectors);
            if (ship_connectors.Count > 0)
                this.systems.orientation_block = ship_connectors[0];
            else
                this.systems.orientation_block = systems.controller;

            // add the comms system
            this.coms_system = new Comms(this.systems.GridTerminalSystem);
        }

        abstract public void run();

        protected virtual void GenerateUID()
        {
           
        }
    }
}
