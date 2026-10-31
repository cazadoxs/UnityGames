using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BurgerKitchen.Tests
{
    public class ProductMatcherTests
    {
        ItemDefinition wrap, bunBottom, bunTop, patty, burnt, cheese, lettuce, fries, cup;

        static ItemDefinition Def(string n, bool failed = false)
        {
            var d = ScriptableObject.CreateInstance<ItemDefinition>();
            d.displayName = n;
            d.isFailedState = failed;
            return d;
        }

        static ProductDefinition Burger(AssemblyOrderRule rule, ItemDefinition baseItem, params ItemDefinition[] layers)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.matchMode = ProductMatchMode.Assembly;
            p.item = baseItem;
            p.orderRule = rule;
            p.ingredients = new List<ItemDefinition>(layers);
            return p;
        }

        [SetUp]
        public void SetUp()
        {
            wrap = Def("wrap"); bunBottom = Def("bunB"); bunTop = Def("bunT"); patty = Def("patty");
            burnt = Def("burnt", true); cheese = Def("cheese"); lettuce = Def("lettuce"); fries = Def("fries"); cup = Def("cup");
        }

        ItemSnapshot Wrap(params ItemDefinition[] contents) => new ItemSnapshot(wrap, contents);

        [Test]
        public void Burger_FirstAndLastFixed_AcceptsMiddleInAnyOrder()
        {
            var p = Burger(AssemblyOrderRule.FirstAndLastFixed, wrap, bunBottom, patty, cheese, lettuce, bunTop);
            Assert.IsTrue(p.Matches(Wrap(bunBottom, lettuce, patty, cheese, bunTop)));
            Assert.IsTrue(p.Matches(Wrap(bunBottom, patty, cheese, lettuce, bunTop)));
        }

        [Test]
        public void Burger_FirstAndLastFixed_RejectsWrongBuns()
        {
            var p = Burger(AssemblyOrderRule.FirstAndLastFixed, wrap, bunBottom, patty, bunTop);
            Assert.IsFalse(p.Matches(Wrap(bunTop, patty, bunBottom)));
            Assert.IsFalse(p.Matches(Wrap(patty, bunBottom, bunTop)));
        }

        [Test]
        public void Burger_RejectsMissingOrExtraIngredients()
        {
            var p = Burger(AssemblyOrderRule.FirstAndLastFixed, wrap, bunBottom, patty, cheese, bunTop);
            Assert.IsFalse(p.Matches(Wrap(bunBottom, patty, bunTop)));
            Assert.IsFalse(p.Matches(Wrap(bunBottom, patty, cheese, cheese, bunTop)));
            p.allowExtras = true;
            Assert.IsTrue(p.Matches(Wrap(bunBottom, patty, cheese, cheese, bunTop)));
        }

        [Test]
        public void Burger_RejectsBurntOrDirty()
        {
            var p = Burger(AssemblyOrderRule.FirstAndLastFixed, wrap, bunBottom, patty, bunTop);
            Assert.IsFalse(p.Matches(Wrap(bunBottom, burnt, bunTop)));
            Assert.IsFalse(p.Matches(new ItemSnapshot(wrap, new[] { bunBottom, patty, bunTop }, isDirty: true)));
        }

        [Test]
        public void Burger_RequiresBaseContainer()
        {
            var p = Burger(AssemblyOrderRule.FirstAndLastFixed, wrap, bunBottom, patty, bunTop);
            Assert.IsFalse(p.Matches(new ItemSnapshot(cup, new[] { bunBottom, patty, bunTop })));
            p.item = null;
            Assert.IsTrue(p.Matches(new ItemSnapshot(cup, new[] { bunBottom, patty, bunTop })));
        }

        [Test]
        public void Exact_RequiresOrder()
        {
            var p = Burger(AssemblyOrderRule.Exact, wrap, bunBottom, patty, cheese, bunTop);
            Assert.IsTrue(p.Matches(Wrap(bunBottom, patty, cheese, bunTop)));
            Assert.IsFalse(p.Matches(Wrap(bunBottom, cheese, patty, bunTop)));
        }

        [Test]
        public void AnyOrder_Toppings()
        {
            var p = Burger(AssemblyOrderRule.AnyOrder, cup, cheese, lettuce);
            Assert.IsTrue(p.Matches(new ItemSnapshot(cup, new[] { lettuce, cheese })));
            Assert.IsFalse(p.Matches(new ItemSnapshot(cup, new[] { lettuce })));
        }

        [Test]
        public void SingleItem_MatchesExactDefinition()
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.matchMode = ProductMatchMode.SingleItem;
            p.item = fries;
            Assert.IsTrue(p.Matches(new ItemSnapshot(fries)));
            Assert.IsFalse(p.Matches(new ItemSnapshot(cup)));
            Assert.IsFalse(p.Matches(new ItemSnapshot(fries, null, true)));
        }
    }
}
