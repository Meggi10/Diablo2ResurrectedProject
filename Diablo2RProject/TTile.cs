using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class TTile : TPiece
    {
        public virtual void ReadImage(BinaryReader reader) { }
    }
}
