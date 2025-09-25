using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        struct LAY_INF_S
        {
            byte shad_a;
            byte shad_b;
            byte trans_a;
            byte trans_b;
            string wclass;

            // editor only
            int bmp_num;
            //BITMAP** bmp;
            int off_x;
            int off_y;
            int last_good_frame;
        }

        public string Token;
        public string Mode;
        public string ClassType;
        public string BasePath;
        public int PaletteIdx;
        byte LayersCount;
        byte FramesCount;
        byte DirectionCount;
        int xoffset;
        int yoffset;
        LAY_INF_S[] lay_inf;
        byte[] priority;
        int cur_frame;
        int cur_dir;
        int spd_mul;
        int spd_div;
        int spd_mod; // = is (mul % div), for extra precision
        int orderflag; // from data\global\excel\objects.txt, 0 1 or 2
        byte[] Palette;
        public List<string> Armor = new List<string>();
        static string[] LayerType = {
            "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH",
            "S1", "S2", "S3","S4", "S5", "S6", "S7", "S8"};
        static int[] FrameBitsCount = { 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 32 };
        static int[] BitsCount = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };
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
                for (int f = 0; f < frames.Length; f++)
                {
                    var pixMap = new TPixmap(boundingBox.Width, boundingBox.Height);
                    var blocks = framesBlocks[f];
                    for (int blockY = 0; blockY < blocks.GetLength(0); blockY++)
                        for (int blockX = 0; blockX < blocks.GetLength(1); blockX++)
                        {
                            var block = blocks[blockY, blockX];
                            for (int y = 0; y <block.Height; y++)
                                for (int x = 0; x < block.Width; x++)
                                {
                                    if (block.IsStill)
                                    {

                                    }
                                    else
                                    {
                                        var bitCount = 2;
                                        if (block.PixelValues[0] == block.PixelValues[1])
                                            bitCount = 0;
                                        else if (block.PixelValues[1] == block.PixelValues[2])
                                            bitCount = 1;

                                        var code = pixelCodesStream.Read(bitCount);
                                        var color = TDiabloMap.Palette[block.PixelValues[code]];
                                        pixMap[block.PosX + x, block.PosY + y] = color;
                                    }
                                }
                        }
                    frames[f].Image = pixMap.Image;
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
                    block.PosX = currWidth;
                    block.PosY = currHeight;
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

        void ReadDc6(Stream s)
        {

        }

        public void Read()
        {
            Name = $"{Token}{Mode}{ClassType}";
            var basePath = $"{TDiabloMap.GamePath}/D2/data/global/{BasePath}/{Token}/";
            var fileName = $"{basePath}cof/{Name}.cof";
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

            // layers
            LayersCount = reader.ReadByte();

            // frames per direction
            FramesCount = reader.ReadByte();

            // directions
            DirectionCount = reader.ReadByte();

            // skip 25 unknown bytes
            reader.ReadBytes(25);

            // layers infos
            var dirBounds = new Rectangle[DirectionCount];
            for (var i = 0; i < LayersCount; i++)
            {
                // composit index
                var armorIdx = reader.ReadByte();

                // shadows
                var shad_a = reader.ReadByte();
                var shad_b = reader.ReadByte();
                var transparency_a = reader.ReadByte();
                var transparency_b = reader.ReadByte();

                // weapon class (used to know a part of the dcc name)
                var wclass = TDiabloMap.ReadZString(reader);

                // dcc / dc6
                var armor = Armor[armorIdx];
                var layerType = LayerType[armorIdx];
                var dccName = $"{basePath}{layerType}/{Token}{layerType}{armor}{Mode}{wclass}.dcc";

                if (File.Exists(dccName))
                {
                    ReadDcc(new FileStream(dccName, FileMode.Open));
                }
                else
                {
                    dccName = dccName.Substring(0, dccName.Length - 1) + "6";
                    //ReadDc6(new FileStream(dccName, FileMode.Open));
                }

                if (i == 0)
                    dirBounds = (Rectangle[])LayerDirBounds.Clone();
                else
                    for (int d = 0; d < DirectionCount; d++)
                    {
                        dirBounds[d] = Rectangle.Union(dirBounds[d], LayerDirBounds[d]);
                    }
            }
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

            // skip flags of each frames
            reader.ReadBytes(FramesCount);

            // priority layer
            priority = reader.ReadBytes(DirectionCount * FramesCount * LayersCount);

            // default animation speed
            //cof->spd_mul = 1;
            //cof->spd_div = 256;

            //// default x and y offsets
            //cof->xoffset = cof->yoffset = 0;

            //// speed info : try in animdata.d2
            //sprintf(animdata_name, "%s%s%s", tok, mod, clas);
            //if (animdata_get_cof_info(animdata_name, &animdata_fpd, &animdata_speed) == 0)
            //{
            //    // found
            //    //      cof->fpd     = animdata_fpd;
            //    cof->spd_mul = animdata_speed; // can be override by objects.txt values
            //    cof->spd_div = 256;
            //}

            //// objects.txt ID of that obj
            //sptr = txt->data +
            //       (obj_line * txt->line_size) +
            //       txt->col[glb_ds1edit.col_obj_id].offset;
            //lptr = (long*)sptr;
            //id = *lptr;
            //printf("object %s ID = %li\n", name, id);


            //// which mode is this obj ?
            //if (stricmp(mod, "NU") == 0)
            //    mode = 0;
            //else if (stricmp(mod, "OP") == 0)
            //    mode = 1;
            //else if (stricmp(mod, "ON") == 0)
            //    mode = 2;
            //else if (stricmp(mod, "S1") == 0)
            //    mode = 3;
            //else if (stricmp(mod, "S2") == 0)
            //    mode = 4;
            //else if (stricmp(mod, "S3") == 0)
            //    mode = 5;
            //else if (stricmp(mod, "S4") == 0)
            //    mode = 6;
            //else if (stricmp(mod, "S5") == 0)
            //    mode = 7;
            //else
            //{
            //    // invalid object's mode, or simply not an object COF (like a monster COF)
            //    // end
            //    free(buff);
            //    if (pal_buff)
            //        free(pal_buff);
            //    return cof;
            //}

            //// search line in objects.txt for this ID
            //if (id)
            //{
            //    done = FALSE;
            //    i = 0;
            //    line = 0;
            //    glb_ds1edit.obj_desc[obj_line].objects_line = -1;
            //    while (!done)
            //    {
            //        sptr = txt2->data +
            //               (i * txt2->line_size) +
            //               txt2->col[glb_ds1edit.col_objects_id].offset;
            //        lptr = (long*)sptr;
            //        if ((*lptr) == id)
            //        {
            //            done = TRUE;
            //            line = i;
            //        }
            //        else
            //        {
            //            i++;
            //            if (i >= txt2->line_num)
            //            {
            //                // end
            //                free(buff);
            //                if (pal_buff)
            //                    free(pal_buff);
            //                return cof;
            //            }
            //        }
            //    }
            //    glb_ds1edit.obj_desc[obj_line].objects_line = line;

            //    // speed multiplicator
            //    sptr =
            //       txt2->data +
            //       (line * txt2->line_size) +
            //       txt2->col[glb_ds1edit.col_frame_delta[mode]].offset;
            //    lptr = (long*)sptr;
            //    cof->spd_mul = (*lptr) == 0 ? 256 : (*lptr);

            //    // speed divisor
            //    cof->spd_div = 256;

            //    // xoffset & yoffset
            //    if (txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Xoffset")].size)
            //    {
            //        sptr = txt2->data + (line * txt2->line_size) +
            //               txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Xoffset")].offset;
            //        lptr = (long*)sptr;
            //        cof->xoffset = *lptr;
            //    }
            //    if (txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Yoffset")].size)
            //    {
            //        sptr = txt2->data + (line * txt2->line_size) +
            //               txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Yoffset")].offset;
            //        lptr = (long*)sptr;
            //        cof->yoffset = *lptr;
            //    }

            //    // orderflag
            //    if (txt2->col[glb_ds1edit.col_orderflag[mode]].size)
            //    {
            //        sptr =
            //           txt2->data +
            //           (line * txt2->line_size) +
            //           txt2->col[glb_ds1edit.col_orderflag[mode]].offset;
            //        lptr = (long*)sptr;
            //        cof->orderflag = *lptr;

            //        // if 0, check NU
            //        // because Mephisto bridge only have a 1 in the NU mode
            //        if (*lptr == 0)
            //        {
            //            if (txt2->col[glb_ds1edit.col_orderflag[0]].size)
            //            {
            //                sptr =
            //                   txt2->data +
            //                   (line * txt2->line_size) +
            //                   txt2->col[glb_ds1edit.col_orderflag[0]].offset;
            //                lptr = (long*)sptr;
            //                cof->orderflag = *lptr;
            //            }
            //        }

            //        printf("object %s orderflag = %li\n", name, cof->orderflag);
            //    }
            //}
        
        }
    }
}
