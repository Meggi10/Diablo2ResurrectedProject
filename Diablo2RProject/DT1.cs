using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
                newTile.Type = reader.ReadInt32(); // first index
                newTile.Style = reader.ReadInt32(); // second index
                newTile.Sequence = reader.ReadInt32(); // third index
                newTile.RarityFrameIndex = reader.ReadInt32();
                reader.ReadInt32();
                
                for (int j = 0; j < newTile.Subtile.Length; j++)
                {
                    newTile.Subtile[j] = new Subtile(reader.ReadByte());
                }

                reader.ReadBytes(7);

                newTile.BlockHeaderPointer = reader.ReadInt32();
                newTile.BlockHeaderSize = reader.ReadInt32(); //BlockDatasLength
                
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
                    //pixmap[x, y] = DT1.DefaultPalette[brightness].ToArgb();
                    pixmap[x, y] = DefaultPalette[brightness].ToArgb();
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

        public void DecodeBlockBodies(BinaryReader reader, Tile tile)
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
            //var yOffset = DetermineYOffset();

            var tileWidth = tile.Width;
            var tileHeight = tile.Height;

            if (tileHeight < 0)
                tileHeight = -tileHeight;

            //Block block = tile.Block[0];
            //foreach (var block in tile.Blocks)
            block.PixelData = new byte[tileWidth * tileHeight];
            block.EncodingData = reader.ReadBytes(block.Length);
            block.Palette = DT1.Palette("act1\\pal.dat");

            //if (block.Palette != null && block.Palette.Count > 0)
            //{
            //    Console.WriteLine($"First 10 palette colors:");
            //    for (int i = 0; i < Math.Min(10, block.Palette.Count); i++)
            //    {
            //        var color = block.Palette[i];
            //        Console.WriteLine($"  [{i}]: R={color.R}, G={color.G}, B={color.B}");
            //    }
            //}

            if (block.Format == DT1.BlockDataFormat.Isometric)
            {
                DecodeIsometric(block, block.EncodingData);
                //DecodeIsometric(block, tileWidth, yOffset);
            }
            else
            {
                //block.Width = tileWidth; //linijki 325 oraz 326 powodują przerwanie programu przy większych plikach
                //block.Height = tileHeight; // długie ładowanie się "mapy"
                block.Width = 160;
                block.Height = 80;
                DecodeRLE(block, tileWidth);
                block.DisplayImage();
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

        //Algorytm RLE prawdobodobnie jest dobrze napisany i prawidłowo odczytuje dane
        public void DecodeRLE(Block block, int tileWidth)
        {
            int pixelWritten = 0;
            int outOfBoundsCount = 0;
            int totalPixels = block.Width * block.Height;

            //Console.WriteLine($"=== DecodeRLE START ===");
            //Console.WriteLine($"blockX: {block.X}, blockY: {block.Y}");
            //Console.WriteLine($"w: {w}, yOffset: {yOffset}");
            //Console.WriteLine($"PixelData.Length: {block.PixelData?.Length}");
            //Console.WriteLine($"EncodingData.Length: {block.EncodingData?.Length}");

            //Block block = new Block();
            int x = 0;
            int y = 0;
            //int blockX = block.X + x;
            //int blockY = block.Y + y; //Math.Abs(block.Y);

            int index = 0;
            int length = block.Length;

            while (length > 0)
            {
                byte b1 = block.EncodingData[index];
                byte b2 = block.EncodingData[index + 1];
                index += 2;
                length -= 2;

                //if (pixelWritten + outOfBoundsCount < 5)
                //{
                //Console.WriteLine($"b1: {b1}, b2: {b2}, x: {x}, y: {y}");
                //}

                //Console.WriteLine($"RLE step: b1={b1}, b2={b2}, x={x}, y={y}, index={index}, length={length}");

                if (b1 != 0 || b2 != 0)
                {
                    //Console.WriteLine($"New line at y: {y}");

                    x += b1;
                    length -= b2;
                    //Console.WriteLine($"Drawing {b2} pixels starting at x={x + b1}");
                    //x = 0;
                    //y++;
                    //continue;


                    //x += (int)b1;
                    //length -= (int)b2;

                    while (b2 > 0)
                    {
                        //int offset = ((blockY + y + yOffset) * w) + (blockX + x); //wcześniejszy offset
                        int offset = (y * tileWidth) + x;
                        //int baseY = Math.Abs(block.Y);
                        //int blockX = block.X + x;
                        //int blockY = (block.Y + baseY) + y;
                        //int offset = (blockY * tileWidth) + blockX;

                        //if (pixelWritten + outOfBoundsCount < 10)
                        //{
                        //    Console.WriteLine($"Calculating offset: blockY({blockY}) + y({y}) + yOffset({yOffset}) = {blockY + y + yOffset}");
                        //    Console.WriteLine($"  * w({w}) + blockX({blockX}) + x({x}) = offset {offset}");
                        //    Console.WriteLine($"  PixelData.Length: {block.PixelData?.Length}");
                        //}

                        if (offset >= 0 && offset < block.PixelData.Length && index < block.EncodingData.Length)
                        {
                            //block.PixelData[offset] = block.EncodingData[index];

                            byte colorIndex = block.EncodingData[index];
                            block.PixelData[offset] = colorIndex; //dane z PixelData są odczytywane
                            //block.At(x, y);
                            //block.At(blockX, blockY);
                            pixelWritten++;

                            //if (pixelWritten <= 5)
                            //{
                            //    Console.WriteLine($"Wrote pixel {pixelWritten}: colorIndex {colorIndex} at offset {offset}");
                            //}
                        }
                        else
                        {
                            outOfBoundsCount++;

                            //DEBUGGING - dlaczego poza zakresem
                            //if (outOfBoundsCount <= 5)
                            //{
                            //    Console.WriteLine($"Out of bounds #{outOfBoundsCount}: offset {offset}, PixelData.Length {block.PixelData?.Length}");
                            //}
                        }

                        index++;
                        x++;
                        b2--;
                    }
                }
                else
                {
                    x = 0;
                    y++;
                    //Console.WriteLine($"New line: x=0, y={y + 1}");
                }

                //for (int i = 0; i < b2; i++)
                //{
                //    int offset = ((blockY + y + yOffset) * w) + (blockX + x);
                //    if (offset >=0 && offset < block.PixelData.Length)
                //    {
                //        byte pixelValue = block.EncodingData[index];
                //        block.PixelData[offset] = pixelValue;
                //    }
                //    index++;
                //    x++;
                //}
            }
            Console.WriteLine($"=== DecodeRLE END ===");
            Console.WriteLine($"Pixels written: {pixelWritten}");
            Console.WriteLine($"Out of bounds: {outOfBoundsCount}");
            //Console.WriteLine($"First 10 PixelData values: {string.Join(",", block.PixelData?.Take(10) ?? new byte[0])}");
            Console.WriteLine($"Block pos: X={block.X}, Y={block.Y}, tileWidth={tileWidth}, PixelData.Length={block.PixelData.Length}");
            Console.WriteLine($"Fill percentage: {(pixelWritten * 100.0) / totalPixels:F1}%");
            Console.WriteLine($"Non-zero pixels: {block.PixelData.Count(p => p != 0)}");

        }

        public static List<Color> DefaultPalette;
        private static bool paletteLoaded = false;
        private static byte[] paletteData;
        //private static string fileName;

        //Problem może leżeć w braku konwersji surowych danych na rzeczywiste RGB
        public static void LoadPalette()
        {
            if (!paletteLoaded) //if (!paletteLoaded && paletteData != null)
            {
                //Palette(fileName);
                DefaultPalette = new List<Color>();
                DefaultPalette.Clear();
                for (int i = 0; i <= 768; i++)
                {
                    if (i * 3 + 2 < paletteData.Length)
                    {
                        Color from_idx = Color.FromArgb(255,
                            paletteData[i * 3], //R
                            paletteData[i * 3 + 1], //G
                            paletteData[i * 3 + 2]); //B
                        DefaultPalette.Add(from_idx);
                    }
                }
                paletteLoaded = true;
            }
        }

        public static void SetPaletteData(byte[] data)
        {
            paletteData = data;
            paletteLoaded = false;
        }

        //public static string ReadZString(BinaryReader reader)
        //{
        //    List<byte> bytes = new List<byte>();
        //    byte b;
        //    while ((b = reader.ReadByte()) != 0)
        //    {
        //        bytes.Add(b);
        //    }
        //    return Encoding.ASCII.GetString(bytes.ToArray());

            //var result = "";
            //var ch = reader.ReadChar();

            //while (ch != 0)
            //{
            //    result += ch;
            //    ch = reader.ReadChar();
            //}
            //return result;
        //}


        public static List<Color> Palette(string fileName)
        {
            //LoadPalette();
            //InitializePalette();
            //if (DefaultPalette == null || DefaultPalette.Count == 0)
            //{
            //    DefaultPalette = GetDefaultPalette();
            //}
            //return DefaultPalette;

            //var path = Path.GetDirectoryName(fileName) + "/";
            //var stream = new FileStream(fileName, FileMode.Open);
            //var reader = new BinaryReader(stream);
            //var paletteData = ReadZString(reader);
            //paletteData = path + Path.GetFileNameWithoutExtension(paletteData) + ".dat";
            //File.ReadAllBytes(paletteData);

            //LoadPalette(fileName);
            //var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            //var reader = new BinaryReader(stream);
            //string palleteName = ReadZString(reader);
            //string directory = Path.GetDirectoryName(fileName);
            //string fullPalettePath = Path.Combine(directory, palleteName + ".dat");

            
            //if (File.Exists(fullPalettePath))
            //{
            //var palette = new List<Color>();
                byte[] data = File.ReadAllBytes(fileName);

                if (data.Length >= 768)
                {
                    SetPaletteData(data);
                    LoadPalette();
                    //return DefaultPalette;
                }
            //else return GetDefaultPalette();
            //}

            //var palette = new List<Color>();
            //{
            //    if (palette == null)
            //    {
            //        palette = DefaultPalette;
            //    }
            //}

            //return GetDefaultPalette();
            return DefaultPalette;

        }

        static List<Color> GetDefaultPalette()
        {
            const int numberColors = 256;
            var palette = new List<Color>();

            for (int i = 0; i < numberColors; i++)
            {
                palette.Add(Color.FromArgb(255, i, i, i));
            }

            return palette;
        }

        //static DT1()
        //{
        //    DefaultPalette = GetDefaultPalette();
        //}
    }
}
