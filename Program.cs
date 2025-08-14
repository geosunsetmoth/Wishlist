using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wishlist
{
    public class Program
    {
        public static HashSet<Spell> globalSpellList = new HashSet<Spell>();
        public static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Wishlist"
        );

        public static HashSet<HashSet<Spell>> triedBranches = new HashSet<HashSet<Spell>>();
        public static HashSet<HashSet<Spell>> bestBranches = new HashSet<HashSet<Spell>>();
        public static HashSet<Spell> currentBranch = new HashSet<Spell>();
        public static int bestBranchLength = int.MaxValue;

        public static void Main()
        {
            Directory.CreateDirectory(AppDataPath); 
            Loader.LoadSpells(AppDataPath);
            while (true)
            {
                MainMenu();
            }
        }

        public static void MainMenu()
        {
            Console.WriteLine ("\n-------------------------------------------------------------" +
                "\nWelcome to the Wishlist App! I will help you find the right spells for your needs." +
                "\nYour database currently has " + globalSpellList.Count + " spells loaded." +
                "\nWhat do you want to do?" +
                "\n" +
                "\n(1) Run Simulation for a specific level" +
                "\n(2) Run Simulation for all levels" +
                "\n");

            string? choice = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(choice))
            {
                Console.WriteLine("Invalid input. Please try again.");
                return;
            }

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\nEnter the level you want to simulate (1-9):\n");
                    string? levelInput = Console.ReadLine();
                    if (int.TryParse(levelInput, out int level) && level >= 1 && level <= 9)
                    {
                        RunSimulationForLevel(level);
                    }
                    else
                    {
                        Console.WriteLine("Invalid level. Please enter a number between 1 and 9.");
                    }
                    break;
                case "2":
                    SimulateAllLevels();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break; 
            }

        }

        public static void SimulateAllLevels()
        {
            Console.WriteLine("Running simulation...");
            
            for (int i = 1; i <= 9; i++)
            {
                RunSimulationForLevel(i);
            }
        }
        public static void RunSimulationForLevel(int level)
        {
            List<Spell> spellsAtLevel = globalSpellList.Where(s => s.level == level && s.damageTypes.Count > 0).ToList();
            List<Spell> takenSpells = spellsAtLevel.Where(s => s.HasBeenLearned || s.IsOnWishList).ToList();
            List<Spell> availableSpells = spellsAtLevel.Where(s => !s.HasBeenLearned && !s.IsOnWishList).ToList();
            availableSpells = availableSpells.OrderByDescending(s => s.damageTypes.Count).ToList();

            HashSet<int> allTypes = new HashSet<int>(Enumerable.Range(0, DamageTypes.Names.Length));
            HashSet<int> targetDamageTypes = new HashSet<int>();
            HashSet<int> takenDamageTypes = new HashSet<int>();
            HashSet<int> availableDamageTypes = new HashSet<int>();
            HashSet<int> impossibleDamageTypes = new HashSet<int>();

            List<List<Spell>> output = new List<List<Spell>>();

            triedBranches.Clear();
            bestBranches.Clear();
            bestBranchLength = int.MaxValue;
            currentBranch.Clear();

            FilterDamageTypesInLevel(takenSpells, availableSpells, allTypes, out targetDamageTypes, takenDamageTypes, availableDamageTypes, out impossibleDamageTypes);

            RecursionLogic(availableSpells, targetDamageTypes); // Simulation is run here

            PrintOutput(level, takenSpells, targetDamageTypes, availableDamageTypes, impossibleDamageTypes);
        }

        private static void PrintOutput(int level, List<Spell> takenSpells, HashSet<int> targetDamageTypes, HashSet<int> availableDamageTypes, HashSet<int> impossibleDamageTypes)
        {
            List<string> namesOfAvailableDamageTypes = new List<string>();
            foreach (var dt in availableDamageTypes)
            {
                namesOfAvailableDamageTypes.Add(DamageTypes.GetName(dt));
            }

            HashSet<string> hashNamesOfTakenDamageTypes = new HashSet<string>();
            foreach (var spell in takenSpells)
            {
                foreach (var dt in spell.damageTypes)
                {
                    hashNamesOfTakenDamageTypes.Add(DamageTypes.GetName(dt));
                }
            }
            List<string> namesOfTakenDamageTypes = new List<string>(hashNamesOfTakenDamageTypes);

            List<string> namesOfImpossibleDamageTypes = new List<string>();
            foreach (var dt in impossibleDamageTypes)
            {
                namesOfImpossibleDamageTypes.Add(DamageTypes.GetName(dt));
            }

            Console.WriteLine($"\n------------------------------------------------" +
                $"\nLevel {level}:" +
                $"\nThe damage types available at this level {FormatListWithVerb(namesOfAvailableDamageTypes)}." +
                $"\nThe damage types already covered by your selections {FormatListWithVerb(namesOfTakenDamageTypes)}.");

            if (targetDamageTypes.Count == 0)
            {
                Console.WriteLine("You have all damage types covered at this level!");
            }
            else
            {
                List<string> targetTypes = new List<string>();
                foreach (var dt in targetDamageTypes)
                {
                    targetTypes.Add(DamageTypes.GetName(dt));
                }
                Console.WriteLine("That means the damage types you still need to cover at this level " + FormatListWithVerb(targetTypes) + ".");
            }

            Console.WriteLine($"The minimum amount of spells you need to cover all damage types at this level is {bestBranchLength}.");
            Console.WriteLine("The following lists of spells are the best candidates to cover the remaining damage types:");

            int branchCount = 1;
            foreach (var branch in bestBranches)
            {
                Console.WriteLine($"\n----- Option {branchCount} ----");
                foreach (var spell in branch)
                {
                    List<string> damageTypes = spell.damageTypes
                        .Select(dt => DamageTypes.GetName(dt))
                        .ToList();
                    string damageTypesFormatted = string.Join(", ", damageTypes);
                    Console.WriteLine($"{spell.name} ({damageTypesFormatted})");
                }
                branchCount++;
            }

            Console.WriteLine($"\nThe damage types that are impossible to get at this level {FormatListWithVerb(namesOfImpossibleDamageTypes)}.");
        }

        private static void FilterDamageTypesInLevel(List<Spell> takenSpells, List<Spell> availableSpells, HashSet<int> allTypes, out HashSet<int> targetDamageTypes, HashSet<int> takenDamageTypes, HashSet<int> availableDamageTypes, out HashSet<int> impossibleDamageTypes)
        {
            // Figure out which damage types are available at this level
            foreach (var spell in availableSpells)
            {
                foreach (var dt in spell.damageTypes)
                {
                    availableDamageTypes.Add(dt);
                }
            }
            impossibleDamageTypes = allTypes.Except(availableDamageTypes).ToHashSet();

            // Figure out which damage types have been taken or wishlisted yet
            foreach (var spell in takenSpells)
            {
                foreach (var dt in spell.damageTypes)
                {
                    takenDamageTypes.Add(dt);
                }
            }

            // Figure out, then, which damage types are still needed
            targetDamageTypes = availableDamageTypes.Except(takenDamageTypes).ToHashSet();
        }
        public static void RecursionLogic(List<Spell> spells, HashSet<int> remainingTypes)
        {
            bool flowControl = PruneBranches(spells, remainingTypes); // Checks if this current branch can be pruned yet, or if it's the best branch so far
            if (!flowControl)
            {
                return;
            }

            spells = FilterSpells(spells, remainingTypes); // Also checks which remaining types need to be covered
            AddSpellToBranch(spells, remainingTypes); // Adds an elligible spell to the current branch and recurses further
        }

        private static void AddSpellToBranch(List<Spell> spells, HashSet<int> remainingTypes)
        {
            for (int i = 0; i < spells.Count; i++)
            {
                Spell chosen = spells[i];
                currentBranch.Add(chosen);
                triedBranches.Add(new HashSet<Spell>(currentBranch));

                var newRemaining = new HashSet<int>(remainingTypes);
                newRemaining.ExceptWith(chosen.damageTypes);

                var nextSpells = spells.Skip(i + 1).ToList();

                RecursionLogic(nextSpells, newRemaining);
            }
        }

        private static bool PruneBranches(List<Spell> spells, HashSet<int> remainingTypes)
        {
            if (currentBranch.Count >= bestBranchLength)
            {
                // If the current branch is already longer than the best found, stop recursion
                return false;
            }

            if (remainingTypes.Count == 0 || spells.Count == 0)
            {
                // Base case: no more types needed or no spells left
                if (currentBranch.Count < bestBranchLength) // Found a new best branch
                {
                    bestBranches.Clear();
                    bestBranches.Add(new HashSet<Spell>(currentBranch));
                    bestBranchLength = currentBranch.Count;
                }
                else if (currentBranch.Count == bestBranchLength) // Found another solved branch that is as good as the current best
                {
                    bestBranches.Add(new HashSet<Spell>(currentBranch)); 
                }
                spells.Clear();
                return false;
            }

            return true;
        }

        private static List<Spell> FilterSpells(List<Spell> spells, HashSet<int> remainingTypes)
        {
            // Filter spells that cover at least one of the remaining types
            spells = spells.Where(s => s.damageTypes.Overlaps(remainingTypes)).ToList();

            // Filter spells already tried for the current branch
            HashSet<HashSet<Spell>> branchesAhead = new HashSet<HashSet<Spell>>();
            HashSet<Spell> spellsInBranchesAhead = new HashSet<Spell>();
            foreach (HashSet<Spell> branch in triedBranches)
            {
                if (branch.Count == currentBranch.Count + 1 && currentBranch.IsSubsetOf(branch))
                {
                    var difference = branch.Except(currentBranch);
                    spellsInBranchesAhead.UnionWith(difference);
                }
            }
            spells = spells.Where(s => !spellsInBranchesAhead.Contains(s)).ToList();
            return spells;
        }

        public static string FormatListWithVerb(List<string> items)
        {
            if (items == null || items.Count == 0)
                return "";

            if (items.Count == 1)
                return $"is {items[0]}";

            // For more than one item:
            var sb = new StringBuilder();
            sb.Append("are ");

            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    if (i == items.Count - 1)
                        sb.Append(", and ");
                    else
                        sb.Append(", ");
                }
                sb.Append(items[i]);
            }

            return sb.ToString();
        }
    }
}