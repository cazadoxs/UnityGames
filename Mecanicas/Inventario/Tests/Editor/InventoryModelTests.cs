using NUnit.Framework;
using UnityEngine;

namespace Mecanicas.Inventario.Tests
{
    public class InventoryModelTests
    {
        private static ItemData Make(string id, int maxStack, float weight = 0f, string category = "General")
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = id;
#if UNITY_EDITOR
            item.EditorSetup(id, id, "", category, Color.white, maxStack, weight, false, false);
#endif
            return item;
        }

        [Test]
        public void Add_FillsExistingStacksThenEmptySlots()
        {
            var apple = Make("apple", 10);
            var inv = new InventoryModel(3);
            Assert.AreEqual(0, inv.Add(apple, 7));
            Assert.AreEqual(0, inv.Add(apple, 5));
            Assert.AreEqual(10, inv.GetSlot(0).amount);
            Assert.AreEqual(2, inv.GetSlot(1).amount);
            Assert.AreEqual(12, inv.Count(apple));
        }

        [Test]
        public void Add_ReturnsLeftoverWhenFull()
        {
            var key = Make("key", 1);
            var inv = new InventoryModel(2);
            Assert.AreEqual(1, inv.Add(key, 3));
            Assert.IsTrue(inv.IsFull);
        }

        [Test]
        public void Add_RespectsWeightLimit()
        {
            var stone = Make("stone", 20, 1f);
            var inv = new InventoryModel(5, 3.5f);
            Assert.AreEqual(7, inv.Add(stone, 10));
            Assert.AreEqual(3, inv.Count(stone));
            Assert.AreEqual(0, inv.SpaceFor(stone));
        }

        [Test]
        public void Remove_TakesFromLastSlotsAndClears()
        {
            var apple = Make("apple", 5);
            var inv = new InventoryModel(3);
            inv.Add(apple, 8);
            Assert.AreEqual(4, inv.Remove(apple, 4));
            Assert.AreEqual(4, inv.Count(apple));
            Assert.AreEqual(4, inv.GetSlot(0).amount);
            Assert.IsTrue(inv.GetSlot(1).IsEmpty);
            Assert.AreEqual(4, inv.Remove(apple, 10));
        }

        [Test]
        public void MoveOrSwap_MergesSameItemAndSwapsDifferent()
        {
            var apple = Make("apple", 10);
            var key = Make("key", 1);
            var inv = new InventoryModel(4);
            inv.SetSlot(0, new ItemStack(apple, 6));
            inv.SetSlot(1, new ItemStack(apple, 7));
            inv.SetSlot(2, new ItemStack(key, 1));

            inv.MoveOrSwap(0, 1);
            Assert.AreEqual(10, inv.GetSlot(1).amount);
            Assert.AreEqual(3, inv.GetSlot(0).amount);

            inv.MoveOrSwap(2, 0);
            Assert.AreEqual(key, inv.GetSlot(0).item);
            Assert.AreEqual(apple, inv.GetSlot(2).item);
        }

        [Test]
        public void SplitAndMoveAmount()
        {
            var apple = Make("apple", 10);
            var inv = new InventoryModel(3);
            inv.Add(apple, 9);
            int to = inv.Split(0);
            Assert.AreEqual(1, to);
            Assert.AreEqual(5, inv.GetSlot(0).amount);
            Assert.AreEqual(4, inv.GetSlot(1).amount);
            Assert.AreEqual(1, inv.MoveAmount(0, 2, 1));
            Assert.AreEqual(1, inv.GetSlot(2).amount);
        }

        [Test]
        public void Sort_MergesAndOrdersByCategoryThenName()
        {
            var b = Make("b", 10, 0f, "B");
            var a = Make("a", 10, 0f, "A");
            var inv = new InventoryModel(5);
            inv.SetSlot(4, new ItemStack(b, 3));
            inv.SetSlot(1, new ItemStack(a, 2));
            inv.SetSlot(3, new ItemStack(b, 4));
            inv.Sort();
            Assert.AreEqual(a, inv.GetSlot(0).item);
            Assert.AreEqual(b, inv.GetSlot(1).item);
            Assert.AreEqual(7, inv.GetSlot(1).amount);
            Assert.IsTrue(inv.GetSlot(2).IsEmpty);
        }

        [Test]
        public void Resize_ReturnsWhatDoesNotFit()
        {
            var key = Make("key", 1);
            var inv = new InventoryModel(4);
            inv.Add(key, 4);
            var overflow = inv.Resize(2);
            Assert.AreEqual(2, inv.Count(key));
            Assert.AreEqual(2, overflow.Count);
        }
    }
}
