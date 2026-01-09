# Animated Login Wallpaper - Architecture

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        LoginScene                                │
│  (Manages login flow and UI lifecycle)                          │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  │ Creates & adds to UIManager
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                     LoginBackground (Gump)                       │
│  • Reads Settings.GlobalSettings.LoginWallpaperAnimated         │
│  • Conditionally creates AnimatedGumpPicTiled or GumpPicTiled   │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  │ If animated enabled
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│              AnimatedGumpPicTiled (Control)                      │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ Update() - Called every frame                             │  │
│  │  • Increments _pulseTime by Time.Delta                    │  │
│  │  • Cycles through frames if multiple graphics provided    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ GetPulseAlpha() - Calculates current alpha                │  │
│  │  • Uses sine wave: sin(time × π / 2.5)                    │  │
│  │  • Maps to range [0.85, 1.0]                              │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ AddToRenderLists() - Renders to screen                    │  │
│  │  • Gets current pulse alpha                                │  │
│  │  • Creates hue vector with alpha                           │  │
│  │  • Draws tiled texture with alpha effect                   │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  │ Uses
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Rendering Infrastructure                       │
│  • TextureAtlas - Manages texture storage                       │
│  • Batcher2D - GPU rendering                                    │
│  • ShaderHueTranslator - Alpha/hue calculations                 │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

```
User Settings (settings.json)
        │
        │ login_wallpaper_animated: true
        │ login_wallpaper_frame_delay: 150
        ▼
Settings.GlobalSettings
        │
        │ Read on LoginBackground creation
        ▼
LoginBackground
        │
        │ Creates appropriate control
        ▼
AnimatedGumpPicTiled
        │
        │ Every frame:
        ├─► Update() ──► Increment time
        │                    │
        │                    ▼
        │              GetPulseAlpha()
        │                    │
        │                    │ Calculate alpha using sine
        │                    ▼
        └─► AddToRenderLists() ──► Apply alpha to texture
                                    │
                                    ▼
                              GPU Rendering
                                    │
                                    ▼
                              Screen Display
```

## Animation Timeline

```
Time (seconds):  0    1    2    3    4    5    6    7    8    9    10
                 │    │    │    │    │    │    │    │    │    │    │
Alpha (opacity): 
    100% ─────●─────────────────────────●─────────────────────────●──
              ╱ ╲                       ╱ ╲                       ╱
             ╱   ╲                     ╱   ╲                     ╱
            ╱     ╲                   ╱     ╲                   ╱
           ╱       ╲                 ╱       ╲                 ╱
     85% ─●─────────●───────────────●─────────●───────────────●─────
         │         │               │         │               │
         └─ 5 sec ─┘               └─ 5 sec ─┘               └─ 5 sec

     ◄──────── Breathing In ────────►◄──────── Breathing Out ──────►
```

## Class Hierarchy

```
Control (base class)
    │
    ├─── GumpPicTiled (existing, static backgrounds)
    │
    └─── AnimatedGumpPicTiled (new, animated backgrounds)
         │
         ├─ Properties:
         │   • _graphics: ushort[] - Array of gump IDs
         │   • _currentFrame: int - Current frame index
         │   • _pulseTime: float - Elapsed time for animation
         │   • _frameDelay: float - MS between frame changes
         │
         ├─ Constants:
         │   • DEFAULT_BACKGROUND_GRAPHIC = 0x0E14
         │   • PULSE_PERIOD_SECONDS = 5.0f
         │   • MIN_PULSE_ALPHA = 0.85f
         │   • MAX_PULSE_ALPHA = 1.0f
         │
         └─ Methods:
             • Update() - Update animation state
             • GetPulseAlpha() - Calculate current opacity
             • AddToRenderLists() - Render to screen
             • Contains() - Hit testing
```

## Configuration Flow

```
┌──────────────────────────────────────────────────────────────┐
│  1. User edits settings.json                                 │
│     {                                                         │
│       "login_wallpaper_animated": true,                      │
│       "login_wallpaper_frame_delay": 150                     │
│     }                                                         │
└───────────────────────┬──────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────┐
│  2. ClassicUO starts, Settings.Load() reads configuration    │
└───────────────────────┬──────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────┐
│  3. LoginScene.Load() creates LoginBackground                │
└───────────────────────┬──────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────┐
│  4. LoginBackground checks Settings.GlobalSettings           │
│     if (LoginWallpaperAnimated)                              │
│         Use AnimatedGumpPicTiled                             │
│     else                                                      │
│         Use GumpPicTiled                                     │
└───────────────────────┬──────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────┐
│  5. Control is added to UI and begins rendering             │
└──────────────────────────────────────────────────────────────┘
```

## Performance Characteristics

```
┌─────────────────────────┬──────────────────────────────────┐
│ Metric                  │ Value                            │
├─────────────────────────┼──────────────────────────────────┤
│ CPU Overhead            │ ~0.1% (one sin() calc per frame) │
│ Memory Overhead         │ ~1KB (animation state)           │
│ GPU Impact              │ Negligible (reuses atlas)        │
│ Frame Rate Impact       │ 0 FPS (time-based animation)     │
│ Texture Memory          │ 0 (reuses existing textures)     │
└─────────────────────────┴──────────────────────────────────┘
```

## Extension Points

The architecture is designed to be extensible:

1. **Multiple Frame Animation**
   ```csharp
   ushort[] frames = { 0x0E14, 0x0E15, 0x0E16 };
   new AnimatedGumpPicTiled(0, 0, 640, 480, frames, 200f);
   ```

2. **Custom Animation Functions**
   - Override `GetPulseAlpha()` for different effects
   - Add fade, bounce, or custom interpolation curves

3. **Additional Settings**
   - Pulse intensity (min/max alpha range)
   - Animation style (fade, slide, rotate)
   - Custom image paths

4. **Event Hooks**
   - OnAnimationCycleComplete
   - OnFrameChange
   - OnPulsePhaseChange
