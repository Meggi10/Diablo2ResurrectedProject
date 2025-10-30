using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Diablo2RProject.Diablo
{
    internal class DiabloAnimation: TAnimation
    {
        class MacroBlock
        {
            public int[] PixelValues = new int[4];
            public int PosX;
            public int PosY;
            public int Width, Height;
            public bool IsStill;
        }

        public string Token;
        public string Mode;
        public string ClassType;
        public string BasePath;
        public int PaletteIdx;
        byte LayersCount;
        byte FramesCount;
        byte DirectionCount;
        byte[] priority;
        byte[] Palette;
        string[] possiblePaths = { "monsters", "objects" };
        //string basePath = null;
        //string fileName = null;
        public List<string> Armor = new List<string>(); // działa dla map
        //public List<string> Armor = new List<string>(new string[16]); //działa dla samych plików animacji
        //public List<string> Armor = new List<string>(LayerNames.Length); //tutaj Count dla Armor zawsze był 0
        //public string[] Armor = new string[LayerNames.Length];
        public static string[] LayerNames = {
            "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH",
            "S1", "S2", "S3","S4", "S5", "S6", "S7", "S8"};
        static int[] FrameBitsCount = { 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 32 };
        static int[] BitsCount = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };
        //public static int[] Directions = {
        //              4, 16,  8, 17,  0, 18,  9, 19,
        //              5, 20, 10, 21,  1, 22, 11, 23,
        //              6, 24, 12, 25,  2, 26, 13, 27,
        //              7, 28, 14, 29,  3, 30, 15, 31 };
        Rectangle[] LayerDirBounds;

        void ReadDcc(Stream s)
        {
            var reader = new BinaryReader(s);
            var signature = reader.ReadByte();
            var version = reader.ReadByte();
            var directionsCount = reader.ReadByte();
            var framesPerDirection = reader.ReadInt32();
            var tag = reader.ReadInt32();
            var finalDc6Size = reader.ReadInt32();
            var dirOffsets = new int[directionsCount + 1];
            for (int d = 0; d < directionsCount; d++)
                dirOffsets[d] = reader.ReadInt32();
            dirOffsets[directionsCount] = (int)s.Length;
            var seq = new TFrame[directionsCount][];
            Sequences.Add(seq);
            LayerDirBounds = new Rectangle[directionsCount];

            for (int d = 0; d < seq.Length; d++)
            {
                var bitStreamSize = dirOffsets[d + 1] - dirOffsets[d];
                var bs = new BitStream(reader.ReadBytes(bitStreamSize));
                bs.Size = bitStreamSize;
                var outsizeCoded = bs.Read(32);
                var hasRawBlocks = bs.ReadBool();
                var hasStillBlocks = bs.ReadBool();
                var variable0Bits = bs.Read(4);
                var widthBits = bs.Read(4);
                var heightBits = bs.Read(4);
                var xOffsetBits = bs.Read(4);
                var yOffsetBits = bs.Read(4);
                var optionalBytesBits = bs.Read(4);
                var codedBytesBits = bs.Read(4);
                var frames = new TFrame[framesPerDirection];
                seq[d] = frames;
                var optionalBytesCount = 0;
                var boundingBox = new Rectangle();
                for (var f = 0; f < frames.Length; f++)
                {
                    var frame = new TFrame();
                    frames[f] = frame;
                    var variable0 = bs.Read(FrameBitsCount[variable0Bits]);
                    frame.Bounds.Width = bs.Read(FrameBitsCount[widthBits]);
                    frame.Bounds.Height = bs.Read(FrameBitsCount[heightBits]);
                    frame.Bounds.X = bs.ReadSigned(FrameBitsCount[xOffsetBits]);
                    frame.Bounds.Y = bs.ReadSigned(FrameBitsCount[yOffsetBits]);
                    optionalBytesCount += bs.ReadSigned(FrameBitsCount[optionalBytesBits]);
                    var codedBytes = bs.Read(FrameBitsCount[codedBytesBits]);
                    var isBottomUp = bs.ReadBool();
                    if (!isBottomUp)
                        frame.Bounds.Y -= frame.Bounds.Height - 1;
                    if(f == 0)
                        boundingBox = frame.Bounds;
                    else
                        boundingBox = Rectangle.Union(boundingBox, frame.Bounds);
                }
                LayerDirBounds[d] = boundingBox;
                reader.ReadBytes(optionalBytesCount);

                var stillBlocksStream = new BitStream(bs.Buffer);
                if (hasStillBlocks)
                    stillBlocksStream.Size = bs.Read(20);
                var pixelMaskStream = new BitStream(bs.Buffer);
                pixelMaskStream.Size = bs.Read(20);
                var encodeTypeStream = new BitStream(bs.Buffer);
                var rawBlocksStream = new BitStream(bs.Buffer);
                if (hasRawBlocks)
                {
                    encodeTypeStream.Size = bs.Read(20);
                    rawBlocksStream.Size = bs.Read(20);
                }
                var pixelCodesStream = new BitStream(bs.Buffer);
                var pixelBlock = new List<int>(256);
                for (int i = 0; i < pixelBlock.Capacity; i++)
                    if (bs.ReadBool())
                        pixelBlock.Add(i);

                stillBlocksStream.Position = bs.Position;
                pixelMaskStream.Position = stillBlocksStream.Position + stillBlocksStream.Size;
                encodeTypeStream.Position = pixelMaskStream.Position + pixelMaskStream.Size;
                rawBlocksStream.Position = encodeTypeStream.Position + encodeTypeStream.Size;
                pixelCodesStream.Position = rawBlocksStream.Position + rawBlocksStream.Size;
                //Stage 1
                var pbBlockHeight = (boundingBox.Height + 3) / 4;
                var pbBlockWidth = (boundingBox.Width + 3) / 4;
                var pixelBuffer = new int[pbBlockHeight, pbBlockWidth][];
                var framesBlocks = new MacroBlock[frames.Length][,];
                for(int f = 0; f < frames.Length; f++)
                {
                    var frame = frames[f];
                    frame.Offset.X = frame.Bounds.X - boundingBox.X;
                    frame.Offset.Y = frame.Bounds.Y - boundingBox.Y;
                    var blocks = CreateMacroBlocks(frame);
                    framesBlocks[f] = blocks;
                    var blocksOffsetX = frame.Offset.X / 4;
                    var blocksOffsetY = frame.Offset.Y / 4;
                    for (int blockY = 0; blockY < blocks.GetLength(0); blockY++)
                        for (int blockX = 0; blockX < blocks.GetLength(1); blockX++)
                        {
                            var block = blocks[blockY, blockX];
                            var bufBlockX = blockX + blocksOffsetX;
                            var bufBlockY = blockY + blocksOffsetY;
                            var pixelMask = 0xF;
                            var prevPixelValues = pixelBuffer[bufBlockY, bufBlockX];
                            if (prevPixelValues != null)
                            {
                                if (stillBlocksStream.Size == 0 || !stillBlocksStream.ReadBool())
                                    pixelMask = pixelMaskStream.Read(4);
                                else
                                {
                                    block.IsStill = true;
                                    continue;
                                }
                                
                            }
                            var stackPixels = new int[4];
                            var currPixel = 0;
                            var stackPixelsCount = BitsCount[pixelMask];
                            var encodingType = 0;
                            if (stackPixelsCount > 0 && encodeTypeStream.Size > 0)
                                encodingType = encodeTypeStream.Read(1);

                            for (int i = 0; i < stackPixelsCount; i++)
                            {
                                if (encodingType > 0)
                                    stackPixels[i] = rawBlocksStream.Read(8);
                                else
                                {
                                    stackPixels[i] = currPixel;
                                    var displacement = 0;
                                    do
                                    {
                                        displacement = pixelCodesStream.Read(4);
                                        stackPixels[i] += displacement;
                                    }
                                    while (displacement == 0xF);
                                }
                                if (stackPixels[i] == currPixel)
                                {
                                    stackPixelsCount = i;
                                    break;
                                }
                                currPixel = stackPixels[i];
                            }
                            var pixelValues = new int[4];
                            for (int i = 0; i < pixelValues.Length; i++)
                            {
                                if ((pixelMask >> i & 1) != 0)
                                {
                                    var code = stackPixelsCount > 0 ? stackPixels[--stackPixelsCount] : 0;
                                    pixelValues[i] = pixelBlock[code];
                                }
                                else
                                    pixelValues[i] = prevPixelValues[i];
                            }
                            block.PixelValues = pixelValues;
                            pixelBuffer[bufBlockY, bufBlockX] = pixelValues;
                        }
                }
                //Stage 2
                TPixmap prevPixmap = null;
                for (int f = 0; f < frames.Length; f++)
                {
                    var pixMap = new TPixmap(boundingBox.Width, boundingBox.Height);
                    var blocks = framesBlocks[f];
                    for (int blockY = 0; blockY < blocks.GetLength(0); blockY++)
                    {
                        for (int blockX = 0; blockX < blocks.GetLength(1); blockX++)
                        {
                            var block = blocks[blockY, blockX];
                            for (int y = 0; y < block.Height; y++)
                                for (int x = 0; x < block.Width; x++)
                                {
                                    int color = 0;
                                    if (block.IsStill)
                                    {
                                        color = prevPixmap[block.PosX + x, block.PosY + y];
                                    }
                                    else
                                    {
                                        var bitCount = 2;
                                        if (block.PixelValues[0] == block.PixelValues[1])
                                            bitCount = 0;
                                        else if (block.PixelValues[1] == block.PixelValues[2])
                                            bitCount = 1;

                                        var code = pixelCodesStream.Read(bitCount);
                                        color = TDiabloMap.Palette[block.PixelValues[code]];
                                    }
                                    pixMap[block.PosX + x, block.PosY + y] = color;
                                }
                        }
                    }
                    frames[f].Image = pixMap.Image;
                    prevPixmap = pixMap;
                }
            }
        }

        MacroBlock[,] CreateMacroBlocks(TFrame frame)
        {
            int heightFirstRow = 4 - (frame.Offset.Y & 3);
            var currHeight = frame.Bounds.Height - heightFirstRow;
            int blockYCount = currHeight > 1 ? (currHeight + 6) >> 2 : 1;

            int widthFirstColumn = 4 - (frame.Offset.X & 3);
            var currWidth = frame.Bounds.Width - widthFirstColumn;
            int blockXCount = currWidth > 1 ? (currWidth + 6) >> 2 : 1;

            var blocks = new MacroBlock[blockYCount, blockXCount];
            currHeight = 0;
            for (int y = 0; y < blockYCount; y++)
            {
                currWidth = 0;
                for (int x = 0; x < blockXCount; x++)
                {
                    var block = new MacroBlock();
                    block.PosX = currWidth + frame.Offset.X;
                    block.PosY = currHeight + frame.Offset.Y;
                    block.Width = 4;
                    block.Height = 4;
                    if (y == 0) block.Height = heightFirstRow;
                    if (x == 0) block.Width = widthFirstColumn;
                    if (y == blockYCount - 1) block.Height = frame.Bounds.Height - currHeight;
                    if (x == blockXCount - 1) block.Width = frame.Bounds.Width - currWidth;
                    currWidth += block.Width;
                    blocks[y, x] = block;
                }
                currHeight += blocks[y, 0].Height;
            }
            return blocks;
        }

        public void ReadDc6(Stream s)
        {
            var reader = new BinaryReader(s);
            var version = reader.ReadInt32();
            var flags = reader.ReadInt32();
            var format = reader.ReadInt32();
            var d2CMPColor = reader.ReadInt32();
            var directionsCount = reader.ReadInt32();
            var framesCount = reader.ReadInt32();
            var frameHeadersFilePos = new int[directionsCount * framesCount];
            for (int i = 0; i < frameHeadersFilePos.Length; i++)
                frameHeadersFilePos[i] = reader.ReadInt32();
            var framesBlocksCount = new int[directionsCount, framesCount];
            var seq = new TFrame[directionsCount][];
            Sequences.Add(seq);
            LayerDirBounds = new Rectangle[directionsCount];
            for (int d = 0; d < directionsCount; d++)
            {
                var dir = new TFrame[framesCount];
                seq[d] = dir;
                var bounds = new Rectangle();
                for (int f = 0; f < framesCount; f++)
                {
                    reader.ReadBytes(frameHeadersFilePos[d * framesCount + f] - (int)reader.BaseStream.Position);
                    var frame = new TFrame();
                    var frmBottomUp = reader.ReadInt32();
                    frame.Bounds.Width = reader.ReadInt32();
                    frame.Bounds.Height = reader.ReadInt32();
                    frame.Bounds.X = reader.ReadInt32();
                    frame.Bounds.Y = reader.ReadInt32();
                    if (frmBottomUp == 0)
                        frame.Bounds.Y -= frame.Bounds.Height - 1;
                    var frmDecodedSize = reader.ReadInt32();
                    var nextBlock = reader.ReadInt32();
                    framesBlocksCount[d, f] = reader.ReadInt32();
                    if (f == 0) bounds = frame.Bounds;
                    else bounds = Rectangle.Union(bounds, frame.Bounds);
                    // Eat any leading 0s. Blizzard somehow changed and fucked up the encoding or serialization in D2:Remaster
                    // The 3 additional trailing bytes that used to be garbage at the end of the data are now replaced with leading 0s
                    // Those are NOT counted by the FrameHeader::length member, so ignore them
                    int x = 0;
                    int y = frame.Bounds.Height - 1;
                    var pixmap = new TPixmap(frame.Bounds.Width, frame.Bounds.Height);
                    for (int rawIdx = 0; rawIdx < framesBlocksCount[d, f]; rawIdx++)
                    {
                        var blockSize = reader.ReadByte();
                        if (blockSize == 0x80)
                        {
                            x = 0;
                            y--;
                            if (y < 0)
                                break;
                        }
                        else if ((blockSize & 0x80) != 0)
                        {
                            x += (blockSize & 0x7F);
                        }
                        else
                        {
                            for (var i = 0; i < blockSize; i++)
                            {
                                pixmap[x + i, y] = TDiabloMap.Palette[reader.ReadByte()];
                            }
                            x += blockSize;
                        }
                    }
                    reader.ReadBytes(3);
                    LayerDirBounds[d] = bounds;
                    frame.Image = pixmap.Image;
                    dir[f] = frame;
                }
            }
        }

        //COF files
        public void Read()
        {
            Name = $"{Token}{Mode}{ClassType}";
            var basePath = $"{TDiabloMap.GamePath}/D2/data/global/{BasePath}/{Token}/";
            var fileName = $"{basePath}cof/{Name}.cof";
            foreach (var path in possiblePaths)
            {
                basePath = $"{TDiabloMap.GamePath}/D2/data/global/{path}/{Token}/";
                fileName = $"{basePath}cof/{Name}.cof";

                if (File.Exists(fileName))
                {
                    BasePath = path;
                    break;
                }
            }
            var s = new FileStream(fileName, FileMode.Open);
            var reader = new BinaryReader(s);
            var palPath = TDiabloMap.GamePath + "/D2/data/global/monsters/randtransforms.dat";

            if (PaletteIdx > 0)
            {
                if (PaletteIdx > 30)
                    palPath = basePath + "cof/palshift.dat";
                Palette = new byte[256];
                var buffer = File.ReadAllBytes(palPath);
                Array.Copy(buffer, 256 * (PaletteIdx - 1), Palette, 0, Palette.Length);
            }

            LayersCount = reader.ReadByte();
            FramesCount = reader.ReadByte();
            DirectionCount = reader.ReadByte();
            reader.ReadBytes(25);
            var dirBounds = new Rectangle[DirectionCount];
            for (var i = 0; i < LayersCount; i++)
            {
                var armorIdx = reader.ReadByte();
                var shad_a = reader.ReadByte();
                var shad_b = reader.ReadByte();
                var transparency_a = reader.ReadByte();
                var transparency_b = reader.ReadByte();
                var wclass = TDiabloMap.ReadZString(reader);
                var armor = Armor[armorIdx];
                var layerType = LayerNames[armorIdx];
                var dccName = $"{basePath}{layerType}/{Token}{layerType}{armor}{Mode}{wclass}.dcc";

                if (File.Exists(dccName))
                    ReadDcc(new FileStream(dccName, FileMode.Open));
                else
                {
                    dccName = dccName.Substring(0, dccName.Length - 4) + ".dc6";
                    ReadDc6(new FileStream(dccName, FileMode.Open));
                }
                if (i == 0)
                    dirBounds = (Rectangle[])LayerDirBounds.Clone();
                else
                    for (int d = 0; d < DirectionCount; d++)
                    {
                        dirBounds[d] = Rectangle.Union(dirBounds[d], LayerDirBounds[d]);
                    }
            }
            // skip flags of each frames
            reader.ReadBytes(FramesCount);
            var seq = new TFrame[DirectionCount][];
            for (int d = 0; d < DirectionCount; d++)
            {
                var frames = new TFrame[FramesCount];
                for (int f = 0; f < FramesCount; f++)
                {
                    var frame = new TFrame();
                    frame.Bounds = dirBounds[d];
                    frame.Image = new Bitmap(frame.Bounds.Width, frame.Bounds.Height);
                    var gc = Graphics.FromImage(frame.Image);
                    for (int i = 0; i < Sequences.Count; i++)
                    {
                        var layerFrame = Sequences[i][d][f];
                        var offsetX = layerFrame.Bounds.X - layerFrame.Offset.X - frame.Bounds.X;
                        var offsetY = layerFrame.Bounds.Y - layerFrame.Offset.Y - frame.Bounds.Y;
                        gc.DrawImage(layerFrame.Image, offsetX, offsetY);
                    }
                    frames[f] = frame;
                }
                seq[d] = frames;
            }
            Sequences.Clear();
            Sequences.Add(seq);

            // priority layer
            priority = reader.ReadBytes(DirectionCount * FramesCount * LayersCount);
        }
    }
}
