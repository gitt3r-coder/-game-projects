using System;

namespace Tanki
{
    public static class MapGenerator
    {
        public static MapCell[,] Generate(
            int width,
            int height,
            Random rnd,
            MapTheme theme,
            out int topHqX,
            out int topHqY,
            out int bottomHqX,
            out int bottomHqY)
        {
            var map = new MapCell[width, height];
            var reserved = new bool[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    map[x, y] = new MapCell(CellType.Empty);

            int leftLaneX = 6;
            int centerLaneX = (width / 2) - 1;
            int rightLaneX = width - 9;

            ReserveLane(map, reserved, leftLaneX, height);
            ReserveLane(map, reserved, centerLaneX, height);
            ReserveLane(map, reserved, rightLaneX, height);

            topHqX = centerLaneX + 1;
            topHqY = 1;
            bottomHqX = centerLaneX + 1;
            bottomHqY = height - 2;

            ReserveRect(map, reserved, topHqX - 2, topHqY - 1, 5, 4);
            ReserveRect(map, reserved, bottomHqX - 2, bottomHqY - 2, 5, 4);
            ReserveRect(map, reserved, 0, 3, width, 1);
            ReserveRect(map, reserved, 0, height - 4, width, 1);

            switch (theme)
            {
                case MapTheme.Wetlands:
                    BuildWetlands(map, reserved, rnd, width, height);
                    break;

                case MapTheme.FrozenFront:
                    BuildFrozenFront(map, reserved, rnd, width, height);
                    break;

                case MapTheme.Fortress:
                    BuildFortress(map, reserved, rnd, width, height);
                    break;

                default:
                    BuildBalanced(map, reserved, rnd, width, height);
                    break;
            }

            FillHQ(map, topHqX, topHqY, CellType.HeadquartersTop);
            FillHQ(map, bottomHqX, bottomHqY, CellType.HeadquartersBottom);
            return map;
        }

        private static void BuildBalanced(MapCell[,] map, bool[,] reserved, Random rnd, int width, int height)
        {
            ReserveRect(map, reserved, (width / 2) - 3, (height / 2) - 3, 7, 6);
            CreateCentralFortress(map, reserved, width, height);
            CreateWaterPockets(map, reserved, width, height);
            CreateMirroredBushes(map, reserved, width, height, 4);
            CreateIceRoutes(map, reserved, width, height, 1);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Brick, 24, 2, 3, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Concrete, 12, 2, 2, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Bush, 14, 2, 2, width, height);
        }

        private static void BuildWetlands(MapCell[,] map, bool[,] reserved, Random rnd, int width, int height)
        {
            ReserveRect(map, reserved, (width / 2) - 2, (height / 2) - 2, 5, 4);
            CreateWaterPockets(map, reserved, width, height);
            CreateWetlandChannels(map, reserved, width, height);
            CreateMirroredBushes(map, reserved, width, height, 6);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Brick, 28, 2, 3, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Bush, 24, 3, 2, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Water, 8, 2, 2, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Concrete, 8, 2, 2, width, height);
        }

