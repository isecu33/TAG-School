using System.Collections.Generic;
using PieceBook.CitySim.Data;
using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>References to the instantiated grey city, handed back to the loop.</summary>
    public sealed class CityView
    {
        public Transform Root;
        public readonly List<SurfaceMarker> Surfaces = new List<SurfaceMarker>(4);
        public readonly List<ContainerMarker> Containers = new List<ContainerMarker>(4);
    }

    /// <summary>
    /// Instantiates the zone as a pure grey blockout (NO art — cubes only): ground, building
    /// occluders, road strips (so the street graph is legible), paintable surfaces and
    /// container hideouts. All colliders on the Default layer so patrol vision rays are
    /// blocked by buildings/containers but never by the player (Ignore Raycast layer).
    /// </summary>
    public static class CityBuilder
    {
        private const float BuildingHeight = 2.6f;

        public static CityView Build(ZoneDef zone, Transform parent)
        {
            var view = new CityView();
            var root = new GameObject("City").transform;
            root.SetParent(parent, false);
            view.Root = root;

            var groundMat = MaterialFactory.Solid(new Color(0.20f, 0.20f, 0.23f));
            var roadMat = MaterialFactory.Solid(new Color(0.28f, 0.28f, 0.32f));
            var buildingMat = MaterialFactory.Solid(new Color(0.42f, 0.42f, 0.46f));
            var surfaceMat = MaterialFactory.Solid(new Color(0.62f, 0.60f, 0.55f));
            MaterialFactory.SetEmission(surfaceMat, new Color(0.16f, 0.14f, 0.06f)); // reads as a "target"
            var containerMat = MaterialFactory.Solid(new Color(0.30f, 0.45f, 0.35f));

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Road strips along every edge (visualises the graph — grafo legible)
            for (int e = 0; e < zone.streetEdges.Length; e++)
            {
                var ed = zone.streetEdges[e];
                Vector3 a = zone.NodeToWorld(ed.a);
                Vector3 b = zone.NodeToWorld(ed.b);
                Vector3 mid = (a + b) * 0.5f; mid.y = 0.02f;
                Vector3 dir = b - a;
                float len = dir.magnitude;
                var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(road.GetComponent<Collider>());
                road.name = $"Road_{ed.a}_{ed.b}";
                road.transform.SetParent(root, false);
                road.transform.position = mid;
                road.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                road.transform.localScale = new Vector3(1.4f, 0.04f, len);
                road.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
            }

            // Buildings (occluders)
            for (int i = 0; i < zone.buildings.Length; i++)
            {
                var bnds = zone.buildings[i];
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Building_{i}";
                cube.transform.SetParent(root, false);
                cube.transform.position = new Vector3(bnds.center.x, BuildingHeight * 0.5f, bnds.center.y);
                cube.transform.localScale = new Vector3(bnds.width, BuildingHeight, bnds.height);
                cube.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;
            }

            // Paintable surfaces (visual panels on building faces)
            for (int i = 0; i < zone.surfaces.Length; i++)
            {
                var def = zone.surfaces[i];
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(panel.GetComponent<Collider>());
                panel.name = "Surface_" + def.id;
                Vector3 facing = new Vector3(def.facing.x, 0f, def.facing.y).normalized;
                panel.transform.SetParent(root, false);
                panel.transform.position = new Vector3(def.position.x, def.size.y * 0.5f, def.position.y) + facing * 0.06f;
                panel.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
                panel.transform.localScale = new Vector3(def.size.x, def.size.y, 0.08f);
                panel.GetComponent<MeshRenderer>().sharedMaterial = surfaceMat;
                var marker = panel.AddComponent<SurfaceMarker>();
                marker.Configure(def);
                view.Surfaces.Add(marker);
            }

            // Container hideouts
            for (int i = 0; i < zone.hideSpots.Length; i++)
            {
                var hs = zone.hideSpots[i];
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Container_{i}";
                cube.transform.SetParent(root, false);
                cube.transform.position = new Vector3(hs.x, 0.55f, hs.y);
                cube.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);
                cube.GetComponent<MeshRenderer>().sharedMaterial = containerMat;
                view.Containers.Add(cube.AddComponent<ContainerMarker>());
            }

            return view;
        }
    }
}
