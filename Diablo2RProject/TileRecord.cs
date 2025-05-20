using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class TileRecord
    {
        public List<FloorShadowRecord> FloorsShadow = new List<FloorShadowRecord>();
        public List<WallRecord> Walls = new List<WallRecord>();
        public List<FloorShadowRecord> Shadows = new List<FloorShadowRecord>();
        public List<SubstitutionRecord> Substitutions = new List<SubstitutionRecord>();

        public TileType? Type { get; set; }
    }
}
