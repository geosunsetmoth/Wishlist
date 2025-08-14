using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wishlist
{
    public class Spell
    {
        public string? name { get; set; }
        public int level { get; set; }
        public HashSet<int> damageTypes { get; set; }
        public bool HasBeenLearned { get; set; }
        public bool IsOnWishList { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not Spell other) return false;
            return name == other.name && level == other.level;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name, level);
        }
    }

    public class ToolsWrapper
    {
        public string? name { get; set; }
        public int level { get; set; }
        public List<string>? damageInflict { get; set; }
    }
}

