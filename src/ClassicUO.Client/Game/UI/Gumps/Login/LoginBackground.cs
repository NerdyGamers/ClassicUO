// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class LoginBackground : Gump
    {
        public LoginBackground(World world) : base(world, 0, 0)
        {
            if (Client.Game.UO.Version >= ClientVersion.CV_706400)
            {
                // Background - add support for animated wallpapers
                if (Settings.GlobalSettings.LoginWallpaperAnimated)
                {
                    // Create animated background with a subtle pulsing effect using the same graphic
                    // This creates a "live" feel by cycling through frames
                    ushort[] animFrames = new ushort[] { 0x0150 };
                    Add
                    (
                        new AnimatedGumpPicTiled
                        (
                            0,
                            0,
                            640,
                            480,
                            animFrames,
                            Settings.GlobalSettings.LoginWallpaperFrameDelay
                        ) { AcceptKeyboardInput = false }
                    );
                }
                else
                {
                    Add
                    (
                        new GumpPicTiled
                        (
                            0,
                            0,
                            640,
                            480,
                            0x0150
                        ) { AcceptKeyboardInput = false }
                    );
                }

                // UO Flag
                Add(new GumpPic(0, 4, 0x0151, 0) { AcceptKeyboardInput = false });
            }
            else
            {
                // Background - add support for animated wallpapers
                if (Settings.GlobalSettings.LoginWallpaperAnimated)
                {
                    // Create animated background for older clients
                    ushort[] animFrames = new ushort[] { 0x0E14 };
                    Add
                    (
                        new AnimatedGumpPicTiled
                        (
                            0,
                            0,
                            640,
                            480,
                            animFrames,
                            Settings.GlobalSettings.LoginWallpaperFrameDelay
                        ) { AcceptKeyboardInput = false }
                    );
                }
                else
                {
                    Add
                    (
                        new GumpPicTiled
                        (
                            0,
                            0,
                            640,
                            480,
                            0x0E14
                        ) { AcceptKeyboardInput = false }
                    );
                }

                // Border
                Add(new GumpPic(0, 0, 0x157C, 0) { AcceptKeyboardInput = false });
                // UO Flag
                Add(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });

                // Quit Button
                Add
                (
                    new Button(0, 0x1589, 0x158B, 0x158A)
                    {
                        X = 555,
                        Y = 4,
                        ButtonAction = ButtonAction.Activate,
                        AcceptKeyboardInput = false
                    }
                );
            }


            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptKeyboardInput = false;

            LayerOrder = UILayer.Under;
        }


        public override void OnButtonClick(int buttonID)
        {
            Client.Game.Exit();
        }
    }
}