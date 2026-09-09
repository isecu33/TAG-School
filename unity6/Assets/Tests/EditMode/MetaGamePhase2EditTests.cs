using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.MetaGame.Blackbook;
using PieceBook.MetaGame.Glossary;
using PieceBook.MetaGame.Store;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for MetaGame Fase 2 (META-03 blackbook, META-04 glosario, META-05 tienda).</summary>
    public class MetaGamePhase2EditTests
    {
        // ---- META-04: glossary --------------------------------------------

        private const string GlossaryJson = @"
{
  ""entries"": [
    { ""id"": ""flow"", ""term"": ""Flow"", ""definition"": ""Continuidad del trazo"",
      ""era"": ""80s"", ""relatedTerms"": [""handstyle""] },
    { ""id"": ""handstyle"", ""term"": ""Handstyle"", ""definition"": ""Estilo de firma"",
      ""relatedTerms"": [] }
  ]
}";

        [Test]
        public void Glossary_ParsesAndResolvesRelatedTerms()
        {
            var g = GlossaryJsonParser.Parse(GlossaryJson);
            Assert.AreEqual(2, g.Count);
            Assert.IsTrue(g.TryGet("flow", out var flow));
            Assert.AreEqual("Flow", flow.term);
            CollectionAssert.IsEmpty(g.DanglingRelatedTerms(), "relatedTerms must all resolve.");
        }

        [Test]
        public void Glossary_DetectsDanglingAndMissing()
        {
            var g = GlossaryJsonParser.Parse(
                @"{""entries"":[{""id"":""a"",""term"":""A"",""relatedTerms"":[""ghost""]}]}");
            CollectionAssert.IsNotEmpty(g.DanglingRelatedTerms());

            var missing = g.MissingRefs(new[] { "a", "flow" });
            CollectionAssert.Contains(missing, "flow");
            CollectionAssert.DoesNotContain(missing, "a");
        }

        // ---- META-03: blackbook -------------------------------------------

        [Test]
        public void Blackbook_AddsPage_PersistsAndKeepsThumbnail()
        {
            var repo = new InMemoryProgressRepo();
            var save = new SaveService(repo);
            var store = new InMemoryArtworkStore();
            var book = new BlackbookService(save, store);

            var thumb = new byte[] { 9, 9, 9 };
            book.AddArtwork(ArtworkMeta.New("art_1", "mockup_shutter"), thumb);

            CollectionAssert.Contains(book.Pages, "art_1");
            Assert.AreEqual("mockup_shutter", book.Get("art_1").wallContext);
            CollectionAssert.AreEqual(thumb, book.Thumbnail("art_1"));

            // Persisted: a fresh service over the same repo still lists the page.
            var reopened = new SaveService(repo);
            CollectionAssert.Contains(reopened.Current.BlackbookPages, "art_1");
        }

        // ---- META-05: store -----------------------------------------------

        [Test]
        public void Store_View_ReflectsOwnershipAndBalance()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            save.Current.Coins = 50;
            var store = new StoreService(save, new EventBus(), new[]
            {
                new StoreItem("cap_fat", 30),
                new StoreItem("cap_ny_fat", 120),
            });

            var view = store.View();
            var fat = view.Find(v => v.Id == "cap_fat");
            var ny = view.Find(v => v.Id == "cap_ny_fat");
            Assert.IsTrue(fat.Affordable);
            Assert.IsFalse(ny.Affordable, "Too expensive at 50 coins.");
            Assert.IsFalse(fat.Owned);
        }

        [Test]
        public void Store_Buy_DeductsGrantsAndPublishes()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            save.Current.Coins = 50;
            var bus = new EventBus();
            int unlocks = 0;
            bus.Subscribe<ItemUnlocked>(_ => unlocks++);
            var store = new StoreService(save, bus, new[] { new StoreItem("cap_fat", 30) });

            Assert.IsTrue(store.Buy("cap_fat"));
            Assert.AreEqual(20, save.Current.Coins);
            Assert.IsTrue(save.Current.IsUnlocked("cap_fat"));
            Assert.AreEqual(1, unlocks);

            Assert.IsFalse(store.Buy("cap_fat"), "Already owned.");
            Assert.IsFalse(store.Buy("cap_unknown"), "Not in catalog.");
        }
    }
}
