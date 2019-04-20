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
using IngameScript.Drone.Systems;

namespace IngameScript.Drone.Camera
{
    /// <summary>
    /// Enum used to indicate operational mode
    /// </summary>
    public enum Camera_Mode
    {
        collision_avoidence,
        deactivated
    }

    /// <summary>
    /// Given a mode the camera agent will work further it's goals.
    /// </summary>
    public class CameraAgent
    {
        public Camera_Mode mode;
        public double active_range;
        public bool safe_point_avalible = false;

        private IMyCameraBlock cam;

        private ShipSystems systems;

        public CameraAgent(IMyCameraBlock cam, ShipSystems systems, double active_range = 2000)
        {
            this.cam = cam;
            this.active_range = active_range;
            this.systems = systems;
        }

        public void Run()
        {
            cam.EnableRaycast = true;

            // check if the camera can scan along the velocity vector
            if (cam.CanScan(this.systems.velocity) && this.systems.collision_object.IsEmpty())
            {
                // check if the camera can reach the range
                if (cam.CanScan(this.systems.stopping_distance.Length() * 1.5, this.systems.velocity))
                {
                    this.systems.collision_object = cam.Raycast(active_range);

                    if (!this.systems.collision_object.IsEmpty() && this.systems.collision_object.Velocity == Vector3D.Zero)
                    {
                        Vector3D[] cornners;
                        cornners = this.systems.collision_object.BoundingBox.GetCorners();

                        foreach (Vector3D corner in cornners)
                            this.systems.collision_corrners.Add(corner);
                    }
                }
            }
            // check if collision corners need checking
            else if (this.systems.collision_corrners.Count > 0)
                foreach (Vector3D corner in this.systems.collision_corrners)
                {
                    double distance = Vector3D.Distance(corner, cam.GetPosition());
                    if (cam.CanScan(distance * 1.5, corner))
                    {
                        MyDetectedEntityInfo corner_scan = cam.Raycast(distance * 1.5, corner);

                        if (corner_scan.IsEmpty())
                        {
                            this.systems.safe_point = corner;
                            this.systems.collision_object = new MyDetectedEntityInfo();
                            this.systems.collision_corrners.Clear();
                        }
                        else
                            this.systems.collision_corrners.Remove(corner);
                        break;
                    }
                }
            //else try and check the safe point
            else if (cam.CanScan(this.systems.safe_point))
            {
                this.systems.collision_object = cam.Raycast(this.systems.safe_point);
                // if the safe point is not safe reset the value
                if (!this.systems.collision_object.IsEmpty())
                    this.systems.safe_point = Vector3D.PositiveInfinity;
            }
        }
    }
}
