using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class WallRecord
    {
        public TileType Type { get; set; }
        public byte Zero {  get; set; }
        public byte Property1 { get; set; }
        public byte Sequence { get; set; }
        public byte Unknown1 { get; set; }
        public byte Style {  get; set; }
        public byte Unknown2 { get; set; }
        public bool Hidden { get; set; }
        public byte RandomIndex { get; set; }
        public int YAdjust { get; set; }
        public Tile Tile;
    }
}
