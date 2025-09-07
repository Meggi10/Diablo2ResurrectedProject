using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Diablo2RProject
{
    public class Block
    {
        public Tile Tile {  get; set; }
        public int X {get; set;}
        public int Y {get; set;}
        public byte GridX {get; set;}
        public byte GridY {get; set;}
        public DT1.BlockDataFormat Format {get; set;}
        public byte[] EncodingData {get; set;}
        public int Length {get; set;}
        public int FileOffset {get; set;}
        //public byte[] Image {get; set;}
        public Bitmap Image {get; set;}
        public List<Color> Palette { get; set;}
        public int Width { get; set;}
        public int Height { get; set;}

        //public void DisplayImage()
        //{
        //    if (Width <= 0 || Height <= 0 || Image == null)
        //        return;
        //    Image = new Bitmap(Width, Height);

        //    for (int y = 0; y < Height; y++)
        //    {
        //        for (int x = 0; x < Width; x++)
        //        {
        //            Color pixelColor = At(x, y);
        //            Image.SetPixel(x, y, pixelColor);
        //        }
        //    }
        //}

        //public byte ColorIndexAt(int x, int y)
        //{
        //    int absIndex = y * Width + x;

        //    if (absIndex < 0 || absIndex >= Image.Length)
        //        return 0;

        //    return Image[absIndex];
        //}

        //private string ColorModel()
        //{
        //    return "RGBA";
        //}

        private Rectangle Bounds()
        {
            return new Rectangle(0, 0, Width, Height);
        }

        //public Color At(int x, int y)
        //{
        //    byte pallIndex = ColorIndexAt(x, y);

        //    if (pallIndex == 0)
        //        return Color.Transparent;

        //    if (Palette == null || pallIndex >= Palette.Count)
        //        return Color.Transparent;

        //    return Palette[pallIndex];
        //}
    }
}
