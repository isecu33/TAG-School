using PieceBook.CitySim.Data;
using PieceBook.CitySim.Loop;
using PieceBook.CitySim.Player;
using PieceBook.CitySim.UI;
using PieceBook.CitySim.World;
using PieceBook.Core.Events;
using UnityEngine;

namespace PieceBook.CitySim.Bootstrap
{
    /// <summary>
    /// SPIKE-B harness. Assembles the whole gray scene in code (iso camera, light, city
    /// blockout, player, HUD, loop) so it runs with zero manual wiring: open SpikeB.unity and
    /// press Play, or drop this component on an empty GameObject. Spike-only; delete when the
    /// vertical slice replaces it. Touches nothing outside CitySim.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpikeBBootstrap : MonoBehaviour
    {
        [Tooltip("Zone data. Left empty, the default 'El Polígono' layout is built at runtime.")]
        public ZoneDef zone;

        private void Awake()
        {
            var bus = EventBus.Default;
            if (zone == null) zone = CityLayout.BuildDefault();

            var input = new InputReader();

            // Iso camera
            var camGo = new GameObject("IsoCamera");
            camGo.transform.SetParent(transform, false);
            camGo.AddComponent<Camera>();
            var rig = camGo.AddComponent<IsoCameraRig>();
            camGo.tag = "MainCamera";

            // Directional light so the lit gray blockout reads
            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // City blockout
            var city = CityBuilder.Build(zone, transform);

            // Player
            var player = CreatePlayer();
            player.Init(camGo.transform);

            // HUD
            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(transform, false);
            var hud = hudGo.AddComponent<CitySimHud>();
            hud.Init();

            // Loop orchestrator
            var loopGo = new GameObject("Loop");
            loopGo.transform.SetParent(transform, false);
            var loop = loopGo.AddComponent<BombingLoopController>();
            loop.Init(bus, input, player, rig, hud, zone, city);
        }

        private PlayerController CreatePlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            var primitiveCollider = go.GetComponent<Collider>();
            if (primitiveCollider != null) DestroyImmediate(primitiveCollider); // CharacterController owns collision

            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 1f, 0f);

            go.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Solid(new Color(0.9f, 0.75f, 0.2f));

            var ctrl = go.AddComponent<PlayerController>();
            go.transform.SetParent(transform, false);
            go.transform.position = zone.NodeToWorld(3) + Vector3.up; // start on a street node
            return ctrl;
        }
    }
}
