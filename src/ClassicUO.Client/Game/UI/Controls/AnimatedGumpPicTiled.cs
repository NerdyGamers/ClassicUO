// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// An animated tiled background control that cycles through multiple gump graphics to create animated wallpaper effects.
    /// </summary>
    internal class AnimatedGumpPicTiled : Control
    {
        private readonly ushort[] _graphics;
        private int _currentFrame;
        private float _nextFrameTime;
        private readonly float _frameDelay;

        /// <summary>
        /// Creates an animated tiled background control.
        /// </summary>
        /// <param name="graphics">Array of gump graphics to cycle through for animation</param>
        /// <param name="frameDelayMs">Delay between frames in milliseconds (default: 100ms)</param>
        public AnimatedGumpPicTiled(ushort[] graphics, float frameDelayMs = 100f)
        {
            CanMove = true;
            AcceptMouseInput = true;
            _graphics = graphics ?? new ushort[] { 0x0E14 }; // Default to static background
            _currentFrame = 0;
            _frameDelay = frameDelayMs;
            _nextFrameTime = Time.Ticks + _frameDelay;
            
            UpdateGraphic();
        }

        public AnimatedGumpPicTiled(int x, int y, int width, int height, ushort[] graphics, float frameDelayMs = 100f) 
            : this(graphics, frameDelayMs)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ushort Hue { get; set; }

        private ushort CurrentGraphic => _graphics != null && _graphics.Length > 0 
            ? _graphics[_currentFrame % _graphics.Length] 
            : (ushort)0x0E14;

        private void UpdateGraphic()
        {
            if (_graphics == null || _graphics.Length == 0)
                return;

            ushort graphic = CurrentGraphic;
            
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(graphic);

            if (gumpInfo.Texture == null)
            {
                return;
            }

            // Set dimensions based on the current frame
            if (Width == 0)
            {
                Width = gumpInfo.UV.Width;
            }

            if (Height == 0)
            {
                Height = gumpInfo.UV.Height;
            }
        }

        public override void Update()
        {
            base.Update();

            // Only animate if we have multiple frames
            if (_graphics != null && _graphics.Length > 1 && Time.Ticks >= _nextFrameTime)
            {
                _currentFrame = (_currentFrame + 1) % _graphics.Length;
                _nextFrameTime = Time.Ticks + _frameDelay;
                UpdateGraphic();
            }
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            ushort graphic = CurrentGraphic;
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(graphic);

            var texture = gumpInfo.Texture;
            if (texture != null)
            {                
                var sourceRectangle = gumpInfo.UV;
                renderLists.AddGumpWithAtlas
                (
                    (batcher) =>
                    {
                        batcher.DrawTiled(
                            texture,
                            new Rectangle(x, y, Width, Height),
                            sourceRectangle,
                            hueVector,
                            layerDepth
                        );
                        return true;
                    }
                );
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        public override bool Contains(int x, int y)
        {
            int width = Width;
            int height = Height;

            x -= Offset.X;
            y -= Offset.Y;

            ushort graphic = CurrentGraphic;
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(graphic);

            if (gumpInfo.Texture == null)
            {
                return false;
            }

            if (width == 0)
            {
                width = gumpInfo.UV.Width;
            }

            if (height == 0)
            {
                height = gumpInfo.UV.Height;
            }

            while (x > gumpInfo.UV.Width && width > gumpInfo.UV.Width)
            {
                x -= gumpInfo.UV.Width;
                width -= gumpInfo.UV.Width;
            }

            while (y > gumpInfo.UV.Height && height > gumpInfo.UV.Height)
            {
                y -= gumpInfo.UV.Height;
                height -= gumpInfo.UV.Height;
            }

            if (x > width || y > height)
            {
                return false;
            }

            return Client.Game.UO.Gumps.PixelCheck(graphic, x, y);
        }
    }
}
