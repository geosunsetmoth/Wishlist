using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Wishlist
{
    public static class Loader
    {
        public static void LoadSpells(string dataPath)
        {
            List<ToolsWrapper> wrappers = new List<ToolsWrapper>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string spellsFile = Path.Combine(dataPath, "spells.json");
            string sublistFile = Path.Combine(dataPath, "spells-sublist-data.json");
            string settingsFile = Path.Combine(dataPath, "settings.json");
            string wishlistFile = Path.Combine(dataPath, "wishlist.json");
            string learnedFile = Path.Combine(dataPath, "learned.json");

            if (File.Exists(sublistFile))
            {
                string json = File.ReadAllText(sublistFile);
                wrappers = JsonSerializer.Deserialize<List<ToolsWrapper>>(json, options) ?? new List<ToolsWrapper>();
            }
            else
            {
                wrappers = new List<ToolsWrapper>();
            }
            foreach (var wrapper in wrappers)
            {
                Spell spell = new Spell
                {
                    name = wrapper.name,
                    level = wrapper.level,
                    damageTypes = new HashSet<int>()
                };
                if (wrapper.damageInflict != null)
                {
                    foreach (var dtName in wrapper.damageInflict)
                    {
                        int index = DamageTypes.GetIndex(dtName);
                        if (index != -1)
                        {
                            spell.damageTypes.Add(index);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Damage type '{dtName}' not recognized for spell '{spell.name}'.");
                        }
                    }
                }
                Program.globalSpellList.Add(spell);
            }

            if (File.Exists(spellsFile))
            {
                string json = File.ReadAllText(spellsFile);
                var loadedSpells = JsonSerializer.Deserialize<List<Spell>>(json, options) ?? new List<Spell>();
                foreach (var spell in loadedSpells)
                {
                    Program.globalSpellList.Add(spell);
                }
            }

            if (File.Exists(wishlistFile))
            {
                string json = File.ReadAllText(wishlistFile);
                var wishlist = JsonSerializer.Deserialize<HashSet<Spell>>(json, options) ?? new HashSet<Spell>();
                foreach (var spell in wishlist)
                {
                    var existingSpell = Program.globalSpellList.FirstOrDefault(s => s.name == spell.name && s.level == spell.level);
                    existingSpell.IsOnWishList = true;
                }

                File.Delete(wishlistFile); 
            }

            if (File.Exists(learnedFile))
            {
                string json = File.ReadAllText(learnedFile);
                var learnedSpells = JsonSerializer.Deserialize<HashSet<Spell>>(json, options) ?? new HashSet<Spell>();
                foreach (var spell in learnedSpells)
                {
                    var existingSpell = Program.globalSpellList.FirstOrDefault(s => s.name == spell.name && s.level == spell.level);
                    if (existingSpell != null)
                    {
                        existingSpell.HasBeenLearned = true;
                    }
                }

                File.Delete(learnedFile); 
            }

            SaveSpells(dataPath, Program.globalSpellList);
            File.Delete(sublistFile); // Clean up the sublist file after loading
        }
        public static void SaveSpells(string dataPath, HashSet<Spell> spellList)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(spellList, options);
            string path = Path.Combine(dataPath, "spells.json");
            File.WriteAllText(path, json);
        }
    }
}
