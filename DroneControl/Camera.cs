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
using IngameScript.DroneControl.Systems;

namespace IngameScript.DroneControl.Camera
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
        public MyDetectedEntityInfo collision_object;
        public Camera_Mode mode;
        public double active_range;
        public bool safe_point_avalible = false;

        private IMyCameraBlock cam;
        private List<Vector3D> collision_corrners = new List<Vector3D>();
        private Vector3D safe_point = Vector3D.PositiveInfinity;

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

            if (cam.CanScan(active_range) && collision_object.IsEmpty())
            {
                collision_object = cam.Raycast(active_range);

                if (!collision_object.IsEmpty())
                {
                    Vector3D[] cornners;
                    cornners = collision_object.BoundingBox.GetCorners();

                    foreach (Vector3D corner in cornners)
                        collision_corrners.Add(corner);
                }
            }

            if (collision_corrners.Count > 0)
                foreach (Vector3D corner in collision_corrners)
                {
                    double distance = Vector3D.Distance(corner, cam.GetPosition());
                    if (cam.CanScan(distance*1.5, corner))
                    {
                        MyDetectedEntityInfo corner_scan = cam.Raycast(distance * 1.5, corner);

                        if (corner_scan.IsEmpty())
                        {
                            this.safe_point = corner;
                            this.safe_point_avalible = true;
                            collision_object = new MyDetectedEntityInfo();
                            collision_corrners.Clear();
                        }
                        else
                            collision_corrners.Remove(corner);
                        break;
                    }
                }
        }

        public Vector3D GetSafePoint()
        {
            Vector3D temp = safe_point;
            safe_point = Vector3D.PositiveInfinity;
            this.safe_point_avalible = false;
            return temp;
        }
    }
}