        private static void BuildFrozenFront(MapCell[,] map, bool[,] reserved, Random rnd, int width, int height)
        {
            ReserveRect(map, reserved, (width / 2) - 2, (height / 2) - 3, 5, 6);
            CreateCentralFortress(map, reserved, width, height);
            CreateIceRoutes(map, reserved, width, height, 3);
            CreateFrozenCrossroads(map, reserved, width, height);
            CreateMirroredBushes(map, reserved, width, height, 2);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Brick, 20, 2, 2, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Concrete, 10, 2, 2, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Ice, 14, 2, 2, width, height);
        }

        private static void BuildFortress(MapCell[,] map, bool[,] reserved, Random rnd, int width, int height)
        {
            ReserveRect(map, reserved, (width / 2) - 4, (height / 2) - 5, 9, 10);
            CreateCitadel(map, reserved, width, height);
            CreateWaterPockets(map, reserved, width, height);
            CreateIceRoutes(map, reserved, width, height, 1);
            CreateMirroredBushes(map, reserved, width, height, 2);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Brick, 34, 3, 3, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Concrete, 18, 2, 3, width, height);
            ScatterMirroredBlocks(map, reserved, rnd, CellType.Bush, 10, 2, 2, width, height);
        }

        private static void ReserveLane(MapCell[,] map, bool[,] reserved, int startX, int height)
        {
            for (int x = startX; x < startX + 3; x++)
                for (int y = 0; y < height; y++)
                    ReserveCell(map, reserved, x, y);
        }

        private static void CreateCentralFortress(MapCell[,] map, bool[,] reserved, int width, int height)
        {
            int cx = (width / 2) - 4;
            int cy = (height / 2) - 4;

            TryFillRect(map, reserved, cx, cy, 8, 1, CellType.Concrete);
            TryFillRect(map, reserved, cx, cy + 7, 8, 1, CellType.Concrete);
            TryFillRect(map, reserved, cx, cy + 1, 1, 6, CellType.Concrete);
            TryFillRect(map, reserved, cx + 7, cy + 1, 1, 6, CellType.Concrete);
            TryFillRect(map, reserved, cx + 2, cy + 2, 4, 4, CellType.Brick);

            TryFillRect(map, reserved, cx - 6, cy + 1, 3, 2, CellType.Brick);
            TryFillRect(map, reserved, cx + 11, cy + 4, 3, 2, CellType.Brick);
            TryFillRect(map, reserved, cx - 6, cy + 5, 3, 2, CellType.Brick);
            TryFillRect(map, reserved, cx + 11, cy + 1, 3, 2, CellType.Brick);
        }

        private static void CreateCitadel(MapCell[,] map, bool[,] reserved, int width, int height)
        {
            int cx = (width / 2) - 6;
            int cy = (height / 2) - 5;

            TryFillRect(map, reserved, cx, cy, 12, 1, CellType.Concrete);
            TryFillRect(map, reserved, cx, cy + 9, 12, 1, CellType.Concrete);
            TryFillRect(map, reserved, cx, cy + 1, 1, 8, CellType.Concrete);
            TryFillRect(map, reserved, cx + 11, cy + 1, 1, 8, CellType.Concrete);

            TryFillRect(map, reserved, cx + 2, cy + 2, 8, 6, CellType.Brick);
            TryFillRect(map, reserved, cx + 4, cy + 1, 4, 1, CellType.Brick);
            TryFillRect(map, reserved, cx + 4, cy + 8, 4, 1, CellType.Brick);
            TryFillRect(map, reserved, cx + 5, cy + 3, 2, 4, CellType.Empty);

            TryFillRect(map, reserved, cx - 5, cy + 2, 3, 2, CellType.Brick);
            TryFillRect(map, reserved, cx + 14, cy + 2, 3, 2, CellType.Brick);
            TryFillRect(map, reserved, cx - 5, cy + 6, 3, 2, CellType.Concrete);
            TryFillRect(map, reserved, cx + 14, cy + 6, 3, 2, CellType.Concrete);
        }

        private static void CreateWaterPockets(MapCell[,] map, bool[,] reserved, int width, int height)
        {
            int middleY = (height / 2) - 1;
            int leftX = 12;
            int rightX = width - 16;

            TryFillRect(map, reserved, leftX, middleY - 4, 3, 3, CellType.Water);
            TryFillRect(map, reserved, leftX + 5, middleY + 2, 4, 2, CellType.Water);
            TryFillRect(map, reserved, rightX, middleY + 2, 3, 3, CellType.Water);
            TryFillRect(map, reserved, rightX - 6, middleY - 4, 4, 2, CellType.Water);
        }

        private static void CreateWetlandChannels(MapCell[,] map, bool[,] reserved, int width, int height)
        {
            int midY = height / 2;
            TryFillRect(map, reserved, 1, midY - 2, 4, 4, CellType.Water);
            TryFillRect(map, reserved, width - 5, midY - 2, 4, 4, CellType.Water);
            TryFillRect(map, reserved, 10, 8, 3, 6, CellType.Water);
            TryFillRect(map, reserved, width - 13, height - 14, 3, 6, CellType.Water);
        }

        private static void CreateFrozenCrossroads(MapCell[,] map, bool[,] reserved, int width, int height)
        {
            int centerX = (width / 2) - 2;
            int centerY = (height / 2) - 2;
            TryFillRect(map, reserved, centerX, centerY, 5, 4, CellType.Ice);
            TryFillRect(map, reserved, centerX - 8, centerY + 1, 4, 2, CellType.Ice);
            TryFillRect(map, reserved, centerX + 9, centerY + 1, 4, 2, CellType.Ice);
        }

        private static void CreateMirroredBushes(MapCell[,] map, bool[,] reserved, int width, int height, int rows)
        {
            int centerX = width / 2;
            for (int row = 0; row < rows; row++)
            {
                int y = 7 + (row * 4);
                if (y >= height - 6)
                    break;

                TryFillMirroredRect(map, reserved, centerX, 3, y, 3, 2, CellType.Bush);
                TryFillMirroredRect(map, reserved, centerX, 9, y + 1, 2, 2, CellType.Bush);
            }
        }

        private static void CreateIceRoutes(MapCell[,] map, bool[,] reserved, int width, int height, int extraPatches)
        {
            TryFillRect(map, reserved, 2, height / 2 - 6, 3, 5, CellType.Ice);
            TryFillRect(map, reserved, width - 5, height / 2 + 1, 3, 5, CellType.Ice);
            TryFillRect(map, reserved, 12, height - 9, 4, 2, CellType.Ice);
            TryFillRect(map, reserved, width - 16, 7, 4, 2, CellType.Ice);

            for (int i = 0; i < extraPatches; i++)
            {
                int x = 8 + (i * 9);
                int y = 10 + (i * 6);
                TryFillRect(map, reserved, x, y, 3, 2, CellType.Ice);
                TryFillRect(map, reserved, width - x - 3, height - y - 2, 3, 2, CellType.Ice);
            }
        }

        private static void ScatterMirroredBlocks(
            MapCell[,] map,
            bool[,] reserved,
            Random rnd,
            CellType type,
            int attempts,
            int maxWidth,
            int maxHeight,
            int width,
            int height)
        {
            int centerX = width / 2;

            for (int i = 0; i < attempts; i++)
            {
                int blockWidth = rnd.Next(1, maxWidth + 1);
                int blockHeight = rnd.Next(1, maxHeight + 1);
                int x = rnd.Next(1, centerX - 4);
                int y = rnd.Next(5, height - 6);

                if (TryFillMirroredRect(map, reserved, centerX, x, y, blockWidth, blockHeight, type))
                    continue;

                if (CanPlace(map, reserved, x, y, blockWidth, blockHeight))
                    FillRect(map, x, y, blockWidth, blockHeight, type);
            }
        }

        private static bool TryFillMirroredRect(MapCell[,] map, bool[,] reserved, int centerX, int x, int y, int w, int h, CellType type)
        {
            int mirrorX = (centerX * 2) - x - w;
            if (!CanPlace(map, reserved, x, y, w, h) || !CanPlace(map, reserved, mirrorX, y, w, h))
                return false;

            FillRect(map, x, y, w, h, type);
            FillRect(map, mirrorX, y, w, h, type);
            return true;
        }

        private static bool TryFillRect(MapCell[,] map, bool[,] reserved, int x, int y, int w, int h, CellType type)
        {
            if (type == CellType.Empty)
            {
                ReserveRect(map, reserved, x, y, w, h);
                return true;
            }

            if (!CanPlace(map, reserved, x, y, w, h))
                return false;

            FillRect(map, x, y, w, h, type);
            return true;
        }

        private static void FillHQ(MapCell[,] map, int x, int y, CellType type)
        {
            if (x >= 0 && y >= 0 && x < map.GetLength(0) && y < map.GetLength(1))
                map[x, y] = new MapCell(type);
        }

        private static bool CanPlace(MapCell[,] map, bool[,] reserved, int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x + w > map.GetLength(0) || y + h > map.GetLength(1))
                return false;

            for (int yy = y; yy < y + h; yy++)
            {
                for (int xx = x; xx < x + w; xx++)
                {
                    if (reserved[xx, yy] || map[xx, yy].Type != CellType.Empty)
                        return false;
                }
            }

            return true;
        }

        private static void FillRect(MapCell[,] map, int x, int y, int w, int h, CellType type)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    map[xx, yy] = new MapCell(type);
        }

        private static void ReserveCell(MapCell[,] map, bool[,] reserved, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
                return;

            reserved[x, y] = true;
            map[x, y] = new MapCell(CellType.Empty);
        }

        private static void ReserveRect(MapCell[,] map, bool[,] reserved, int x, int y, int w, int h)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    ReserveCell(map, reserved, xx, yy);
        }
    }
}
