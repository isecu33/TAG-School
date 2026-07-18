using PieceBook.CitySim.Events;
using PieceBook.Core.Events;
using UnityEngine;

namespace PieceBook.CitySim.AI
{
    /// <summary>
    /// Lightweight shared blackboard for a zone (ARQUITECTURA §7 "Blackboard ligero: estado
    /// compartido de alerta por zona (nivel de calor) que leen todas las patrullas de esa
    /// zona"). One patrol in the spike, but the interface is the real one: any agent in the
    /// zone reads the heat and the last known player position, so future patrols coordinate.
    /// </summary>
    public sealed class ZoneBlackboard
    {
        private readonly EventBus _bus;
        private float _heat;              // 0 = calm zone, 1 = fully alerted
        private float _lastPublished = -1f;

        public ZoneBlackboard(EventBus bus) { _bus = bus; }

        /// <summary>Zone heat 0..1. Rises when the player is seen, decays over time.</summary>
        public float Heat => _heat;

        public bool HasLastKnownPlayerPos { get; private set; }
        public Vector3 LastKnownPlayerPos { get; private set; }

        public void ReportSighting(Vector3 playerPos)
        {
            LastKnownPlayerPos = playerPos;
            HasLastKnownPlayerPos = true;
            RaiseHeat(1f);
        }

        public void ClearLastKnown() => HasLastKnownPlayerPos = false;

        public void RaiseHeat(float amount)
        {
            SetHeat(Mathf.Clamp01(_heat + amount));
        }

        public void Decay(float perSecond, float dt)
        {
            if (_heat <= 0f) return;
            SetHeat(Mathf.Clamp01(_heat - perSecond * dt));
        }

        private void SetHeat(float value)
        {
            _heat = value;
            // Publish only on a meaningful change to avoid event spam (§7: no Update polling).
            if (_bus != null && Mathf.Abs(_heat - _lastPublished) >= 0.05f)
            {
                _lastPublished = _heat;
                _bus.Publish(new ZoneHeatChanged(_heat));
            }
        }
    }
}
