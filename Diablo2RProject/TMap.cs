using Common;
using Diablo2RProject;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class TMap
    {
        public string MapName;
        public static string GamePath;
        public List<List<string>> Dialogs;
        public List<List<string>> DialogTree;
        public virtual Vector2 TransformGrid(float x, float y) { return new Vector2(x, y); }
        public virtual Vector2 UnTransformGrid(float x, float y) { return new Vector2(x, y); }
        public virtual void RebuildMapView() { }
        TGame game;
        public TGame Game
        {
            get { return game; }
            set { game = value; game.Map = this; }
        }
        public int Width = 1;
        public int Height = 1;
        //public Size MapSize;
        //public virtual Bitmap Image { get; set; }
        public virtual void ReadMap(string filename) { }

    }
}
