using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Serialization;

namespace Diablo2RProject
{
    public partial class DiabloForm : Form
    {
        private DS1 loadedDS1;
        private DT1 loadedDT1;
        private Tile tile;
        private byte[] fileData;
        //private Dictionary<(int Type, int Style, int Sequence), Tile> tileLookup;
        public DiabloForm()
        {
            InitializeComponent();

        }
        public void Button1_Click(object sender, EventArgs e)
        {
            ImportStructure();
            
            //ImportMap();
        }



        //ImportStructure(filename);

        //public void ImportMap()
        //{
        //    var openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "DS1 Files (*.ds1)|*.ds1|DT1 Files (*.dt1)|*.dt1|All Files (*.*)|*.*";
        //    if (openFileDialog.ShowDialog() == DialogResult.OK)
        //    {
        //        DS1 ds1 = DS1.LoadFromFile(Name);
        //        LoadStructure(ds1);
        //    }
        //}

        public void ImportStructure()
        {
            //ImportMap(filename);
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DS1 Files (*.ds1)|*.ds1|DT1 Files (*.dt1)|*.dt1|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //string ex = Path.GetDirectoryName(openFileDialog.FileName).ToLower();
                //if (ex == ".ds1")
                //{
                    DS1 ds1 = DS1.LoadFromFile(openFileDialog.FileName);
                    LoadStructure(ds1);
                    //ImportMap();
                //}
               //else if (ex == ".dt1")
               //{
                    
                   //DT1 dt1 = DT1.FromBytes(fileData, tile);
               //}

                //byte[] fileData = File.ReadAllBytes(openFileDialog.FileName);
                //var stream = new MemoryStream(fileData);
                //var reader = new BinaryReader(stream);

                //var layerStreams = ds1.SetUpStreamLayerTypes().ToArray();
                //ds1.LoadLayerStreams(reader, layerStreams);

                //string folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                //tileLookup = new Dictionary<(int, int, int), Tile>();
                //foreach ( var dt1File in Directory.GetFiles(folderPath, "*.dt1"))
                //{
                //    var dt1 = DT1.FromBytes(File.ReadAllBytes(dt1File), null);
                //    TileGraphic(dt1);
                //}
                //}
            }
        }

        //private void TileGraphic(DT1 dt1)
        //{
        //    GfxDecode gfxdecode = new GfxDecode();

        //    gfxdecode.DecodeTileGraphics(dt1);

        //    foreach (var tile in dt1.Tiles)
        //    {
        //        var key = (tile.Type, tile.Style, tile.Sequence);
        //        if (!tileLookup.ContainsKey(key))
        //            tileLookup[key] = tile;
        //    }
        //}

        private void LoadStructure(DS1 ds1)
        {
            if (ds1 == null || ds1.Width == 0 || ds1.Height == 0)
                return;

            loadedDS1 = ds1;

            int TileSizeX = MapView.Width / (ds1.Width + ds1.Height);
            int TileSizeY = MapView.Width / 2;

            int bmpWidth = (ds1.Width + ds1.Height) * (TileSizeX / 2);
            int bmpHeight = (ds1.Width + ds1.Height) * (TileSizeY / 2);

            int offsetX = (bmpWidth - TileSizeX) / 2;
            int offsetY = (bmpHeight + TileSizeY) / TileSizeY;

            Bitmap bitmap = new Bitmap(bmpWidth, bmpHeight);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);

