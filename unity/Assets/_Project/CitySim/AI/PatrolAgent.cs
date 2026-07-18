using System.Collections.Generic;
using PieceBook.CitySim.Data;
using PieceBook.CitySim.Events;
using PieceBook.CitySim.Graph;
using PieceBook.CitySim.Player;
using PieceBook.CitySim.World;
using PieceBook.Core.Events;
using UnityEngine;

namespace PieceBook.CitySim.AI
{
    /// <summary>
    /// One patrol agent (ARQUITECTURA §7 "FSM por agente: Calma→Sospecha→Persecución,
    /// waypoints + cono de visión. Sin navmesh: navegación por grafo"). Calm walks the graph
    /// route; Suspicious investigates the last sighting; Chase pursues. Reads/writes the shared
    /// <see cref="ZoneBlackboard"/> and publishes state changes on the Core EventBus.
    /// Movement uses a CharacterController so buildings block/deflect it — no wall clipping.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PatrolAgent : MonoBehaviour
    {
        private static readonly Color CalmCol = new Color(0.85f, 0.95f, 1f);
        private static readonly Color SuspCol = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ChaseCol = new Color(1f, 0.3f, 0.3f);

        private PatrolDef _def;
        private StreetGraph _graph;
        private ZoneBlackboard _bb;
        private EventBus _bus;
        private Transform _player;
        private PlayerController _playerCtrl;

        private CharacterController _cc;
        private VisionConeRenderer _cone;
        private MeshRenderer _indicator;
        private Material _indicatorMat;

        private List<int> _route;
        private int _routeIdx;
        private float _pauseTimer;

        private PatrolState _state = PatrolState.Calm;
        private float _suspicion;
        private Vector3 _lastKnown;
        private float _searchTimer;

        public PatrolState State => _state;
        public float Suspicion => _suspicion;
        public bool IsChasing => _state == PatrolState.Chase;
        public Vector3 Position => transform.position;

        public void Init(PatrolDef def, StreetGraph graph, ZoneBlackboard bb, EventBus bus, PlayerController player)
        {
            _def = def;
            _graph = graph;
            _bb = bb;
            _bus = bus;
            _player = player.transform;
            _playerCtrl = player;

            gameObject.layer = 2; // Ignore Raycast (vision rays skip agents)
            _cc = GetComponent<CharacterController>();

            _route = graph.BuildLoopRoute(def.waypoints);
            _routeIdx = 0;
            _pauseTimer = def.waypointPause;
            if (_route.Count > 0) transform.position = _graph.Position(_route[0]) + Vector3.up;

            var coneGo = new GameObject("PatrolVisionCone");
            coneGo.transform.SetParent(transform.parent, false);
            _cone = coneGo.AddComponent<VisionConeRenderer>();

            var ind = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ind.GetComponent<Collider>());
            ind.name = "AlertIndicator";
            ind.transform.SetParent(transform, false);
            ind.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            ind.transform.localScale = Vector3.one * 0.5f;
            _indicator = ind.GetComponent<MeshRenderer>();
            _indicatorMat = MaterialFactory.Solid(CalmCol);
            _indicator.sharedMaterial = _indicatorMat;
        }

        /// <summary>Total time for one full Calm loop of the route — used by the window (§ point 3).</summary>
        public float EstimateCycleSeconds()
        {
            if (_route == null || _route.Count < 2) return 0f;
            float len = 0f;
            for (int i = 0; i < _route.Count; i++)
            {
                Vector3 a = _graph.Position(_route[i]);
                Vector3 b = _graph.Position(_route[(i + 1) % _route.Count]);
                len += Vector3.Distance(a, b);
            }
            float travel = len / Mathf.Max(0.1f, _def.speed);
            float pauses = _route.Count * _def.waypointPause;
            return travel + pauses;
        }

