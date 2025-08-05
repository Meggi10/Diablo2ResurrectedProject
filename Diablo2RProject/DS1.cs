using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class DS1
    {
        const int maxActNumber = 5;

        //public List<string> Files { get; set; } = new List<string>();
        public List<Tile> TileSet = new List<Tile>();
        public List<Object> Objects { get; set; } = new List<Object>();
        public List<List<TileRecord>> Tiles { get; set; } = new List<List<TileRecord>>();
        public List<SubstitutionGroup> SubstitutionGroups { get; set; } = new List<SubstitutionGroup>();
        public Version Version { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ActNo { get; set; }
        public int NumberOfWalls { get; set; }
        public int NumberOfFloors { get; set; }
        public int SubtitutionType { get; set; }
        public int NumberOfShadowLayers { get; set; }
        public int NumberOfSubstitutionLayers { get; set; }
        public int SubstitutionGroupNumber { get; set; }

        public List<LayerStreamType> SetUpStreamLayerTypes()
        {
            var layers = new List<LayerStreamType>();
            for (int i = 0; i < NumberOfWalls; i++)
                layers.Add(LayerStreamType.LayerWall1 + i);

            for (int i = 0; i < NumberOfFloors; i++)
                layers.Add(LayerStreamType.LayerFloor1 + i);

            if (NumberOfShadowLayers > 0)
                layers.Add(LayerStreamType.LayerShadow);

            if (NumberOfSubstitutionLayers > 0)
                layers.Add(LayerStreamType.LayerSubstitute);

            return layers;
        }

        public void LoadLayerStreams(BinaryReader reader, LayerStreamType[] layerStreams)
        {
            var dirLookup = new int[]{
                0x00, 0x01, 0x02, 0x01, 0x02, 0x03, 0x03, 0x05, 0x05, 0x06,
                0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
                0x0F, 0x10, 0x11, 0x12, 0x14,};

            for (int i = 0; i < layerStreams.Length; i++)
            {
                var layerStreamType = layerStreams[i];


                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var bits = reader.ReadInt32();



                        switch (layerStreamType)
                        {
                            case LayerStreamType.LayerWall1:
                            case LayerStreamType.LayerWall3:
                            case LayerStreamType.LayerWall4:
                                {
                                    var wallIndex = (int)layerStreamType - (int)LayerStreamType.LayerWall1;
                                    var tile = Tiles[y][x].Walls[wallIndex];

                                    tile.Property1 = (byte)(bits & 0x000000FF);

                                    tile.Sequence = (byte)((bits & 0x00003F00) >> 8);

                                    tile.Unknown1 = (byte)((bits & 0x000FC000) >> 14);

                                    tile.Style = (byte)((bits & 0x03F00000) >> 20);

                                    tile.Unknown2 = (byte)((bits & 0x7C000000) >> 26);

                                    tile.Hidden = (byte)((bits & 0x80000000) >> 31) > 0;
                                    tile.Tile = TileSet[tile.Sequence];
                                    break;
                                }

                            case LayerStreamType.LayerOrientation1:
                            case LayerStreamType.LayerOrientation2:
                            case LayerStreamType.LayerOrientation3:
                            case LayerStreamType.LayerOrientation4:
                                {

                                    var wallIndex = (int)(layerStreamType) - (int)LayerStreamType.LayerOrientation1;

                                    var c = bits & 0x000000FF;


                                    if ((int)Version < 7)
                                    { 
                                        if (c < dirLookup.Length)
                                        {
                                            c = dirLookup[c];
                                        }
                                    }
                                    Tiles[y][x].Walls[wallIndex].Type = (TileType)c;


                                    Tiles[y][x].Walls[wallIndex].Zero = (byte)((bits & 0xFFFFFF00) >> 8);

                                    break;
                                }

                            case LayerStreamType.LayerFloor1:
                            case LayerStreamType.LayerFloor2:
                                {
                                    var floorIndex = (int)layerStreamType - (int)LayerStreamType.LayerFloor1;
                                    var tile = Tiles[y][x].FloorsShadow[floorIndex];

                                    tile.property1 = (byte)(bits & 0x000000FF);

                                    tile.sequence = (byte)((bits & 0x00003F00) >> 8);

                                    tile.unknown1 = (byte)((bits & 0x000FC000) >> 14);

                                    tile.style = (byte)((bits & 0x03F00000) >> 20);

                                    tile.unknown2 = (byte)((bits & 0x7C000000) >> 26);

                                    tile.hidden = (byte)((bits & 0x80000000) >> 31) > 0;
                                    tile.Tile = TileSet[tile.sequence];
                                    break;
                                }

                            case LayerStreamType.LayerShadow:
                                {
                                    Tiles[y][x].Shadows[0].property1 = (byte)(bits & 0x000000FF);

                                    Tiles[y][x].Shadows[0].sequence = (byte)((bits & 0x00003F00) >> 8);

                                    Tiles[y][x].Shadows[0].unknown1 = (byte)((bits & 0x000FC000) >> 14);

                                    Tiles[y][x].Shadows[0].style = (byte)((bits & 0x03F00000) >> 20);

                                    Tiles[y][x].Shadows[0].unknown2 = (byte)((bits & 0x7C000000) >> 26);

                                    Tiles[y][x].Shadows[0].hidden = (byte)((bits & 0x80000000) >> 31) > 0;
                                    break;
                                }

                            case LayerStreamType.LayerSubstitute:
                                {
                                    Tiles[y][x].Substitutions[0].Unknown = bits;
                                    break;
                                }
                        }
                    }
                }
            }
        }

        public static string ReadZString(BinaryReader reader)
        {
            var result = "";
            var ch = reader.ReadChar();

            while (ch != 0)
            {
                result += ch;
                ch = reader.ReadChar();
            }
            return result;
        }

        public static DS1 LoadFromFile(string fileName)
        {
            var path = Path.GetDirectoryName(fileName) + "/";
            var stream = new FileStream(fileName, FileMode.Open);
            var reader = new BinaryReader(stream);

            try
            {
                var ds1 = new DS1
                {
                    ActNo = 1,
                    NumberOfFloors = 0,
                    NumberOfWalls = 0,
                    NumberOfShadowLayers = 1,
                    NumberOfSubstitutionLayers = 0
                };

                int versionValue = reader.ReadInt32();
                ds1.Version = (Version)versionValue;

                ds1.Width = reader.ReadInt32();
                ds1.Height = reader.ReadInt32();

                ds1.Width++;
                ds1.Height++;

                if (ds1.Version.EncodeAct())
                {
                    ds1.ActNo = reader.ReadInt32();
                }

                if (ds1.Version.EncodeSubstitutionLayers())
                {
                    ds1.SubtitutionType = reader.ReadInt32();

                    if (ds1.SubtitutionType == 1 || ds1.SubtitutionType == 2)
                    {
                        ds1.NumberOfSubstitutionLayers = 1;
                    }
                }

                if (ds1.Version.EncodeFiles())
                {
                  
                    int numberOfFiles = reader.ReadInt32();
                    //ds1.Files = new List<string>(numberOfFiles);

                    for (int i = 0; i < numberOfFiles; i++)
                    {
                        //List<byte> bytes = new List<byte>();
                        //byte b;

                        //while ((b = reader.ReadByte()) != 0)
                        //{
                        //    bytes.Add(b);
                        //}
                        //string fileName = Encoding.ASCII.GetString(bytes.ToArray());
                        var tileFile = ReadZString(reader);
                        tileFile = path + Path.GetFileNameWithoutExtension(tileFile) + ".dt1";
                        //ds1.Files.Add(fileName);

                        var dt1 = DT1.FromBytes(File.ReadAllBytes(tileFile), null);
                        ds1.TileSet.AddRange(dt1.Tiles);
                    }
                }

                if (ds1.Version.HasUnknownBytes1())
                {
                    const int unknownBytesLength = 8;
                    reader.ReadBytes(unknownBytesLength);
                }

                if (ds1.Version.EncodeFloorLayers())
                {
                    ds1.NumberOfFloors = reader.ReadInt32();

                    if (ds1.Version.EncodeWallLayers())
                    {
                        ds1.NumberOfFloors = reader.ReadInt32();
                        ds1.NumberOfFloors = 1;
                    }
                }

                ds1.Tiles = new List<List<TileRecord>>();

                for (int y = 0; y < ds1.Height; y++)
                {
                    var row = new List<TileRecord>();

                    for (int x = 0; x < ds1.Width; x++)
                    {
                        var tile = new TileRecord
                        {
                            Walls = new List<WallRecord>(),
                            FloorsShadow = new List<FloorShadowRecord>(),
                            Shadows = new List<FloorShadowRecord>(),
                            Substitutions = new List<SubstitutionRecord>()
                        };

                        for (int i = 0; i < ds1.NumberOfWalls; i++)
                            tile.Walls.Add(new WallRecord());

                        for (int i = 0; i < ds1.NumberOfFloors; i++)
                            tile.FloorsShadow.Add(new FloorShadowRecord());

                        for (int i = 0; i < ds1.NumberOfShadowLayers; i++)
                            tile.Shadows.Add(new FloorShadowRecord());

                        for (int i = 0; i < ds1.NumberOfSubstitutionLayers; i++)
                            tile.Substitutions.Add(new SubstitutionRecord());

                        row.Add(tile);
                    }
                    ds1.Tiles.Add(row);

                }
                var layerStreams = ds1.SetUpStreamLayerTypes().ToArray();
                ds1.LoadLayerStreams(reader, layerStreams);

                return ds1;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Błąd podczas wczytywania pliku ds1", ex);
            }

            finally
            {
                reader.Close();
                stream.Close();
            }
        }
    }
}