                for (int y = 0; y < ds1.Height; y++)
                {
                    for (int x = 0; x < ds1.Width; x++)
                    {
                        TileType tileType = default;

                        if (ds1.Tiles[y][x].FloorsShadow != null && ds1.Tiles[y][x].FloorsShadow.Count > 0)
                        {
                            var wall = ds1.Tiles[y][x].FloorsShadow[0];
                            //tileType = (TileType)wall.Type;

                            int screenX = (x - y) * (TileSizeX / 2) + offsetX;
                            int screenY = (x + y) * (TileSizeY / 2) + offsetY;

                            //if (tileLookup != null && tileLookup.TryGetValue(((int Type, int Style, int Sequence))(tileType, wall.Style, wall.Sequence), out var tile))
                            var tile = ds1.TileSet[wall.sequence];
                            if (tile != null)
                            {
                                var image = tile.Block.FirstOrDefault()?.Image;
                                if (image != null)
                                {
                                    graphics.DrawImage(image, screenX, screenY);
                                    continue;
                                }
                            }

                            //Color color;
                            //if (tileType.IsUpperWall())
                            //    color = Color.Teal;
                            //else if (tileType.IsLowerWall())
                            //    color = Color.Gray;
                            //else if (tileType.IsSpecial())
                            //    color = Color.Blue;
                            //else
                            //    continue;

                            Point[] isometric = new Point[]
                            {
                                new Point(screenX, screenY + TileSizeY / 2),
                                new Point(screenX + TileSizeX / 2, screenY),
                                new Point(screenX + TileSizeX, screenY + TileSizeY / 2),
                                new Point(screenX + TileSizeX / 2, screenY + TileSizeY)
                            };

                            //using (Brush b = new SolidBrush(color))
                            //{
                            //    graphics.FillPolygon(b, isometric);
                            //}

                            graphics.DrawPolygon(Pens.Red, isometric);
                        }
                    }
                }
            }

            if (bitmap.Width > MapView.Width || bitmap.Height > MapView.Height)
            {
                Bitmap scaled = new Bitmap(bitmap, MapView.Width, MapView.Height);
                MapView.Image = scaled;
            }
            else
            {
                MapView.Image = bitmap;
            }

            MapView.Invalidate();
        }

        //private void LoadStructure(DS1 ds1)
        //{
        //    if (ds1 == null || ds1.Width == 0 || ds1.Height == 0)
        //        return;

        //    loadedDS1 = ds1;

        //    int TileSizeX = MapView.Width / (ds1.Width + ds1.Height);
        //    int TileSizeY = MapView.Width / 2;

        //    int bmpWidth = (ds1.Width + ds1.Height) * (TileSizeX / 2);
        //    int bmpHeight = (ds1.Width + ds1.Height) * (TileSizeY / 2);

        //    int offsetX = (bmpWidth - TileSizeX) / 2;
        //    int offsetY = (bmpHeight + TileSizeY) / TileSizeY;

        //    Bitmap bitmap = new Bitmap(bmpWidth, bmpHeight);
        //    using (Graphics graphics = Graphics.FromImage(bitmap))
        //    {
        //        graphics.Clear(Color.Black);

        //        for (int y = 0; y < ds1.Height; y++)
        //        {
        //            for (int x = 0; x < ds1.Width; x++)
        //            {
        //                TileType tileType = new TileType();


        //                if (ds1.Tiles[y][x].Walls != null && ds1.Tiles[y][x].Walls.Count > 0)
        //                {
        //                    tileType = ds1.Tiles[y][x].Walls[0].Type;
        //                }

        //                int screenX = (x - y) * (TileSizeX / 2) + offsetX;
        //                int screenY = (x + y) * (TileSizeY / 2) + offsetY;


        //                if (tileLookup != null && tileLookup.TryGetValue((tileType.Type, tileType.Style, tileType.Sequence), out var tile))
        //                {
        //                    var image = tile.Block.FirstOrDefault()?.Image;
        //                    if (image != null)
        //                    {
        //                        graphics.DrawImage(image, screenX, screenY);
        //                        continue;
        //                    }
        //                }


        //                Color color;
        //                if (tileType.IsUpperWall())
        //                    color = Color.Teal;
        //                else if (tileType.IsLowerWall())
        //                    color = Color.Gray;
        //                else if (tileType.IsSpecial())
        //                    color = Color.Blue;
        //                else
        //                    continue;

        //                Point[] isometric = new Point[]
        //                {
        //                    new Point(screenX, screenY + TileSizeY / 2),
        //                    new Point(screenX + TileSizeX / 2, screenY),
        //                    new Point(screenX + TileSizeX, screenY + TileSizeY / 2),
        //                    new Point(screenX + TileSizeX / 2, screenY + TileSizeY)
        //                };

        //                using (Brush b = new SolidBrush(color))
        //                {
        //                    graphics.FillPolygon(b, isometric);
        //                }

        //                graphics.DrawPolygon(Pens.Black, isometric);
        //            }
        //        }
        //    }

        //    if (bitmap.Width > MapView.Width || bitmap.Height > MapView.Height)
        //    {
        //        Bitmap scaled = new Bitmap(bitmap, MapView.Width, MapView.Height);
        //        MapView.Image = scaled;
        //    }
        //    else
        //    {
        //        MapView.Image = bitmap;
        //    }

        //    MapView.Invalidate();
        //}


        //private void LoadStructure(DS1 ds1)
        //{
        //    if (ds1 == null || ds1.Width == 0 || ds1.Height == 0)
        //        return;

        //    loadedDS1 = ds1;

        //    int TileSizeX = MapView.Width / (ds1.Width + ds1.Height);
        //    int TileSizeY = MapView.Width / 2;

        //    int bmpWidth = (ds1.Width + ds1.Height) * (TileSizeX / 2);
        //    int bmpHeight = (ds1.Width + ds1.Height) * (TileSizeY / 2);

        //    int offsetX = (bmpWidth - TileSizeX) / 2;
        //    int offsetY = (bmpHeight + TileSizeY) / TileSizeY;

        //    Bitmap bitmap = new Bitmap(bmpWidth, bmpHeight);
        //    using (Graphics graphics = Graphics.FromImage(bitmap))
        //    {
        //        graphics.Clear(Color.Black);

        //        for (int y = 0; y < ds1.Height; y++)
        //        {
        //            for (int x = 0; x < ds1.Width; x++)
        //            {
        //                TileType tileType = new TileType();

        //                if (ds1.Tiles[y][x].Walls != null && ds1.Tiles[y][x].Walls.Count > 0)
        //                {
        //                    tileType = ds1.Tiles[y][x].Walls[0].Type;
        //                }

        //                Color color;

        //                if (tileType.IsUpperWall())
        //                    color = Color.Teal;
        //                else if (tileType.IsLowerWall())
        //                    color = Color.Gray;
        //                else if (tileType.IsSpecial())
        //                    color = Color.Blue;
        //                else
        //                    continue;

        //                int screenX = (x - y) * (TileSizeX / 2) + offsetX;
        //                int screenY = (x + y) * (TileSizeY / 2) + offsetY;

        //                Point[] isometric = new Point[]
        //                {
        //                    new Point(screenX, screenY + TileSizeY / 2),
        //                    new Point(screenX + TileSizeX / 2, screenY),
        //                    new Point(screenX + TileSizeX, screenY + TileSizeY / 2),
        //                    new Point(screenX + TileSizeX / 2, screenY + TileSizeY)
        //                };

        //                using (Brush b = new SolidBrush(color))
        //                {
        //                    graphics.FillPolygon(b, isometric);
        //                }

        //                graphics.DrawPolygon(Pens.Black, isometric);
        //            }
        //        }
        //    }

        //    if (bitmap.Width > MapView.Width || bitmap.Height > MapView.Height)
        //    {
        //        Bitmap scaled = new Bitmap(bitmap, MapView.Width, MapView.Height);
        //        MapView.Image = scaled;
        //    }
        //    else
        //    {
        //        MapView.Image = bitmap;
        //    }
        //    MapView.Invalidate();
        //}

        private void DiabloForm_Load(object sender, EventArgs e)
        {
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void MapView_Click(object sender, EventArgs e)
        {
            
        }
    }
}
