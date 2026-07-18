using PieceBook.CitySim.AI;
using PieceBook.CitySim.Data;
using PieceBook.CitySim.Player;
using PieceBook.CitySim.UI;
using PieceBook.CitySim.World;
using PieceBook.Core.Events;
using UnityEngine;

namespace PieceBook.CitySim.Loop
{
    /// <summary>
    /// Session orchestrator for the bombing loop (GD-02 §2): Explore → Painting → Chase →
    /// Outcome. A single top-level state machine that ticks input, drives the player, and
    /// hands control to the painting/chase subsystems. Grown point by point through the spike.
    /// </summary>
    public sealed class BombingLoopController : MonoBehaviour
    {
        private const float PaintRange = 1.7f;
        private const float WalkSpeed = 3.2f;

        private EventBus _bus;
        private InputReader _input;
        private PlayerController _player;
        private IsoCameraRig _camera;
        private CitySimHud _hud;
        private ZoneDef _zone;
        private CityView _city;

        private LoopPhase _phase = LoopPhase.Explore;
        private SurfaceMarker _nearSurface;

        public LoopPhase Phase => _phase;

        public void Init(EventBus bus, InputReader input, PlayerController player,
                         IsoCameraRig camera, CitySimHud hud, ZoneDef zone, CityView city)
        {
            _bus = bus;
            _input = input;
            _player = player;
            _camera = camera;
            _hud = hud;
            _zone = zone;
            _city = city;
            _camera.Follow(_player.transform);
        }

        private void Update()
        {
            if (_input == null) return;
            float dt = Time.deltaTime;
            _input.Tick();

            switch (_phase)
            {
                case LoopPhase.Explore: TickExplore(dt); break;
            }

            _hud.SetPhase(_phase);
        }

        private void TickExplore(float dt)
        {
            _player.Drive(_input.Move, WalkSpeed);

            _nearSurface = FindNearestSurface(PaintRange);
            _hud.SetPrompt(_nearSurface != null
                ? "Muro a tiro — pulsa Espacio para pintar"
                : "Explora El Polígono: acércate a un muro pintable");

            if (_nearSurface != null && _input.InteractPressed)
                StartPainting(_nearSurface);
        }

        // Filled in point 4 (first-person painting). Stubbed so Explore is runnable now.
        private void StartPainting(SurfaceMarker surface) { }

        private SurfaceMarker FindNearestSurface(float range)
        {
            SurfaceMarker best = null;
            float bestSqr = range * range;
            Vector3 p = _player.transform.position;
            for (int i = 0; i < _city.Surfaces.Count; i++)
            {
                var s = _city.Surfaces[i];
                Vector3 d = s.PaintStand - p; d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSqr) { bestSqr = sq; best = s; }
            }
            return best;
        }
    }
}
