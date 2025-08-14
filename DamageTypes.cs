using System;
using System.Collections.Generic;

namespace Wishlist
{
    public static class DamageTypes
    {
        public static readonly string[] Names =
        {
            "Bludgeoning", // 0
            "Piercing",    // 1
            "Slashing",    // 2
            "Acid",        // 3
            "Cold",        // 4
            "Fire",        // 5
            "Force",       // 6
            "Lightning",   // 7
            "Necrotic",    // 8
            "Poison",      // 9
            "Psychic",     // 10
            "Radiant",     // 11
            "Thunder",     // 12
            "Custard",     // 13
            "Blueberry"    // 14
        };

        // Dictionary for quick reverse lookup (lowercase keys)
        public static readonly Dictionary<string, int> IndexByName;

        static DamageTypes()
        {
            IndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Use StringComparer.OrdinalIgnoreCase to make lookups case-insensitive automatically

            for (int i = 0; i < Names.Length; i++)
            {
                // Add lowercase keys for case-insensitive lookup
                IndexByName[Names[i].ToLower()] = i;
            }
        }

        // Get name from index safely (always capitalized as in Names array)
        public static string GetName(int index)
        {
            if (index >= 0 && index < Names.Length)
                return Names[index];
            return "Unknown";
        }

        // Get index from name safely, case-insensitive
        public static int GetIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;

            // Using case-insensitive dictionary, so just try lookup directly
            if (IndexByName.TryGetValue(name.ToLower(), out int idx))
                return idx;
            return -1;
        }
    }
}
