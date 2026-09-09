using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.DrawingEngine.Drips;
using PieceBook.DrawingEngine.Interpolation;
using PieceBook.DrawingEngine.Pooling;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// Pure-logic EditMode tests for the Phase-0 engine. They cover the parts that must be
    /// correct for the spike to be trustworthy: interpolation endpoints, allocation-free
    /// pooling (ENG-03), drip thresholding (ENG-04) and the typed EventBus.
    /// </summary>
    public class DrawingEngineEditTests
    {
        [Test]
        public void CatmullRom_PassesThroughControlPoints()
        {
            var p0 = new Vector2(0f, 0f);
            var p1 = new Vector2(1f, 2f);
            var p2 = new Vector2(3f, 2f);
            var p3 = new Vector2(4f, 0f);

            Assert.That(Vector2.Distance(CatmullRom.Evaluate(p0, p1, p2, p3, 0f), p1), Is.LessThan(1e-4f));
            Assert.That(Vector2.Distance(CatmullRom.Evaluate(p0, p1, p2, p3, 1f), p2), Is.LessThan(1e-4f));
        }

        [Test]
        public void ObjectPool_ReusesReturnedInstances()
        {
            var pool = new ObjectPool<object>(() => new object(), prewarm: 4);
            var a = pool.Get();
            pool.Return(a);
            var b = pool.Get();
            Assert.AreSame(a, b, "Pool must hand back the returned instance, not allocate a new one.");
        }

        [Test]
        public void ObjectPool_PrewarmDoesNotStarve()
        {
            int created = 0;
            var pool = new ObjectPool<object>(() => { created++; return new object(); }, prewarm: 8);
            Assert.AreEqual(8, created, "Prewarm should allocate exactly the requested count up front.");

            var taken = new object[8];
            for (int i = 0; i < 8; i++) taken[i] = pool.Get();
            Assert.AreEqual(8, created, "Taking prewarmed instances must not allocate more.");
        }

        [Test]
        public void PaintAccumulator_SpawnsAndResets()
        {
            var accum = new PaintAccumulator(32);
            var pos = new Vector2(0.5f, 0.5f);

            int idx = accum.Deposit(pos, 0.5f);
            accum.Deposit(pos, 0.5f);
            Assert.That(accum.Get(idx), Is.EqualTo(1.0f).Within(1e-4f));

            var center = accum.CellCenter01(idx);
            Assert.That(center.x, Is.InRange(0f, 1f));
            Assert.That(center.y, Is.InRange(0f, 1f));

            accum.Consume(idx);
            Assert.That(accum.Get(idx), Is.EqualTo(0f));
        }

        private struct Ping : IEvent { public int N; }

        [Test]
        public void EventBus_PublishInvokesSubscriber_ThenStopsAfterUnsubscribe()
        {
            var bus = new EventBus();
            int sum = 0;
            System.Action<Ping> handler = p => sum += p.N;

            bus.Subscribe(handler);
            bus.Publish(new Ping { N = 5 });
            Assert.AreEqual(5, sum);

            bus.Unsubscribe(handler);
            bus.Publish(new Ping { N = 7 });
            Assert.AreEqual(5, sum, "Unsubscribed handler must not fire.");
        }
    }
}
