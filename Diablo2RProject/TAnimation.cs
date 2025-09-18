using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class TAnimation
    {
        public List<TFrame[][]> Sequences = new List<TFrame[][]>();
        public List<TAnimation> Source;
        public int Index;
        public string Name;
    }
}
