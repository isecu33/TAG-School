using PieceBook.CitySim.AI;
using PieceBook.CitySim.Data;
using PieceBook.CitySim.Events;
using PieceBook.CitySim.Paint;
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
        private const float RunSpeed = 4.4f;
        private const float HideRange = 1.6f;
        private const float CatchRange = 1.2f;

        private EventBus _bus;
        private InputReader _input;
        private PlayerController _player;
        private IsoCameraRig _camera;
        private CitySimHud _hud;
        private ZoneDef _zone;
        private CityView _city;
        private PatrolAgent _patrol;
        private ZoneBlackboard _blackboard;

        private WindowRing _ring;
        private FirstPersonPaint _fp;
        private readonly PaintingWindow _window = new PaintingWindow();
        private SurfaceMarker _windowSurface;
        private float _windowTimer;

        private const float ExploreOrtho = 7.5f;
        private const float PaintOrtho = 4f;

        private LoopPhase _phase = LoopPhase.Explore;
        private SurfaceMarker _nearSurface;

        public LoopPhase Phase => _phase;

        public void Init(EventBus bus, InputReader input, PlayerController player,
                         IsoCameraRig camera, CitySimHud hud, ZoneDef zone, CityView city,
                         PatrolAgent patrol, ZoneBlackboard blackboard)
        {
            _bus = bus;
            _input = input;
            _player = player;
            _camera = camera;
            _hud = hud;
            _zone = zone;
            _city = city;
            _patrol = patrol;
            _blackboard = blackboard;
            _camera.Follow(_player.transform);

            var ringGo = new GameObject("WindowRing");
            ringGo.transform.SetParent(transform, false);
            _ring = ringGo.AddComponent<WindowRing>();

            var fpGo = new GameObject("FirstPersonPaint");
            fpGo.transform.SetParent(transform, false);
            _fp = fpGo.AddComponent<FirstPersonPaint>();
            _fp.Init(_input);
        }

        private void Update()
        {
            if (_input == null) return;
            float dt = Time.deltaTime;
            _input.Tick();

            switch (_phase)
            {
                case LoopPhase.Explore: TickExplore(dt); break;
                case LoopPhase.Painting: TickPainting(dt); break;
                case LoopPhase.Chase: TickChase(dt); break;
                case LoopPhase.Caught:
                case LoopPhase.Escaped: TickOutcome(dt); break;
            }

            _hud.SetPhase(_phase);
            if (_patrol != null)
                _hud.SetAlert(_patrol.State, _patrol.Suspicion, _blackboard != null ? _blackboard.Heat : 0f);
        }

        private void TickExplore(float dt)
        {
            _player.Drive(_input.Move, WalkSpeed);

            _nearSurface = FindNearestSurface(PaintRange);
            UpdateWindow(dt);

            _hud.SetPrompt(_nearSurface != null
                ? $"Ventana ≈ {_window.Remaining:0.0}s — pulsa Espacio para pintar"
                : "Explora El Polígono: acércate a un muro pintable (observa a la patrulla)");

            if (_nearSurface != null && _input.InteractPressed)
                StartPainting(_nearSurface);

            if (_patrol != null && _patrol.IsChasing)
                EnterPhase(LoopPhase.Chase);
        }

        /// <summary>Live window estimate + ring while observing a surface (point 3).</summary>
        private void UpdateWindow(float dt)
        {
            if (_nearSurface == null)
            {
                if (_ring != null) _ring.Hide();
                _windowSurface = null;
                return;
            }
            if (_nearSurface != _windowSurface)
            {
                _windowSurface = _nearSurface;
                _window.Bind(_patrol, _nearSurface.transform.position);
                _windowTimer = 0f;
            }
            _windowTimer -= dt;
            if (_windowTimer <= 0f) { _windowTimer = 0.2f; _window.LiveEstimate(); }

            _ring.Show(_nearSurface.RingAnchor.position + Vector3.up * 1.4f);
            _ring.SetFraction(_window.Fraction, PaintingWindow.Urgency(_window.Fraction));
        }

        private void TickChase(float dt)
        {
            if (_ring != null) _ring.Hide();

            // Hide in a nearby container by holding (GD-02 §3).
            var container = FindNearestContainer(HideRange);
            bool wantHide = _input.HideHeld && container != null;
            _player.SetHidden(wantHide, container != null ? container.transform.position : _player.transform.position);
            if (!wantHide) _player.Drive(_input.Move, RunSpeed);

            _hud.SetPrompt(container != null
                ? "Contenedor cerca — MANTÉN Shift/H para esconderte"
                : "¡Corre! Rompe la línea de visión (esquina/edificio) y busca un contenedor");

            // Caught: the patrol closes to arm's reach while it can still see you.
            float dist = Vector3.Distance(_player.transform.position, _patrol.Position);
            if (dist < CatchRange && !_player.IsHidden)
            {
                Outcome(true);
                return;
            }
            // Escaped: the patrol lost you and dropped out of Persecución.
            if (!_patrol.IsChasing)
                Outcome(false);
        }

        private void Outcome(bool caught)
        {
            _player.ControlEnabled = false;
            EnterPhase(caught ? LoopPhase.Caught : LoopPhase.Escaped);
            if (caught) _hud.ShowOutcome("PILLADO", new Color(1f, 0.4f, 0.4f));
            else _hud.ShowOutcome("ESCAPADO", new Color(0.5f, 1f, 0.6f));
        }

        private void TickOutcome(float dt)
        {
            if (_input.InteractPressed) Restart();
        }

        private void Restart()
        {
            _hud.HideOutcome();
            _player.SetHidden(false, _player.transform.position);
            _player.ControlEnabled = true;
            _player.Teleport(_zone.NodeToWorld(3) + Vector3.up);
            _patrol.ResetToStart();
            _blackboard.Reset();
            _camera.OrthoSize = ExploreOrtho;
            EnterPhase(LoopPhase.Explore);
        }

        private World.ContainerMarker FindNearestContainer(float range)
        {
            World.ContainerMarker best = null;
            float bestSqr = range * range;
            Vector3 p = _player.transform.position;
            for (int i = 0; i < _city.Containers.Count; i++)
            {
                var c = _city.Containers[i];
                Vector3 d = c.transform.position - p; d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSqr) { bestSqr = sq; best = c; }
            }
            return best;
        }

        private void StartPainting(SurfaceMarker surface)
        {
            // Stand at the wall, freeze world control, zoom in and open the FP canvas.
            _player.Teleport(surface.PaintStand + Vector3.up);
            _player.transform.rotation = Quaternion.LookRotation(
                new Vector3(surface.Def.facing.x, 0f, surface.Def.facing.y));
            _player.ControlEnabled = false;
            _window.Bind(_patrol, surface.transform.position);
            _ring.Hide();
            _camera.OrthoSize = PaintOrtho;
            _fp.Enter(_window);
            EnterPhase(LoopPhase.Painting);
        }

        private void TickPainting(float dt)
        {
            _fp.Tick(dt);
            _hud.SetPrompt(_fp.GlanceActive ? "Vistazo a la calle…" : "Pintando — vigila la ventana");

            // Busted mid-piece: the patrol's cone found you at the wall.
            if (_patrol != null && _patrol.IsChasing)
            {
                EndPainting();
                EnterPhase(LoopPhase.Chase);
                return;
            }
            // Piece finished: clean, back to the street.
            if (_fp.Finished)
            {
                EndPainting();
                _hud.SetPrompt("Pieza terminada — clean getaway");
                EnterPhase(LoopPhase.Explore);
            }
        }

        private void EndPainting()
        {
            _fp.Exit();
            _player.ControlEnabled = true;
            _camera.OrthoSize = ExploreOrtho;
        }

        private void EnterPhase(LoopPhase next)
        {
            if (next == _phase) return;
            var prev = _phase;
            _phase = next;
            _bus?.Publish(new LoopPhaseChanged(prev, next));
        }

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