        private void Update()
        {
            if (_def == null) return;
            float dt = Time.deltaTime;

            Vector3 eye = transform.position + Vector3.up * 1.2f;
            Vector3 playerEye = _player.position + Vector3.up;
            bool canSee = VisionSensor.CanSee(eye, transform.forward, _def.visionCone,
                                              playerEye, _playerCtrl.IsHidden, out _);
            if (canSee)
            {
                _lastKnown = _player.position;
                _bb.ReportSighting(_player.position);
            }

            var prev = _state;
            UpdateSuspicion(dt, canSee);
            switch (_state)
            {
                case PatrolState.Calm: TickCalm(dt); break;
                case PatrolState.Suspicious: TickSuspicious(dt); break;
                case PatrolState.Chase: TickChase(dt, canSee); break;
            }

            if (_state != prev)
            {
                _bus?.Publish(new PatrolStateChanged(prev, _state));
                if (_state == PatrolState.Chase) _bus?.Publish(new PlayerSpotted(_lastKnown));
            }

            _bb.Decay(_def.alertProfile.suspicionDecayRate * 0.4f, dt);
            _cone.SetColor(StateColor(_state));
            _cone.Redraw(eye, transform.forward, _def.visionCone);
            MaterialFactory.Tint(_indicatorMat, StateColor(_state));
        }

        private void UpdateSuspicion(float dt, bool canSee)
        {
            var ap = _def.alertProfile;
            _suspicion = canSee
                ? Mathf.Min(1f, _suspicion + ap.suspicionBuildRate * dt)
                : Mathf.Max(0f, _suspicion - ap.suspicionDecayRate * dt);

            if (_state == PatrolState.Calm && _suspicion >= ap.suspectThreshold)
                _state = PatrolState.Suspicious;
            if (_suspicion >= ap.chaseThreshold)
            {
                _state = PatrolState.Chase;
                _searchTimer = 0f;
            }
        }

        private void TickCalm(float dt)
        {
            if (_route.Count < 2) return;
            Vector3 target = _graph.Position(_route[_routeIdx]);
            Vector3 to = target - transform.position; to.y = 0f;
            if (to.magnitude < 0.3f)
            {
                _pauseTimer -= dt;
                MoveDir(Vector3.zero, 0f);
                if (_pauseTimer <= 0f)
                {
                    _routeIdx = (_routeIdx + 1) % _route.Count;
                    _pauseTimer = _def.waypointPause;
                }
            }
            else MoveDir(to, _def.speed);
        }

        private void TickSuspicious(float dt)
        {
            Vector3 to = _lastKnown - transform.position; to.y = 0f;
            if (to.magnitude > 0.4f) MoveDir(to, _def.speed * 0.9f);
            else MoveDir(Vector3.zero, 0f); // reached the hunch, look around while suspicion decays

            // Calmed down: give up and rejoin the nearest point of the route.
            if (_suspicion <= 0.03f)
            {
                _state = PatrolState.Calm;
                _routeIdx = NearestRouteIndex(transform.position);
                _pauseTimer = 0f;
            }
        }

        private void TickChase(float dt, bool canSee)
        {
            Vector3 target = canSee ? _player.position : _lastKnown;
            _searchTimer = canSee ? 0f : _searchTimer + dt;

            Vector3 to = target - transform.position; to.y = 0f;
            MoveDir(to, _def.speed * _def.alertProfile.chaseSpeedMultiplier);

            if (_searchTimer > _def.alertProfile.loseSightTime)
            {
                // Lost the trail — drop to a search, then Calm as suspicion bleeds off.
                _state = PatrolState.Suspicious;
                _suspicion = Mathf.Min(_suspicion, _def.alertProfile.suspectThreshold * 0.95f);
            }
        }

        private void MoveDir(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (speed <= 0f || dir.sqrMagnitude < 1e-4f) { _cc.SimpleMove(Vector3.zero); return; }
            dir.Normalize();
            _cc.SimpleMove(dir * speed);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        private int NearestRouteIndex(Vector3 pos)
        {
            int best = 0;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _route.Count; i++)
            {
                float sq = (_graph.Position(_route[i]) - pos).sqrMagnitude;
                if (sq < bestSqr) { bestSqr = sq; best = i; }
            }
            return best;
        }

        private static Color StateColor(PatrolState s) => s switch
        {
            PatrolState.Calm => CalmCol,
            PatrolState.Suspicious => SuspCol,
            _ => ChaseCol
        };
    }
}
