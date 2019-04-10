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

namespace IngameScript.DroneControl.Systems
{

    public class ShipSystems
    {
        public Vector3D stopping_distance;
        public Vector3D velocity;
        public Task current_task = null;
        public IMyGridTerminalSystem GridTerminalSystem;
        public IMyShipController controller;

        public ShipSystems(IMyGridTerminalSystem gridTerminalSystem, IMyShipController controller)
        {
            GridTerminalSystem = gridTerminalSystem;
            this.controller = controller;
        }
    }
}
