using Common;
using Diablo2RProject.Diablo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows.Forms;

namespace Diablo2RProject
{
    class TDiabloMap : TMap
    {
        public static int[] Palette;
        static TDiabloMap()
        {
            TCell.Width = 32;
            TCell.Height = 16;
            Palette = new int[256];
            for (int i = 0; i < 256; i++)
                Palette[i] = Color.FromArgb(i, i, i).ToArgb();
        }

        Vector2 GridOffset;
        int WorldHeight;
        int WorldWidth;
        public void ReadTileSet(string filename, string ext)
        {
            filename = filename.Substring(0, filename.Length - 4) + ext;
            var fStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(fStream);
            int verMajor = reader.ReadInt32();
            int verMinor = reader.ReadInt32();
            if (verMajor == 7 && verMinor == 6)
            {
                reader.ReadBytes(260);
                int blockTilesCount = reader.ReadInt32();
                int filePos = reader.ReadInt32();
                var blockTiles = new List<DiabloBlockTile>();
                for (int i = 0; i < blockTilesCount; i++)
                {
                    var blockTile = new DiabloBlockTile();
                    blockTile.ReadHeader(reader);
                    blockTiles.Add(blockTile);
                    Game.Walls.Add(blockTile);
                }
                foreach (var blockTile in blockTiles)
                    blockTile.ReadTiles(reader);
            }
            fStream.Close();
        }

        public override Vector2 TransformGrid(float x, float y)
        {
            var v = new Vector2(x - y, y + x) - GridOffset;
            v.Y = (v.Y - ((int)v.X & 1)) / 2;
            return v;
        }
        public override Vector2 UnTransformGrid(float x_, float y_)
        {
            y_ = 2 * y_ + ((int)x_ & 1);
            x_ += GridOffset.X;
            y_ += GridOffset.Y;
            return new Vector2(x_ + y_, y_ - x_) / 2;
        }

        TCell[,] UntransformFromHexMapping()
        {
            var cells = new TCell[Height, Width];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    //var pos = UnTransformGrid(x, 2 * y + (x & 1));
                    var pos = UnTransformGrid(x, y);
                    if (pos.Y >= 0 && pos.Y < WorldHeight && pos.X >= 0 && pos.X < WorldWidth)
                    {
                        var cell = Game.Cells[(int)pos.Y, (int)pos.X];
                        cell.X = x;
                        cell.Y = y;
                        cells[y, x] = cell;
                    }
                    //else
                    //    cells[y, x] = Game.Cells[0, 0];
                }
            return cells;
        }

