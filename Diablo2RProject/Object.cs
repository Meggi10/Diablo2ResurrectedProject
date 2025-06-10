using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class Object: Path
    {
        public int Type;
        public int Id;
        public int X;
        public int Y;
        public int Flag;
        public List<Path>Paths { get; set; } = new List<Path>();
    }
}
