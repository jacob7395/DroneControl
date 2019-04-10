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
        public double max_speed = 400;

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
    }
}
