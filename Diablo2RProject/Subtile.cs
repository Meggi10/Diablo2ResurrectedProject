using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class Subtile
    {
        public bool BlockWalk {  get; set; }
        public bool BlockLOS { get; set; }
        public bool BlockJump { get; set; }
        public bool BlockPlayerWalk { get; set; }
        public bool Unknown1 { get; set; }
        public bool BlockLight { get; set; }
        public bool Unknown2 { get; set; }
        public bool Unknown3 { get; set; }

        private void Combine(Subtile other)
        {
            BlockWalk = BlockWalk || other.BlockWalk;
            BlockLOS = BlockLOS || other.BlockLOS;
            BlockJump = BlockJump || other.BlockJump;
            BlockPlayerWalk = BlockPlayerWalk || other.BlockPlayerWalk;
            Unknown1 = Unknown1 ||other.Unknown1;
            BlockLight = BlockLight || other.BlockLight;
            Unknown2 = Unknown2 || other.Unknown2;
            Unknown3 = Unknown3 || other.Unknown3;
        }

        private void NewSubtileFlags(byte data)
        {
            BlockWalk = (data & 1) == 1;
            BlockLOS = (data & 2)== 2;
            BlockJump = (data & 4 )== 4;
            BlockPlayerWalk = (data & 8) == 8;
            Unknown1 = (data & 16) == 16;
            BlockLight = (data & 32) == 32;
            Unknown2 = (data & 64) == 64;
            Unknown3 = (data & 128) == 128;
        }

        public Subtile(byte data)
        {
            NewSubtileFlags(data);
        }
    }
}