        TCell[,] TransformToHexMapping()
        {
            var cells = new TCell[WorldHeight, WorldWidth];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    //var pos = UnTransformGrid(x, 2 * y + (x & 1));
                    var pos = UnTransformGrid(x, y);
                    if (pos.Y >= 0 && pos.Y < WorldHeight && pos.X >= 0 && pos.X < WorldWidth)
                    {
                        var cell = Game.Cells[y, x];
                        //cell.X = (int)pos.X;
                        //cell.Y = (int)pos.Y;
                        cells[(int)pos.Y, (int)pos.X] = cell;
                    }
                }
            return cells;
        }

        public void MapTileSet()
        {
            var blockSize = 10 * TCell.Width;
            int mapSize = (int)Math.Ceiling(Math.Sqrt(Game.Walls.Count * 5)) + 1;
            Height = 5 * mapSize;
            Width = 2 * Height;
            Game.Cells = new TCell[Height, Width];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var cell = new TCell();
                    cell.Game = Game;
                    cell.X = x;
                    cell.Y = y;
                    Game.Cells[y, x] = cell;
                }
            for (int idx = 0; idx < Game.Walls.Count; idx++)
            {
                var blockTile = Game.Walls[idx];
                blockTile.X = (idx % mapSize) * blockSize;
                blockTile.Y = idx / mapSize * blockSize;
                blockTile.Bounds = new Rectangle(blockTile.X, blockTile.Y - blockSize, blockSize, blockSize);
            }
        }

        public void MapAnimation(string filename)
        {
            ReadPalette(filename);
            var animation = new DiabloAnimation();
            //animation.BasePath = Path.GetDirectoryName(Path.GetDirectoryName(BasePath));
            var dirName = Path.GetDirectoryName(Path.GetDirectoryName(filename));
            foreach (var dir in Directory.GetDirectories(dirName))
            {
                var layer = Path.GetFileName(dir).ToUpper();
                var layerIdx = Array.IndexOf(DiabloAnimation.LayerType, layer);
                if (layerIdx < 0) continue;
                var layerFiles = Directory.GetFiles(dir);
                var layerFile = Path.GetFileName(layerFiles[TGame.Random.Next(layerFiles.Length)]);
                animation.Armor[layerIdx] = layerFile.Substring(4, 3);
            }
            filename = Path.GetFileName(filename);
            animation.Name = filename;
            animation.Token = filename.Substring(0, 2);
            animation.Mode = filename.Substring(2, 2);
            animation.ClassType = filename.Substring(4, 3);
            animation.Read();
            Game.Animations.Add(animation);
            var posX = 0;
            var posY = 0;
            for (var j = 0; j < animation.Sequences.Count; j++)
            {
                var sequence = animation.Sequences[j];
                posX = 0;
                for (var k = 0; k < sequence.Length; k++)
                {
                    var sprite = new TSprite();
                    sprite.Animation = animation;
                    sprite.Sequence = j;
                    sprite.ViewAngle = k;
                    var width = sprite.ActFrame.Bounds.Width;
                    var height = sprite.ActFrame.Bounds.Height;
                    sprite.X = posX;
                    sprite.Y = posY;
                    posX += 2 * width;
                    sprite.Bounds = new Rectangle(sprite.X, sprite.Y, width, height);
                    Game.Sprites.Add(sprite);
                }
                posY += 2 * sequence[0][0].Bounds.Height;
            }
            Width = 2 * posX / TCell.Width + 4;
            Height = 2 * posY / TCell.Height + 2;
        }

        public enum TVersion
        {
            HasFiles = 3,
            HasWalls = 4,
            HasAct = 8,
            HasUnknownBytes1Low = 9,
            HasSubtitutionLayers = 10,
            HasSubtitutionGroups = 12,
            HasUnknownBytes1High = 13,
            HasNpcs = 14,
            HasNpcActions = 15,
            HasFloors = 16,
            HasUnknownBytes2 = 18,
        }

        public static string ReadZString(BinaryReader reader)
        {
            var result = "";
            var chr = reader.ReadChar();
            while (chr != '\0')
            {
                result += chr;
                chr = reader.ReadChar();
            }
            return result;
        }

        public void ReadPalette(string filename)
        {
            var dirName = filename;
            while (dirName != null && Path.GetFileName(dirName) != "D2")
                dirName = Path.GetDirectoryName(dirName);
            if (dirName == null) dirName = filename;
            GamePath = Path.GetDirectoryName(dirName);
            var actName = Path.GetDirectoryName(Path.GetDirectoryName(filename));
            actName = Path.GetFileName(actName).ToLower();
            if (!actName.StartsWith("act"))
                actName = "Units";
            var palPath = GamePath + "\\D2\\Data\\Global\\Palette\\" + actName + "\\pal.dat";
            var fStream = new FileStream(palPath, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(fStream);
            Palette = new int[256];
            for (int i = 0; i < Palette.Length; i++)
            {
                var b = reader.ReadByte();
                var g = reader.ReadByte();
                var r = reader.ReadByte();
                var a = r + g + b > 0 ? 255 : 0;
                //var a = reader.ReadByte();
                //if (r + g + b > 0) a = 255;
                Palette[i] = Color.FromArgb(a, r, g, b).ToArgb();
            }
            fStream.Close();
        }

        int ActNo;
        int SubstitutionType;
        TVersion Version;
        public override void ReadMap(string filename)
        {
            Cursor.Current = Cursors.WaitCursor;
            MapName = Path.GetFileNameWithoutExtension(filename);
            ReadPalette(filename);
            Game.Walls.Clear();
            var fStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(fStream);
            Version = (TVersion)reader.ReadInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            Width++;
            Height++;
            WorldWidth = Width * 5;
            WorldHeight = Height * 5;
            GridOffset.X = -WorldHeight;
            Game.Cells = new TCell[WorldHeight, WorldWidth];
            for (int y = 0; y < WorldHeight; y++)
                for (int x = 0; x < WorldWidth; x++)
                {
                    var cell = new TCell();
                    cell.Game = Game;
                    cell.X = x;
                    cell.Y = y;
                    Game.Cells[y, x] = cell;
                }
            ReadCells(reader);
            ReadObjects(reader);
            //LoadSubstitutions(reader);
            //LoadNPCs(reader);
            reader.Close();
            Width = WorldWidth + WorldHeight;
            Height = Width / 2;
            Game.Cells = UntransformFromHexMapping();
            Game.Board.ScrollPos = new PointF(0.5f, 0.5f);
            Cursor.Current = Cursors.Default;
        }

        void ReadWalls(BinaryReader reader, List<DiabloBlockTile>[,,] blockTiles)
        {
            var typeLookup = new int[]{
                0x00, 0x01, 0x02, 0x01, 0x02, 0x03, 0x03, 0x05, 0x05, 0x06,
                0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
                0x0F, 0x10, 0x11, 0x12, 0x14,};
            var walls = new List<DiabloBlockTile>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var tileInfo = reader.ReadInt32();
                    if (tileInfo == 0) continue;
                    var blockTile = new DiabloBlockTile();
                    blockTile.Seq = tileInfo >> 8 & 0x3F;
                    blockTile.Style = tileInfo >> 20 & 0x3F;
                    blockTile.Hidden = tileInfo < 0;
                    //blockTile.Rarity = tileInfo & 0x3F;
                    //blockTile.Property1 = tileInfo & 0xFF;
                    //blockTile.Unk1 = tileInfo >> 14 & 0x3F;
                    //blockTile.Unk2 = tileInfo >> 26 & 0x1F;
                    walls.Add(blockTile);
                }
            if (Version < TVersion.HasWalls)
                ReadFloors(reader, blockTiles);
            var wallIdx = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var wallType = reader.ReadInt32();
                    if (wallType == 0) continue;
                    var wall = walls[wallIdx]; wallIdx++;
                    if (ActNo == 0 && wallType < typeLookup.Length)
                        wallType = typeLookup[wallType];
                    wall.Type = (TWallType)wallType;
                    var selection = blockTiles[wallType, wall.Style, wall.Seq];
                    if (selection != null)
                    {
                        wall.Rarity = TGame.Random.Next(selection.Count);
                        var wallTile = selection[wall.Rarity];
                        wall.Tiles = wallTile.Tiles;
                        var pos = TransformGrid(5 * x, 5 * y);
                        var cell = new TCell();
                        cell.X = (int)pos.X;
                        cell.Y = (int)pos.Y;
                        pos = cell.Position;
                        wall.X = (int)(pos.X - 2 * TCell.Width);
                        wall.Y = (int)pos.Y;
                        if (wall.Type == TWallType.Roof)
                        {
                            wall.Y -= wallTile.RoofHeight;
                            Game.RoofTiles.Add(wall);
                        }
                        else
                        {
                            if (wall.Type < TWallType.Roof)
                                wall.Y += 5 * TCell.Height;
                            Game.Walls.Add(wall);
                        }
                        wall.Bounds = new Rectangle(wall.X, wall.Y + wallTile.Height, wallTile.Width, Math.Abs(wallTile.Height));
                        if (wall.Type == TWallType.LeftTopCorner_Top)
                        {
                            var extraTile = new DiabloBlockTile();
                            extraTile.Type = TWallType.LeftTopCorner_Left;
                            extraTile.Style = wall.Style;
                            extraTile.Seq = wall.Seq;
                            selection = blockTiles[(int)extraTile.Type, extraTile.Style, extraTile.Seq];
                            extraTile.Tiles = selection[wall.Rarity].Tiles;
                            extraTile.X = wall.X;
                            extraTile.Y = wall.Y;
                            extraTile.Bounds = wall.Bounds;
                            Game.Walls.Add(extraTile);
                        }
                    }
                }
        }

        void ReadFloors(BinaryReader reader, List<DiabloBlockTile>[,,] blockTiles)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var tileInfo = reader.ReadInt32();
                    if (tileInfo == 0) continue;
                    var floor = new DiabloBlockTile();
                    floor.Seq = tileInfo >> 8 & 0x3F;
                    floor.Style = tileInfo >> 20 & 0x3F;
                    floor.Hidden = tileInfo < 0;
                    var selection = blockTiles[(int)floor.Type, floor.Style, floor.Seq];
                    if (selection != null)
                    {
                        floor = selection[TGame.Random.Next(selection.Count)];
                        for (int u = 0; u < 5; u++)
                            for (int v = 0; v < 5; v++)
                            {
                                var cell = Game.Cells[5 * y + u, 5 * x + v];
                                var tileIdx = (4 - u) * 5 + 4 - v;
                                if (tileIdx < floor.Tiles.Count)
                                    cell.GroundTile = floor.Tiles[tileIdx];
                            }
                    }
                }
        }

        void ReadShadows(BinaryReader reader)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var tileInfo = reader.ReadInt32();
                    //if (tileInfo == 0) continue;
                }
        }

        void ReadSubstitutions(BinaryReader reader)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var tileInfo = reader.ReadInt32();
                    //if (tileInfo == 0) continue;
                }
        }

        void ReadCells(BinaryReader reader)
        {
            if (Version >= TVersion.HasAct)
                ActNo = 1 + reader.ReadInt32();
            if (Version >= TVersion.HasSubtitutionLayers)
                SubstitutionType = reader.ReadInt32();
            if (Version >= TVersion.HasFiles)
            {
                //Game.GroundTiles = new List<TTile>();
                int filesCount = reader.ReadInt32();
                for (int i = 0; i < filesCount; i++)
                {
                    var fileName = ReadZString(reader);
                    if (fileName.StartsWith("C:\\"))
                        fileName = fileName.Substring(3);
                    //string fileName = Encoding.ASCII.GetString(bytes.ToArray());
                    fileName = GamePath + fileName;
                    //Game.GroundTiles.AddRange(ReadTileSet(fileName, ".dt1"));
                    ReadTileSet(fileName, ".dt1");
                }
            }
            var blockTiles = new List<DiabloBlockTile>[64, 64, 64];
            foreach (var blockTile in Game.Walls)
            {
                var dbTile = (DiabloBlockTile)blockTile;
                if (blockTiles[(int)dbTile.Type, dbTile.Style, dbTile.Seq] == null)
                    blockTiles[(int)dbTile.Type, dbTile.Style, dbTile.Seq] = new List<DiabloBlockTile>();
                blockTiles[(int)dbTile.Type, dbTile.Style, dbTile.Seq].Add(dbTile);
            }
            Game.Walls.Clear();
            if (Version >= TVersion.HasUnknownBytes1Low && Version <= TVersion.HasUnknownBytes1High)
                reader.ReadBytes(8);
            if (Version < TVersion.HasWalls)
            {
                ReadWalls(reader, blockTiles);
                ReadSubstitutions(reader);
                ReadShadows(reader);
            }
            else
            {
                var wallsLayersCount = reader.ReadInt32();
                var floorsLayersCount = Version < TVersion.HasFloors ? 1 : reader.ReadInt32();
                for (int i = 0; i < wallsLayersCount; i++)
                    ReadWalls(reader, blockTiles);
                for (int i = 0; i < floorsLayersCount; i++)
                    ReadFloors(reader, blockTiles);
                ReadShadows(reader);
                if (SubstitutionType == 1 || SubstitutionType == 2)
                    ReadSubstitutions(reader);
            }
        }
        void ReadObjects(BinaryReader reader)
        {
            var objInfo = new TIniReader($"{GamePath}/D2/data/global/obj.txt", '\t')[""];
            var tokenIdx = objInfo[0].IndexOf("Token");
            var modeIdx = objInfo[0].IndexOf("Mode");
            var classIdx = objInfo[0].IndexOf("Class");
            var dirIdx = objInfo[0].IndexOf("Direction");
            var palIdx = objInfo[0].IndexOf("Index");
            var armorIdx = objInfo[0].IndexOf("HD");
            var anims = new DiabloAnimation[objInfo.Count];
            var objCount = reader.ReadInt32();
            for (int i = 0; i < objCount; i++)
            {
                var obj = new TElement();
                var type = reader.ReadInt32() - 1;
                obj.Id = reader.ReadInt32();
                var x = reader.ReadInt32();
                var y = reader.ReadInt32();
                var pos = TransformGrid(x - 4, y - 1);
                var cell = new TCell();
                cell.X = (int)pos.X;
                cell.Y = (int)pos.Y;
                pos = cell.Position;
                obj.X = (int)pos.X;
                obj.Y = (int)pos.Y;
                obj.EventId = reader.ReadInt32();
                obj.Bounds = new Rectangle(obj.X, obj.Y, TCell.Width, TCell.Height);
                var idx = (ActNo - 1) * 210 + type * 60 + obj.Id + 1;
                if (anims[idx] == null)
                {
                    var anim = new DiabloAnimation();
                    anim.Token = objInfo[idx][tokenIdx];
                    anim.Mode = objInfo[idx][modeIdx];
                    anim.ClassType = objInfo[idx][classIdx];
                    anim.BasePath = type == 0 ? "Monsters" : "Objects";
                    int.TryParse(objInfo[idx][palIdx], out anim.PaletteIdx);
                    for (var a = 0; a < 16; a++)
                        anim.Armor.Add(objInfo[idx][armorIdx + a]);
                    anim.Index = idx;
                    anim.Read();
                    obj.Animation = anim;
                    anims[idx] = anim;
                }
                else
                    obj.Animation = anims[idx];
                Game.Sprites.Add(obj);
            }
        }
    }

}
