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
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Diablo2RProject
{
    public partial class DiabloForm: Form
    {
        private DS1 loadedDS1;
        private DT1 loadedDT1;
        private Tile tile;
        public DiabloForm()
        {
            InitializeComponent();
            
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            ImportStructure();
        }

        //private void ImportStructure()
        //{
        //    var openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "DS1 Files (*.ds1)|*.ds1|DT1 Files (*.dt1)|*.dt1|All Files (*.*)|*.*";

        //    if (openFileDialog.ShowDialog() == DialogResult.OK)
        //    {
        //        byte[] fileData = File.ReadAllBytes(openFileDialog.FileName);
        //        DS1 ds1 = DS1.FromBytes(fileData);
        //        var stream = new MemoryStream(fileData);
        //        var reader = new BinaryReader(stream);

        //        var layerStreams = ds1.SetUpStreamLayerTypes().ToArray();
        //        ds1.LoadLayerStreams(reader, layerStreams);
        //        LoadStructure(ds1);
        //        //ImportTileGraphics();
        //    }
        //}

        private void ImportStructure()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DS1 Files (*.ds1)|*.ds1";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string ds1Path = openFileDialog.FileName;
                byte[] fileData = File.ReadAllBytes(ds1Path);
                string folderPath = Path.GetDirectoryName(ds1Path);

                DS1 ds1 = DS1.FromBytes(fileData);
                var stream = new MemoryStream(fileData);
                var reader = new BinaryReader(stream);

                var layersStreams = ds1.SetUpStreamLayerTypes().ToArray();
                ds1.LoadLayerStreams(reader, layersStreams);

                loadedDT1s.Clear();
                var dt1Paths = Directory.GetFiles(folderPath, "*.dt1");
                foreach (string dt1Path in dt1Paths)
                {
                    byte[] dt1Data = File.ReadAllBytes(dt1Path);
                    var dt1 = DT1.FromBytes(dt1Data, tile);
                    var decoder = new GfxDecode();
                    decoder.DecodeTileGraphics(dt1);
                    loadedDT1s.Add(dt1);
                }
                ImportTileGraphics(folderPath);
                LoadStructure(ds1);
            }
        }

        List<DT1> loadedDT1s = new List<DT1>();
        private void ImportTileGraphics(string folderPath)
        {
            var ds1Files = Directory.GetFiles(folderPath);
            loadedDT1s.Clear();

            foreach (string file in Directory.GetFiles(folderPath, "*.dt1"))
            {
                byte[] fileData = File.ReadAllBytes(file);
                var dt1 = DT1.FromBytes(fileData, tile);
                var decoder = new GfxDecode();
                decoder.DecodeTileGraphics(dt1);
                loadedDT1s.Add(dt1);
            }
        }

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
                        if (ds1.Tiles[y][x].Walls == null || ds1.Tiles[y][x].Walls.Count == 0)
                            continue;

                        var wall = ds1.Tiles[y][x].Walls[0];
                        var tile = FindMatchingTile(wall);

                        if (tile == null || tile.Block.Count == 0 || tile.Block[0].Image == null)
                            continue;
                        //TileType tileType = new TileType();

                        //if (ds1.Tiles[y][x].Walls != null && ds1.Tiles[y][x].Walls.Count > 0)
                        //{
                        //    tileType = ds1.Tiles[y][x].Walls[0].Type;
                        //}

                        //Color color;

                        //if (tileType.IsUpperWall())
                        //    color = Color.Teal;
                        //else if (tileType.IsLowerWall())
                        //    color = Color.Gray;
                        //else if (tileType.IsSpecial())
                        //    color = Color.Blue;
                        //else
                        //    continue;

                        //var wall = ds1.Tiles[y][x].Walls.FirstOrDefault();
                        //if (wall == null)
                        //    continue;

                        //var tile = FindMatchingTile(wall);
                        //if (tile == null)
                        //    continue;

                        //var block = tile.Block.FirstOrDefault();
                        //if (block == null || block.Image == null)
                        //    continue;

                        int screenX = (x - y) * (TileSizeX / 2) + offsetX;
                        int screenY = (x + y) * (TileSizeY / 2) + offsetY;

                        graphics.DrawImage(tile.Block[0].Image, screenX, screenY - tile.Block[0].Height);

                        //Point[] isometric = new Point[]
                        //{
                        //    new Point(screenX, screenY + TileSizeY / 2),
                        //    new Point(screenX + TileSizeX / 2, screenY),
                        //    new Point(screenX + TileSizeX, screenY + TileSizeY / 2),
                        //    new Point(screenX + TileSizeX / 2, screenY + TileSizeY)
                        //};

                        //using (Brush b = new SolidBrush(color))
                        //{
                        //    graphics.FillPolygon(b, isometric);
                        //}

                        //graphics.DrawPolygon(Pens.Black, isometric);
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

        private Tile FindMatchingTile(WallRecord wall)
        {
            foreach (var dt1 in loadedDT1s)
            {
                foreach (var tile in dt1.Tiles)
                {
                    if (tile.Style == wall.Style && tile.Sequence == wall.Sequence)
                        return tile;
                }
            }
            return null;
        }

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
