using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public enum TileType
    {
        TileLeftWall,
        TileRightWall,
        TileRightPartOfNorthCornerWall,
        TileLeftPartOfNorthCornerWall,
        TileLeftEndWall,
        TileRightEndWall,
        TileSouthCornerWall,
        TileLeftWallWithDoor,
        TileRightWallWithDoor,
        TileSpecialTile1,
        TileSpecialTile2,
        TilePillarsColumnsAndStandaloneObjects,
        TileShadow,
        TileTree,
        TileRoof,
        TileLowerWallsEquivalentToLeftWall,
        TileLowerWallsEquivalentToRightWall,
        TileLowerWallsEquivalentToRightLeftNorthCornerWall,
        TileLowerWallsEquivalentToSouthCornerwall
    }

    public static class TileTypeExtensions
    {
        public static bool IsLowerWall(this TileType tile)
        {
            return tile == TileType.TileLowerWallsEquivalentToLeftWall ||
                   tile == TileType.TileLowerWallsEquivalentToRightWall ||
                   tile == TileType.TileLowerWallsEquivalentToRightLeftNorthCornerWall ||
                   tile == TileType.TileLowerWallsEquivalentToSouthCornerwall;
        }

        public static bool IsUpperWall(this TileType tile)
        {
            return tile == TileType.TileLeftWall ||
                   tile == TileType.TileRightWall ||
                   tile == TileType.TileRightPartOfNorthCornerWall ||
                   tile == TileType.TileLeftPartOfNorthCornerWall ||
                   tile == TileType.TileLeftEndWall ||
                   tile == TileType.TileRightEndWall ||
                   tile == TileType.TileSouthCornerWall ||
                   tile == TileType.TileLeftWallWithDoor ||
                   tile == TileType.TileRightWallWithDoor ||
                   tile == TileType.TilePillarsColumnsAndStandaloneObjects ||
                   tile == TileType.TileTree;
        }

        public static bool IsSpecial(this TileType tile)
        {
            return tile == TileType.TileSpecialTile1 ||
                   tile == TileType.TileSpecialTile2;
        }
    }
}
