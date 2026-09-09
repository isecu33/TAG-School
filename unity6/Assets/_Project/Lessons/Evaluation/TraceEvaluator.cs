using System.Collections.Generic;
using PieceBook.Lessons.Model;
using UnityEngine;

namespace PieceBook.Lessons.Evaluation
{
    /// <summary>Outcome of evaluating one drawn stroke against a set of metrics.</summary>
    public struct EvaluationResult
    {
        public int Crowns;          // 1..3 for a completed attempt, 0 if nothing to score
        public float Overall;       // mean of the evaluated metric scores, 0..1
        public Dictionary<TraceMetric, float> Scores;
    }

    /// <summary>
    /// Turns geometric metrics (<see cref="StrokeMetrics"/>) into a 1-3 crown score
    /// (ARQUITECTURA §5 "Puntuación → 1-3 coronas"). No ML in the MVP; a future on-device
    /// classifier only *adds* style feedback (§5, Fase 4 LES-06), it never replaces this.
    /// </summary>
    public static class TraceEvaluator
    {
        // Overall thresholds → crowns.
        public const float ThreeCrowns = 0.75f;
        public const float TwoCrowns = 0.5f;

        public static EvaluationResult Evaluate(
            Vector2[] positions,
            float[] times,
            float[] pressures,
            TraceMetric[] metrics,
            TraceTemplate template = null,
            float tolerance = 0.1f)
        {
            var scores = new Dictionary<TraceMetric, float>();
            if (metrics != null)
            {
                foreach (var m in metrics)
                {
                    switch (m)
                    {
                        case TraceMetric.Precision:
                            if (template == null) continue; // freeform: no template to score against
                            scores[m] = StrokeMetrics.Precision(positions, template, tolerance);
                            break;
                        case TraceMetric.Smoothness:
                            scores[m] = StrokeMetrics.Smoothness(positions);
                            break;
                        case TraceMetric.SpeedConsistency:
                            scores[m] = StrokeMetrics.SpeedConsistency(positions, times);
                            break;
                        case TraceMetric.LineWeight:
                            scores[m] = StrokeMetrics.LineWeight(pressures);
                            break;
                    }
                }
            }

            float overall = 0f;
            if (scores.Count > 0)
            {
                foreach (var s in scores.Values) overall += s;
                overall /= scores.Count;
            }

            int crowns = 0;
            bool scored = scores.Count > 0 && positions != null && positions.Length > 0;
            if (scored)
                crowns = overall >= ThreeCrowns ? 3 : overall >= TwoCrowns ? 2 : 1;

            return new EvaluationResult { Crowns = crowns, Overall = overall, Scores = scores };
        }
    }
}
