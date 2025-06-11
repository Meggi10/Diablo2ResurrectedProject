using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class Tile
    {
        public DT1 Dt1 {  get; set; }
        public int Direction { get; set; }
        public int RoofHeight { get; set; }
        public Material Material { get; set; } = new Material();
        public int Height { get; set; }
        public int Width { get; set; }
        public int Type { get; set; }
        public int Style { get; set; }
        public int Sequence { get; set; }
        public int RarityFrameIndex { get; set; }
        public Subtile[] Subtile { get; set; } = new Subtile[25];
        private int blockHeaderPointer;
        private int blockHeaderSize;
        public List<Block> Block {  get; set; } = new List<Block>();

        public Tile(DT1 dt1)
        {
            Dt1 = dt1;
        }

        public int BlockHeaderPointer
        {
            get => blockHeaderPointer;
            set => blockHeaderPointer = value;
        }

        public int BlockHeaderSize
        {
            get => blockHeaderSize;
            set => blockHeaderSize = value;
        }
    }
}
