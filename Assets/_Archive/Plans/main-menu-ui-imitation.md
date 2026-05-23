# Project Overview
- Game Title: Fight or Flight
- High-Level Concept: A sci-fi flight combat/exploration game.
- Players: Single player.
- Inspiration / Reference Games: Sci-fi space combat sims.
- Tone / Art Direction: Futuristic, high-tech, space aesthetic.
- Target Platform: StandaloneOSX
- Screen Orientation / Resolution: Landscape 1920x1080 (Canvas reference resolution)
- Render Pipeline: Built-in

# Game Mechanics
## Core Gameplay Loop
The player navigates through space, possibly engaging in combat or exploration. The main menu serves as the entry point for starting the game, viewing instructions, adjusting settings, or quitting.

## Controls and Input Methods
The main menu is navigated via mouse clicks on UI buttons.

# UI
The main menu UI features futuristic, glowing rectangular buttons. 
- "START GAME": Cyan glow, dark background.
- "INSTRUCTIONS" & "SETTINGS": Blue/Purple glow.
- "QUIT": Red/Pink glow.
- Font: Clean, uppercase sans-serif.
- Layout: Vertically stacked in the center/lower part of the screen.

# Key Asset & Context
- `Assets/Fight or Flight/Content/UI/Sprites/SciFiButtonFrame.png`: A new sprite for the button frame.
- `Assets/Fight or Flight/Code/UI/MainMenuController.cs`: The script managing the menu layout and styling.

# Implementation Steps
1. **Generate Button Frame Sprite**:
   - Use AI generation to create a clean, 9-sliceable sci-fi button frame matching the style in the reference image.
   - Prompt: "A sci-fi futuristic UI button frame. Rectangular with thin glowing cyan borders and a semi-transparent dark background. Sharp corners with technical detailing. White background for extraction."
2. **Decompose/Extract Background (Optional)**:
   - Extract the background image from the reference if needed, or generate a matching one.
3. **Update MainMenuController.cs**:
   - Modify `StyleButton` to use the new `SciFiButtonFrame` sprite.
   - Set `Image.type = Image.Type.Sliced` and configure the sprite borders (9-slicing) via script or editor.
   - Apply specific colors to each button's Image component to match the image:
     - Start: Cyan (`#00FFFF`)
     - Instructions/Settings: Light Blue/Purple (`#A0A0FF`)
     - Quit: Pink/Red (`#FF5050`)
   - Update the font to `Roboto-Bold` or `Inter` from the project.
   - Ensure `alphaHitTestMinimumThreshold` is set to `0.1f` on the button images to restrict clickability to the "seeable" parts.
4. **Fine-tune Layout**:
   - Ensure all buttons have the `unifiedSize` of `(560f, 96f)` as currently defined, which matches the visual weight in the image.
   - Adjust `startY` and `spacing` if necessary to match the vertical alignment in the image.

# Verification & Testing
- **Visual Check**: Compare the in-game buttons with the reference image.
- **Clickability Test**: Ensure buttons only respond to clicks within the visible frame and text areas (not in large transparent gaps if any).
- **Functionality Test**: Ensure all buttons still trigger their respective actions (Start, Open Instructions, Open Settings, Quit).
