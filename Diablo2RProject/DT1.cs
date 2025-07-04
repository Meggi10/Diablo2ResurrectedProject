﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Diablo2RProject
{
    public class DT1
    {
        public List<Tile> Tiles = new List<Tile>();
        public ColorPalette palette;
        public int numberTileBytes = 4;
        public int dataAddressBytes = 4;

        public enum BlockDataFormat: int
        {
            RLE = 0, //Run-Length Encoding
            Isometric = 1
        }

        public enum TileStage1: int
        {
            DirectionBytes = 4,
            RoofHeightBytes = 2,
            MaterialBytes = 2,
            TileHeightBytes = 4,
            TileWidthBytes = TileHeightBytes,
            TileTypeBytes = 4,
            TileStyleBytes = 4,
            TileSequenceBytes = 4,
            TileRarityIndexBytes = 4,
            TileBlockHeaderPointerBytes = 4,
            TileBlockHeaderSizeBytes = 4,
            TileNumberBlockBytes = 4,
            UnknownData1Bytes = 4,
            UnknownData2Bytes = 4,
            UnknownData3Bytes = 7,
            UnknownData4Bytes = 12
        }

        public static DT1 FromBytes(byte[] fileData, Tile tile)
        {
            var dt1 = new DT1();
            var stream = new MemoryStream(fileData);
            var reader = new BinaryReader(stream);

            try
            {
                var valid = dt1.DecodeDT1Header(reader);
                if (!valid)
                {
                    reader.Close();
                    return dt1;
                }
                dt1.DecodeDT1Body(reader, tile);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Błąd parsowania pliku DT1", ex);
            }
            reader.Close();
            //na razie żeby zobaczyć co było OK
            //GfxDecode gfxdecode = new GfxDecode();
            //gfxdecode.DecodeTileGraphics(dt1);
            return dt1;
        }

        private bool DecodeDT1Header(BinaryReader reader)
        {
            var valid = DecodeDT1Version(reader);
            if (!valid)
            {
                return false;
            }

            int unknownDataBytes = 260;
            //reader.BaseStream.Seek(unknownDataBytes, SeekOrigin.Current);
            reader.ReadBytes(unknownDataBytes);

            int numberOfTiles = reader.ReadInt32();
            int tileDataStartAdress = reader.ReadInt32();

            reader.BaseStream.Seek(tileDataStartAdress, SeekOrigin.Begin);

            Tiles = new List<Tile>(numberOfTiles);

            for (int i = 0; i < numberOfTiles; i++)
            {
                Tiles.Add(null);
            }
            return true;
        }

        private void DecodeDT1Body(BinaryReader reader, Tile tile)
        {
            DecodeTilesStage1(reader);
            DecodeTileStage2(reader);
        }

        private bool DecodeDT1Version(BinaryReader reader)
        {
            //const int v1Bytes = 4;
            //const int v2Bytes = 4;
            //const int expectedV1 = 7;
            //const int expectedV2 = 6;

            int ver1 = reader.ReadInt32();
            int ver2 = reader.ReadInt32();

            //if (!((ver1 == 7 && ver2 == 6) || (ver1 == 4 && ver2 == 1)))
            //{
            //    throw new InvalidDataException($"Nieobsługiwana wersja DT1: {ver1}.{ver2}");
            //}
            if(ver1  != 7 || ver2 != 6 )
            {
                return false;
            }
            return true;
        }

        public void DecodeTilesStage1(BinaryReader reader)
        {
            for ( int i = 0; i < Tiles.Count; i++)
            {
                var newTile = new Tile(this);
                newTile.Direction = reader.ReadInt32();
                newTile.RoofHeight = reader.ReadInt16();
                newTile.Material = new Material();
                newTile.Material.Flags = (Material.MaterialFlags)reader.ReadInt16();
                newTile.Height = reader.ReadInt32();
                newTile.Width = reader.ReadInt32();
                reader.ReadInt32();
                newTile.Type = reader.ReadInt32();
                newTile.Style = reader.ReadInt32();
                newTile.Sequence = reader.ReadInt32();
                newTile.RarityFrameIndex = reader.ReadInt32();
                reader.ReadInt32();
                
                for (int j = 0; j < newTile.Subtile.Length; j++)
                {
                    newTile.Subtile[j] = new Subtile(reader.ReadByte());
                }

                reader.ReadBytes(7);

                newTile.BlockHeaderPointer = reader.ReadInt32();
                newTile.BlockHeaderSize = reader.ReadInt32();
                
                var blockCount = reader.ReadInt32();
                for (int j = 0; j < blockCount; j++)
                {
                    newTile.Block.Add(new Block());
                }
                reader.ReadBytes(12);
                Tiles[i] = newTile;
            }
        }

        private void DecodeTileStage2(BinaryReader reader)
        {
            foreach(var tile in Tiles)
            {
                DecodeBlockHeaders(reader, tile);
                DecodeBlockBodies(reader, tile);
            }
        }

        private void DecodeIsometric(Block block, int w, int yOffset)
        {
            //Block block = new Block();
            const int blockDataLength = 256;
            int[] xJump = { 14, 12, 10, 8, 6,   4,  2,  0,  2,  4,  6, 8, 10, 12, 14 }; //16 - nbPix /2
            int[] nbPix = { 4,  8,  12, 16, 20, 24, 28, 32, 28, 24, 20, 16, 12, 8, 4 };
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
                    if (offset >= 0 && offset < block.PixelData.Length && index < block.EncodingData.Length)
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

        public void DecodeIsometric(Block block, byte[] pixels)
        {
            //var tile = block.Tile;
            var blockWidth = 32;
            var blockHeight = 15;

            int pos = 0;
            var pixmap = new TPixmap(blockWidth, blockHeight);
            for (int y = 0; y < pixmap.Height; y++)
            {
                var n = y < pixmap.Height / 2 ? y : pixmap.Height - 1 - y;
                var r = 2 + 2 * n;
                for (int x = pixmap.Width / 2 - r; x < pixmap.Width / 2 + r; x++)
                {
                    var brightness = pixels[pos++];
                    pixmap[x, y] = DT1.DefaultPalette[brightness].ToArgb();
                }
            }
            block.Image = pixmap.Image;
        }

        private void DecodeBlockHeaders(BinaryReader reader, Tile tile)
        {
            //const int BlockXYBytes = 2;
            //const int BlockGridXYBytes = 1;
            //const int BlockFormatValueBytes = 2;
            //const int BlockLengthBytes = 4;
            //const int BlockFileOffsetBytes = 4;
            //const int BlockUnknown1Bytes = 2;
            //const int BlockUnknown2Bytes = 2;

            //reader.BaseStream.Seek(tile.BlockHeaderPointer, SeekOrigin.Begin);
            reader.BaseStream.Position = tile.BlockHeaderPointer;

            for ( int blockIndex = 0; blockIndex < tile.Block.Count; blockIndex++ )
            {
                if (tile.Block[blockIndex] == null)
                {
                    tile.Block[blockIndex] = new Block() { Tile = tile };
                }

                var block = tile.Block[blockIndex];
                block.Tile = tile;
                block.X = reader.ReadInt16();
                block.Y = reader.ReadInt16();
                reader.ReadInt16();
                block.GridX = reader.ReadByte();
                block.GridY = reader.ReadByte();

                int formatValue = reader.ReadInt16();

                block.Format = BlockDataFormat.RLE;

                if (formatValue == 1)
                {
                    block.Format = BlockDataFormat.Isometric;
                }
                block.Length =reader.ReadInt32();
                reader.ReadInt16();
                block.FileOffset = reader.ReadInt32();
            }
        }

        private void DecodeBlockBodies(BinaryReader reader, Tile tile)
        {
            
                foreach (var block in tile.Block)
                {
                    reader.BaseStream.Position = tile.BlockHeaderPointer + block.FileOffset;
                //block.EncodingData = reader.ReadBytes(block.Length);
                    
                    DecodeTileGraphics(reader, block);
                }
            
        }

        public void DecodeTileGraphics(BinaryReader reader, Block block)
        {
            var tile = block.Tile;
            var yOffset = DetermineYOffset();

            var tileWidth = tile.Width;
            var tileHeight = tile.Height;

            if (tileHeight < 0)
                tileHeight = -tileHeight;

            //Block block = tile.Block[0];
            //foreach (var block in tile.Blocks)
            block.PixelData = new byte[tileWidth * tileHeight];
            block.EncodingData = reader.ReadBytes(block.Length);
            if (block.Format == DT1.BlockDataFormat.Isometric)
            {
                DecodeIsometric(block, block.EncodingData);
                //DecodeIsometric(block, tileWidth, yOffset);
            }
            else
            {
                DecodeRLE(block, tileWidth, yOffset);
            }

            //block.Image = new System.Drawing.Bitmap(tileWidth, tileHeight);

            //for (int x = 0; x < tileWidth; x++)
            //{
            //    for (int y = 0; y < tileHeight; y++)
            //    {
            //        var brightness = block.PixelData[y * tileWidth + x];
            //        block.Image.SetPixel(x, y, DT1.DefaultPalette()[brightness]);
            //    }
            //}
        }

        public int DetermineYOffset()
        {
            int yOffset = 0;
            foreach (var tile in Tiles)
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

        public static List<Color> Palette()
        {
            var palette = new List<Color>();
            {
                if (palette == null)
                {
                    palette = DefaultPalette;
                }
            }
            return palette;
        }

        public static List<Color> DefaultPalette;

        static List<Color> GetDefaultPalette()
        {
            const int numberColors = 256;
            var palette = new List<Color>();

            for ( int i = 0; i < numberColors; i++)
            {
                palette.Add(Color.FromArgb(255, i, i, i));
            }

            return palette;
        }

        static DT1()
        {
            DefaultPalette = GetDefaultPalette();
        }
    }
}
