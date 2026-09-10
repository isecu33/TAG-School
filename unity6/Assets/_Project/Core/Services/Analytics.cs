using System;

namespace PieceBook.Core.Services
{
    /// <summary>How sensitive an analytics event is, for age-gating (§10).</summary>
    public enum AnalyticsCategory
    {
        /// <summary>Essential/operational (crashes, app open). Always allowed.</summary>
        Essential,
        /// <summary>Behavioural/learning-funnel. Dropped for under-16 users (§10 "analítica reducida bajo 16").</summary>
        Behavioral,
    }

    /// <summary>
    /// Age gate (ARQUITECTURA §10 "age gate + analytics reducida bajo 16"). Pure classification; the
    /// birth year / age is collected in onboarding (device UI) and set here.
    /// </summary>
    public sealed class AgeGate
    {
        public bool IsUnder16 { get; private set; }

        public void SetAge(int age) => IsUnder16 = age >= 0 && age < 16;
        public void SetUnder16(bool value) => IsUnder16 = value;
    }

    /// <summary>Analytics sink boundary (Firebase Analytics impl on device, §2). A fake serves tests.</summary>
    public interface IAnalytics
    {
        void LogEvent(string name);
    }

    /// <summary>
    /// Age-gated analytics facade (Fase 3 CI-02). Forwards events to the sink, but drops
    /// <see cref="AnalyticsCategory.Behavioral"/> events for under-16 users (§10). Essential events
    /// always pass. Pure over <see cref="IAnalytics"/> + <see cref="AgeGate"/>, so it is testable.
    /// </summary>
    public sealed class AnalyticsService
    {
        private readonly IAnalytics _sink;
        private readonly AgeGate _ageGate;

        public AnalyticsService(IAnalytics sink, AgeGate ageGate)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _ageGate = ageGate ?? throw new ArgumentNullException(nameof(ageGate));
        }

        public void LogEvent(string name, AnalyticsCategory category = AnalyticsCategory.Behavioral)
        {
            if (category == AnalyticsCategory.Behavioral && _ageGate.IsUnder16) return; // §10
            _sink.LogEvent(name);
        }
    }
}
