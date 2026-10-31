using System.Collections.Generic;

namespace BurgerKitchen
{
    /// <summary>Decide si un objeto entregado corresponde a un producto de la carta.</summary>
    public static class ProductMatcher
    {
        public static bool Matches(ProductDefinition product, ItemSnapshot s)
        {
            if (product == null || s.Definition == null || s.IsDirty || s.Definition.isFailedState)
                return false;

            for (int i = 0; i < s.Contents.Count; i++)
            {
                if (s.Contents[i] == null || s.Contents[i].isFailedState)
                    return false;
            }

            switch (product.matchMode)
            {
                case ProductMatchMode.SingleItem:
                    return s.Definition == product.item && (product.allowExtras || s.Contents.Count == 0);

                case ProductMatchMode.Assembly:
                    if (product.item != null && s.Definition != product.item)
                        return false;
                    return MatchContents(product.ingredients, s.Contents, product.orderRule, product.allowExtras);
            }
            return false;
        }

        public static bool MatchContents(IReadOnlyList<ItemDefinition> required, IReadOnlyList<ItemDefinition> actual,
            AssemblyOrderRule rule, bool allowExtras)
        {
            required = required ?? new ItemDefinition[0];
            if (actual.Count < required.Count) return false;
            if (!allowExtras && actual.Count != required.Count) return false;

            switch (rule)
            {
                case AssemblyOrderRule.Exact:
                    return allowExtras ? IsSubsequence(required, actual) : SequenceEqual(required, actual);

                case AssemblyOrderRule.FirstAndLastFixed:
                {
                    int fixedFirst = required.Count >= 1 ? 1 : 0;
                    int fixedLast = required.Count >= 2 ? 1 : 0;
                    if (fixedFirst == 1 && actual[0] != required[0]) return false;
                    if (fixedLast == 1 && actual[actual.Count - 1] != required[required.Count - 1]) return false;
                    var reqMiddle = Slice(required, fixedFirst, required.Count - fixedLast);
                    var actMiddle = Slice(actual, fixedFirst, actual.Count - fixedLast);
                    return MultisetMatch(reqMiddle, actMiddle, allowExtras);
                }

                default:
                    return MultisetMatch(required, actual, allowExtras);
            }
        }

        static List<ItemDefinition> Slice(IReadOnlyList<ItemDefinition> list, int from, int toExclusive)
        {
            var result = new List<ItemDefinition>();
            for (int i = from; i < toExclusive; i++) result.Add(list[i]);
            return result;
        }

        static bool SequenceEqual(IReadOnlyList<ItemDefinition> a, IReadOnlyList<ItemDefinition> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        static bool IsSubsequence(IReadOnlyList<ItemDefinition> sub, IReadOnlyList<ItemDefinition> seq)
        {
            int j = 0;
            for (int i = 0; i < seq.Count && j < sub.Count; i++)
                if (seq[i] == sub[j]) j++;
            return j == sub.Count;
        }

        static bool MultisetMatch(IReadOnlyList<ItemDefinition> required, IReadOnlyList<ItemDefinition> actual, bool allowExtras)
        {
            if (!allowExtras && required.Count != actual.Count) return false;
            var pool = new List<ItemDefinition>(actual);
            for (int i = 0; i < required.Count; i++)
            {
                int idx = pool.IndexOf(required[i]);
                if (idx < 0) return false;
                pool.RemoveAt(idx);
            }
            return allowExtras || pool.Count == 0;
        }
    }
}
