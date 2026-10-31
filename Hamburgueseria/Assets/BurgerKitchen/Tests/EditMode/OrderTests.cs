using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BurgerKitchen.Tests
{
    public class OrderTests
    {
        static ProductDefinition Single(ItemDefinition item, MenuCourse course, int price)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.matchMode = ProductMatchMode.SingleItem;
            p.item = item;
            p.course = course;
            p.price = price;
            return p;
        }

        [Test]
        public void Order_DeliversLinesOneByOne()
        {
            var fries = ScriptableObject.CreateInstance<ItemDefinition>();
            var shake = ScriptableObject.CreateInstance<ItemDefinition>();
            var pFries = Single(fries, MenuCourse.Side, 3);
            var pShake = Single(shake, MenuCourse.Drink, 4);
            var order = new Order(1, new[] { pFries, pShake, pFries });

            Assert.AreEqual(10, order.TotalPrice);
            Assert.IsTrue(order.TryDeliver(new ItemSnapshot(fries), out _));
            Assert.IsTrue(order.TryDeliver(new ItemSnapshot(fries), out _));
            Assert.IsFalse(order.TryDeliver(new ItemSnapshot(fries), out _), "No se pidieron tres patatas");
            Assert.IsFalse(order.IsComplete);
            Assert.IsTrue(order.TryDeliver(new ItemSnapshot(shake), out _));
            Assert.IsTrue(order.IsComplete);
        }

        [Test]
        public void Generator_RespectsUnlockDayAndLimits()
        {
            var menu = ScriptableObject.CreateInstance<MenuDefinition>();
            var early = Single(ScriptableObject.CreateInstance<ItemDefinition>(), MenuCourse.Main, 5);
            var late = Single(ScriptableObject.CreateInstance<ItemDefinition>(), MenuCourse.Main, 9);
            var side = Single(ScriptableObject.CreateInstance<ItemDefinition>(), MenuCourse.Side, 2);
            menu.entries = new List<MenuEntry>
            {
                new MenuEntry { product = early, weight = 1, unlockDay = 1 },
                new MenuEntry { product = late, weight = 1, unlockDay = 3 },
                new MenuEntry { product = side, weight = 1, unlockDay = 1 },
            };
            menu.maxItemsPerOrder = 2;
            menu.extraItemsPerDay = 0f;
            menu.mainChance = 1f; menu.secondMainChance = 1f; menu.sideChance = 1f; menu.drinkChance = 1f; menu.dessertChance = 1f;

            var rng = new System.Random(42);
            for (int i = 0; i < 200; i++)
            {
                var order = OrderGenerator.Generate(menu, 1, rng);
                Assert.That(order.Count, Is.InRange(1, 2));
                CollectionAssert.DoesNotContain(order, late);
            }

            bool sawLate = false;
            for (int i = 0; i < 200; i++) sawLate |= OrderGenerator.Generate(menu, 3, rng).Contains(late);
            Assert.IsTrue(sawLate);
        }

        [Test]
        public void Generator_NeverReturnsEmptyOrder()
        {
            var menu = ScriptableObject.CreateInstance<MenuDefinition>();
            var drink = Single(ScriptableObject.CreateInstance<ItemDefinition>(), MenuCourse.Drink, 2);
            menu.entries = new List<MenuEntry> { new MenuEntry { product = drink, weight = 1, unlockDay = 1 } };
            menu.mainChance = 0f; menu.sideChance = 0f; menu.drinkChance = 0f; menu.dessertChance = 0f;
            var rng = new System.Random(1);
            for (int i = 0; i < 50; i++) Assert.AreEqual(1, OrderGenerator.Generate(menu, 1, rng).Count);
        }
    }
}
