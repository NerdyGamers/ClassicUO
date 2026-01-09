// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// An animated tiled background control that cycles through multiple gump graphics to create animated wallpaper effects.
    /// Can also create pulsing effects by varying alpha/hue values.
    /// </summary>
    internal class AnimatedGumpPicTiled : Control
    {
        // Default background graphic ID for older UO clients
        private const ushort DEFAULT_BACKGROUND_GRAPHIC = 0x0E14;
        
        // Animation constants for pulse effect
        private const float PULSE_PERIOD_SECONDS = 5.0f;
        private const float MIN_PULSE_ALPHA = 0.85f;
        private const float MAX_PULSE_ALPHA = 1.0f;

        private readonly ushort[] _graphics;
        private int _currentFrame;
        private float _nextFrameTime;
        private readonly float _frameDelay;
        private float _pulseTime;
        private bool _enablePulseEffect = true;

        /// <summary>
        /// Creates an animated tiled background control.
        /// </summary>
        /// <param name="graphics">Array of gump graphics to cycle through for animation</param>
        /// <param name="frameDelayMs">Delay between frames in milliseconds (default: 100ms)</param>
        public AnimatedGumpPicTiled(ushort[] graphics, float frameDelayMs = 100f)
        {
            CanMove = true;
            AcceptMouseInput = true;
            _graphics = graphics ?? new ushort[] { DEFAULT_BACKGROUND_GRAPHIC };
            _currentFrame = 0;
            _frameDelay = frameDelayMs;
            _nextFrameTime = Time.Ticks + _frameDelay;
            _pulseTime = 0f;
            
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
            : DEFAULT_BACKGROUND_GRAPHIC;

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

            // Update pulse animation
            _pulseTime += Time.Delta;

            // Only cycle frames if we have multiple frames
            if (_graphics != null && _graphics.Length > 1 && Time.Ticks >= _nextFrameTime)
            {
                _currentFrame = (_currentFrame + 1) % _graphics.Length;
                _nextFrameTime = Time.Ticks + _frameDelay;
                UpdateGraphic();
            }
        }

        private float GetPulseAlpha()
        {
            if (!_enablePulseEffect)
                return Alpha;

            // Create a gentle pulsing effect using sine wave
            // The pulse period creates a smooth oscillation
            float pulseValue = (float)Math.Sin(_pulseTime * Math.PI / (PULSE_PERIOD_SECONDS / 2.0f));
            float normalizedPulse = (pulseValue + 1f) / 2f; // Convert from [-1,1] to [0,1]
            
            // Map to desired alpha range
            return MIN_PULSE_ALPHA + (normalizedPulse * (MAX_PULSE_ALPHA - MIN_PULSE_ALPHA));
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            
            // Apply pulsing alpha effect for live wallpaper
            float currentAlpha = GetPulseAlpha();
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, currentAlpha, true);

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
