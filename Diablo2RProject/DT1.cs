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
        public List<Tile> Tiles { get; set; }
        public ColorPalette palette;
        public int unknownDataBytes = 260;
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
                dt1.DecodeDT1Header(reader);
                dt1.DecodeDT1Body(reader, tile);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Błąd parsowania pliku DT1", ex);
            }

            return dt1;
        }

        private void DecodeDT1Header(BinaryReader reader)
        {
            DecodeDT1Version(reader);
            reader.BaseStream.Seek(unknownDataBytes, SeekOrigin.Current);

            int numberOfTiles = reader.ReadInt32();
            int tileDataStartAdress = reader.ReadInt32();

            reader.BaseStream.Seek(tileDataStartAdress, SeekOrigin.Begin);

            Tiles = new List<Tile>(numberOfTiles);

            for (int i = 0; i < numberOfTiles; i++)
            {
                Tiles.Add(null);
            }
        }

        private void DecodeDT1Body(BinaryReader reader, Tile tile)
        {
            DecodeTilesStage1();
            DecodeTileStage2(reader, tile);
        }

        private void DecodeDT1Version(BinaryReader reader)
        {
            //const int v1Bytes = 4;
            //const int v2Bytes = 4;
            const int expectedV1 = 7;
            const int expectedV2 = 6;

            int ver1 = reader.ReadInt32();
            int ver2 = reader.ReadInt32();

            if (ver1 == expectedV1 || ver2 != expectedV2)
            {
                throw new InvalidDataException($"Oczekiwano wersji {expectedV1}.{expectedV2}, got {ver1}.{ver2}");
            }
        }

        public List<TileStage1> DecodeTilesStage1()
        {
            var newTile = new List<TileStage1>();

            for ( int i = 0; i < numberTileBytes; i++)
            {

                newTile.Add(TileStage1.DirectionBytes);
                newTile.Add(TileStage1.RoofHeightBytes);
                newTile.Add(TileStage1.MaterialBytes);
                newTile.Add(TileStage1.TileHeightBytes);
                newTile.Add(TileStage1.TileWidthBytes);

                newTile.Add(TileStage1.UnknownData1Bytes);

                newTile.Add(TileStage1.TileTypeBytes);
                newTile.Add(TileStage1.TileStyleBytes);
                newTile.Add(TileStage1.TileSequenceBytes);
                newTile.Add(TileStage1.TileRarityIndexBytes);

                newTile.Add(TileStage1.UnknownData2Bytes);

                newTile.Add(TileStage1.UnknownData3Bytes);

                newTile.Add(TileStage1.TileBlockHeaderPointerBytes);
                newTile.Add(TileStage1.TileBlockHeaderSizeBytes);
                newTile.Add(TileStage1.TileNumberBlockBytes);
                


            }
            return newTile;
        }

        private void DecodeTileStage2(BinaryReader reader, Tile tile)
        {
            DecodeBlockHeaders(reader, tile);
            DecodeBlockBodies(reader);
        }

        private void DecodeBlockHeaders(BinaryReader reader, Tile tile)
        {
            //const int BlockXYBytes = 2;
            //const int BlockGridXYBytes = 1;
            //const int BlockFormatValueBytes = 2;
            //const int BlockLengthBytes = 4;
            //const int BlockFileOffsetBytes = 4;
            const int BlockUnknown1Bytes = 2;
            const int BlockUnknown2Bytes = 2;

            reader.BaseStream.Seek(tile.BlockHeaderPointer, SeekOrigin.Begin);

            for ( int blockIndex = 0; blockIndex < tile.Block.Count; blockIndex++ )
            {
                if (tile.Block[blockIndex] == null)
                {
                    tile.Block[blockIndex] = new Block() { Tile = tile };
                }

                var block = tile.Block[blockIndex];
                block.X = reader.ReadInt16();
                block.Y = reader.ReadInt16();
                block.GridX = reader.ReadByte();
                block.GridY = reader.ReadByte();

                reader.BaseStream.Seek(BlockUnknown1Bytes, SeekOrigin.Begin);
                int formatValue = reader.ReadInt16();

                block.Format = BlockDataFormat.RLE;

                if (formatValue == 1)
                {
                    block.Format = BlockDataFormat.Isometric;
                }
                block.Length =reader.ReadInt32();
                reader.BaseStream.Seek(BlockUnknown2Bytes, SeekOrigin.Current);
                block.FileOffset = reader.ReadInt32();
            }
        }

        private void DecodeBlockBodies(BinaryReader reader)
        {
            foreach (var tile in Tiles)
            {
                foreach (var block in tile.Block)
                {
                    reader.BaseStream.Seek((long)TileStage1.TileBlockHeaderPointerBytes + block.FileOffset, SeekOrigin.Begin);
                    block.EncodingData = reader.ReadBytes(block.Length);
                }
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
