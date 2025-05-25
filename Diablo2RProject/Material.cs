using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public class Material
    {
        public enum MaterialFlags
        {
            Other = 0x0001,
            Water = 0x0002,
            WoodObject = 0x0004,
            InsideStone = 0x0008,
            OutsideStone = 0x0010,
            Dirt = 0x0020,
            Sand = 0x0040,
            Wood = 0x0080,
            Lava = 0x0100,
            Snow = 0x0400
        }

        public static MaterialFlags NewMaterialFlag(int data)
        {
            return (MaterialFlags)data;
        }
    }
}
