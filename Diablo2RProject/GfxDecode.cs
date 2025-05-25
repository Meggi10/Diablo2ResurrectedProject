using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class GfxDecode
    {
        private void DecodeTileGraphics(DT1 dt1)
        {
            int yOffset = DetermineYOffset(dt1);
            
            foreach (var tile in dt1.Tiles)
            {
                int tileWidth = tile.Width;
                int tileHeight = tile.Height;

                if (tileHeight < 0)
                    tileHeight = -tileHeight;

                foreach (var block in tile.Block)
                {
                    block.PixelData = new byte[tileWidth * tileHeight];
                    if (block.Format == DT1.BlockDataFormat.Isometric)
                    {
                        DecodeIsometric(block, tileWidth, yOffset);
                    }
                    else
                    {
                        DecodeRLE(block,tileWidth, yOffset);
                    }

                    block.Image = new System.Drawing.Bitmap(tileWidth, tileHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                }
            }
        }

        public int DetermineYOffset(DT1 dt1)
        {
            int yOffset = 0;
            foreach (var tile in dt1.Tiles)
            {
                foreach (var block in tile.Block)
                {
                    if (block.Y < yOffset)
                    {
                        yOffset = block.Y;
                    }
                }
            }

            return yOffset;
        }

        private void DecodeIsometric(Block block, int w, int yOffset)
        {
            //Block block = new Block();
            const int blockDataLength = 256;
            int[] xJump = { 14, 12, 10, 8, 6, 4, 2, 0, 2, 4, 6, 8, 10, 12, 14 };
            int[] nbPix = { 4, 8, 12, 16, 20, 24, 28, 32, 28, 24, 20, 16, 12, 8, 4 };
            int blockX = block.X;
            int blockY = block.Y;
            int length = blockDataLength;
            int x = 0;
            int y = 0;
            int index = 0;

            while (length > 0)
            {
                x = xJump[y];
                int n = nbPix[y];
                length -= n;

                while (n > 0)
                {
                    int offset = ((blockY + y + yOffset) * w) + (blockX + x);
                    if( offset >= 0 && offset < block.PixelData.Length && index < block.EncodingData.Length)
                    {
                        block.PixelData[offset] = block.EncodingData[index];
                    }

                    x++;
                    n--;
                    index++;
                }
                y++;
            }
        }

        private void DecodeRLE(Block block, int w, int yOffset)
        {
            //Block block = new Block();
            int X = block.X;
            int Y = block.Y;
            int x = 0;
            int y = 0;

            int index = 0;
            int length = block.Length;

            while (length > 0)
            {
                byte b1 = block.EncodingData[index];
                byte b2 = block.EncodingData[index + 1];
                index += 2;
                length -= 2;

                if ((b1 | b2) == 0)
                {
                    x = 0;
                    y++;
                    continue;
                }

                x += b1;
                length -= b2;

                while (b2 > 0)
                {
                    int offset = ((Y + y + yOffset) * w) + (X + x);
                    if (offset >= 0 && offset < block.PixelData.Length && index < block.EncodingData.Length)
                    {
                        block.PixelData[offset] = block.EncodingData[index];
                    }

                    index++;
                    x++;
                    b2--;
                }
            }

        }
    }
}
