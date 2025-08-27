// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MiniMapGump : Gump
    {
        struct ColorInfo
        {
            public ushort Color;
            public sbyte Z;
            public bool IsLand;
        }

        private bool _draw;
        private int _lastMap = -1;
        private long _timeMS;
        private bool _useLargeMap;
        private ushort _x, _y;
        private static readonly uint[][] _blankGumpsPixels = new uint[4][];
        /// <summary>
        /// Holds the computed path to render on the minimap while auto-walking.
        /// </summary>
        private Point[] _path;

        const ushort SMALL_MAP_GRAPHIC = 5010;
        const ushort BIG_MAP_GRAPHIC = 5011;

        public MiniMapGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }

        public override GumpType GumpType => GumpType.MiniMap;

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isminimized", _useLargeMap.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            _useLargeMap = bool.Parse(xml.GetAttribute("isminimized"));
            CreateMap();
        }

        private void CreateMap()
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                _useLargeMap ? BIG_MAP_GRAPHIC : SMALL_MAP_GRAPHIC
            );

            int index = _useLargeMap ? 1 : 0;

            if (_blankGumpsPixels[index] == null)
            {
                int size = gumpInfo.UV.Width * gumpInfo.UV.Height;
                _blankGumpsPixels[index] = new uint[size];
                _blankGumpsPixels[index + 2] = new uint[size];
                gumpInfo.Texture.GetData(0, gumpInfo.UV, _blankGumpsPixels[index], 0, size);

                Array.Copy(_blankGumpsPixels[index], 0, _blankGumpsPixels[index + 2], 0, size);
            }

            Width = gumpInfo.UV.Width;
            Height = gumpInfo.UV.Height;
            CreateMiniMapTexture(gumpInfo.Texture, gumpInfo.UV, true);
        }

        public override void Update()
        {
            if (!World.InGame)
            {
                return;
            }

            if (_lastMap != World.MapIndex)
            {
                CreateMap();
                _lastMap = World.MapIndex;
            }

            if (_timeMS < Time.Ticks)
            {
                _draw = !_draw;
                _timeMS = (long)Time.Ticks + 500;
            }
        }

        public bool ToggleSize(bool? large = null)
        {
            if (large.HasValue)
            {
                _useLargeMap = large.Value;
            }
            else
            {
                _useLargeMap = !_useLargeMap;
            }

            CreateMap();

            return _useLargeMap;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                _useLargeMap ? BIG_MAP_GRAPHIC : SMALL_MAP_GRAPHIC
            );

            if (gumpInfo.Texture == null)
            {
                Dispose();

                return false;
            }

            batcher.Draw(gumpInfo.Texture, new Vector2(x, y), gumpInfo.UV, hueVector);

            CreateMiniMapTexture(gumpInfo.Texture, gumpInfo.UV);

            batcher.Draw(gumpInfo.Texture, new Vector2(x, y), gumpInfo.UV, hueVector);

            int w = Width >> 1;
            int h = Height >> 1;

            if (_draw)
            {
                Texture2D mobilesTextureDot = SolidColorTextureCache.GetTexture(Color.Red);

                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob == World.Player)
                    {
                        continue;
                    }

                    int xx = mob.X - World.Player.X;
                    int yy = mob.Y - World.Player.Y;

                    int gx = xx - yy;
                    int gy = xx + yy;

                    hueVector = ShaderHueTranslator.GetHueVector(
                        Notoriety.GetHue(mob.NotorietyFlag)
                    );

                    batcher.Draw(
                        mobilesTextureDot,
                        new Rectangle(x + w + gx, y + h + gy, 2, 2),
                        hueVector
                    );
                }

                //DRAW PLAYER DOT
                hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle(x + w, y + h, 2, 2),
                    hueVector
                );
            }

            if (_path != null)
            {
                // Draw the remaining path segments and destination while autowalking.
                if (World.Player.Pathfinder.AutoWalking)
                {
                    Vector3 pathHue = ShaderHueTranslator.GetHueVector(0);
                    Texture2D lineTexture = SolidColorTextureCache.GetTexture(Color.Yellow);
                    Texture2D pinTexture = SolidColorTextureCache.GetTexture(Color.Red);
                    int startIndex = World.Player.Pathfinder.CurrentPathNode;

                    for (int i = Math.Max(startIndex, 0); i < _path.Length - 1; i++)
                    {
                        Point p0 = _path[i];
                        Point p1 = _path[i + 1];

                        int px0 = p0.X - World.Player.X + w;
                        int py0 = p0.Y - World.Player.Y;
                        int gx0 = px0 - py0;
                        int gy0 = px0 + py0;

                        int px1 = p1.X - World.Player.X + w;
                        int py1 = p1.Y - World.Player.Y;
                        int gx1 = px1 - py1;
                        int gy1 = px1 + py1;

                        batcher.DrawLine(
                            lineTexture,
                            new Vector2(x + gx0, y + gy0),
                            new Vector2(x + gx1, y + gy1),
                            pathHue,
                            1
                        );
                    }

                    Point dest = _path[_path.Length - 1];
                    int pdx = dest.X - World.Player.X + w;
                    int pdy = dest.Y - World.Player.Y;
                    int pgx = pdx - pdy;
                    int pgy = pdx + pdy;

                    batcher.Draw(pinTexture, new Rectangle(x + pgx - 2, y + pgy - 2, 4, 4), pathHue);
                }
                else
                {
                    _path = null;
                }
            }

            return base.Draw(batcher, x, y);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                ToggleSize();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts pathfinding when the user Ctrl+Left-clicks the minimap.
        /// </summary>
        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (
                button == MouseButtonType.Left
                && Keyboard.Ctrl
                && ProfileManager.CurrentProfile.EnableMiniMapPathfinding
            )
            {
                if (TryGetWorldPoint(x, y, out ushort wx, out ushort wy))
                {
                    sbyte z = World.Map.GetTileZ(wx, wy);

                    if (World.Player.Pathfinder.WalkTo(wx, wy, z, 0))
                    {
                        _path = new Point[World.Player.Pathfinder.PathSize];
                        World.Player.Pathfinder.CopyPath(_path);
                    }
                }
            }
        }

        /// <summary>
        /// Translates a minimap click position into world coordinates.
        /// </summary>
        private bool TryGetWorldPoint(int x, int y, out ushort worldX, out ushort worldY)
        {
            int centerX = Width >> 1;
            int centerY = Height >> 1;
            int px = (x + y) >> 1;
            int py = (y - x) >> 1;

            worldX = (ushort)(px - centerX + World.Player.X);
            worldY = (ushort)(py + World.Player.Y);

            return true;
        }

        protected override void UpdateContents()
        {
            CreateMap();
        }

        private unsafe void CreateMiniMapTexture(
            Texture2D texture,
            Rectangle bounds,
            bool force = false
        )
        {
            ushort lastX = World.Player.X;
            ushort lastY = World.Player.Y;

            if (_x != lastX || _y != lastY)
            {
                _x = lastX;
                _y = lastY;
            }
            else if (!force)
            {
                return;
            }

            int blockOffsetX = Width >> 2;
            int blockOffsetY = Height >> 2;
            int gumpCenterX = Width >> 1;
            //int gumpCenterY = Height >> 1;

            //0xFF080808 - pixel32
            //0x8421 - pixel16
            int minBlockX = ((lastX - blockOffsetX) >> 3) - 1;
            int minBlockY = ((lastY - blockOffsetY) >> 3) - 1;
            int maxBlockX = ((lastX + blockOffsetX) >> 3) + 1;
            int maxBlockY = ((lastY + blockOffsetY) >> 3) + 1;

            if (minBlockX < 0)
            {
                minBlockX = 0;
            }

            if (minBlockY < 0)
            {
                minBlockY = 0;
            }

            int maxBlockIndex = World.Map.BlocksCount;
            int mapBlockHeight = Client.Game.UO.FileManager.Maps.MapBlocksSize[World.MapIndex, 1];
            int index = _useLargeMap ? 1 : 0;

            _blankGumpsPixels[index].CopyTo(_blankGumpsPixels[index + 2], 0);

            uint[] data = _blankGumpsPixels[index + 2];

            Span<Point> table = stackalloc Point[2];
            table[0].X = 0;
            table[0].Y = 0;
            table[1].X = 0;
            table[1].Y = 1;

            Span<ColorInfo> staticsZ = stackalloc ColorInfo[64];
            var d = new ColorInfo() { Z = sbyte.MinValue };

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                int blockIndexOffset = i * mapBlockHeight;

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    int blockIndex = blockIndexOffset + j;

                    if (blockIndex >= maxBlockIndex)
                    {
                        break;
                    }

                    ref var indexMap = ref World.Map.GetIndex(i, j);

                    if (!indexMap.IsValid())
                    {
                        break;
                    }

                    staticsZ.Fill(d);
                    indexMap.StaticFile.Seek((long)indexMap.StaticAddress, System.IO.SeekOrigin.Begin);
                    indexMap.MapFile.Seek((long)indexMap.MapAddress, System.IO.SeekOrigin.Begin);
                    var cells = indexMap.MapFile.Read<MapBlock>().Cells;

                    Chunk block = World.Map.GetChunk(blockIndex);
                    int realBlockX = i << 3;
                    int realBlockY = j << 3;


                    for (int c = 0; c < indexMap.StaticCount; ++c)
                    {
                        var stblock = indexMap.StaticFile.Read<StaticsBlock>();
                        if (stblock.Color > 0 && stblock.Color != 0xFFFF && GameObject.CanBeDrawn(World, stblock.Color))
                        {
                            ref var st = ref staticsZ[stblock.Y * 8 + stblock.X];
                            if (st.Z < stblock.Z)
                            {
                                st.Color = stblock.Hue > 0 ? (ushort)(stblock.Hue + 0x4000) : stblock.Color;
                                st.Z = stblock.Z;
                                st.IsLand = stblock.Hue > 0;
                            }
                        }
                    }

                    for (int x = 0; x < 8; x++)
                    {
                        int px = realBlockX + x - lastX + gumpCenterX;

                        for (int y = 0; y < 8; y++)
                        {
                            ref readonly var cell = ref cells[(y << 3) + x];
                            int color = cell.TileID;
                            bool isLand = true;
                            int z = cell.Z;

                            ref var stZ = ref staticsZ[y * 8 + x];
                            if (stZ.Z >= z)
                            {
                                z = stZ.Z;
                                color = stZ.Color;
                                isLand = stZ.IsLand;
                            }

                            if (block != null)
                            {
                                GameObject obj = block.Tiles[x, y];

                                while (obj?.TNext != null)
                                {
                                    obj = obj.TNext;
                                }

                                for (; obj != null; obj = obj.TPrevious)
                                {
                                    if (obj is Multi)
                                    {
                                        if (obj.Hue == 0)
                                        {
                                            color = obj.Graphic;
                                            isLand = false;
                                        }
                                        else
                                        {
                                            color = obj.Hue + 0x4000;
                                        }

                                        break;
                                    }
                                }
                            }

                            if (!isLand)
                            {
                                color += 0x4000;
                            }

                            int tableSize = 2;

                            if (isLand && color > 0x4000)
                            {
                                color = Client.Game.UO.FileManager.Hues.GetColor16(
                                    16384,
                                    (ushort)(color - 0x4000)
                                ); //28672 is an arbitrary position in hues.mul, is the 14 position in the range
                            }
                            else
                            {
                                color = Client.Game.UO.FileManager.Hues.GetRadarColorData(color);
                            }

                            int py = realBlockY + y - lastY;
                            int gx = px - py;
                            int gy = px + py;

                            CreatePixels(
                                data,
                                0x8000 | color,
                                gx,
                                gy,
                                Width,
                                Height,
                                table,
                                tableSize
                            );
                        }
                    }
                }
            }

            fixed (uint* ptr = data)
            {
                texture.SetDataPointerEXT(0, bounds, (IntPtr)ptr, data.Length * sizeof(uint));
            }
        }

        private unsafe void CreatePixels(
            uint[] data,
            int color,
            int x,
            int y,
            int w,
            int h,
            Span<Point> table,
            int count
        )
        {
            int px = x;
            int py = y;

            for (int i = 0; i < count; i++)
            {
                px += table[i].X;
                py += table[i].Y;

                int gx = px;

                if (gx < 0 || gx >= w)
                {
                    continue;
                }

                int gy = py;

                if (gy < 0 || gy >= h)
                {
                    break;
                }

                int block = gy * w + gx;

                if (data[block] == 0xFF080808)
                {
                    data[block] = HuesHelper.Color16To32((ushort)color) | 0xFF_00_00_00;
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            x -= Offset.X;
            y -= Offset.Y;

            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                int index = (_useLargeMap ? 1 : 0) + 2;
                int pos = (y * Width) + x;

                if (pos < _blankGumpsPixels[index].Length)
                {
                    return _blankGumpsPixels[index][pos] != 0;
                }
            }

            return false;
        }
    }
}
