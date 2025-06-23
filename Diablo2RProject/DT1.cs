using System;
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
                newTile.Material.Flags = (Material.MaterialFlags)reader.ReadInt16(); //tu się program "wywala"
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
                    block.EncodingData = reader.ReadBytes(block.Length);
                }
            
        }

        public static List<Color> Palette()
        {
            var palette = new List<Color>();
            {
                if (palette == null)
                {
                    palette = DefaultPalette();
                }
            }
            return palette;
        }

        public static List<Color> DefaultPalette()
        {
            const int numberColors = 256;
            var palette = new List<Color>();

            for ( int i = 0; i < numberColors; i++)
            {
                palette.Add(Color.FromArgb(255, i, i, i));
            }

            return palette;
        }
    }
}
