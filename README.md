# Unity URP Channel Packer Tool
A Unity Editor tool for packing grayscale textures into RGBA channels or unpacking RGBA textures into grayscale images.

## Features
- Pack up to four textures into one RGBA texture.
- Unpack RGBA textures into separate R, G, B, A grayscale images.
- Preview packed textures in the UI.
- Supports fallback colours (Black, Gray, White) and channel inversion.
- Progress bar for large textures (e.g., 4K).
- Handles non-readable textures with fix prompts.

## Installation
1. Download `Editor/ChannelPackerTool.cs` from this repository.
2. Place it in your Unity project’s `Assets/Editor` folder (create the `Editor` folder if needed).
3. Open via `Tools > Channel Packer Tool` in Unity.

## Usage
- **Pack**:
  1. Assign grayscale textures to R, G, B, or A slots.
  2. Set fallback colours for unassigned channels.
  3. Check “Invert” to flip channel values if needed.
  4. Click **Pack** and save the output PNG.
- **Unpack**:
  1. Assign an RGBA texture to the source slot.
  2. Select channels (R, G, B, A) to extract.
  3. Click **Unpack** and choose an output folder for grayscale PNGs.

## Requirements
- Tested in Unity 6000.1.5f1 (likely works in 2021.3+).
- Textures must have “Read/Write” enabled in import settings.

## License
MIT License. See [LICENSE](LICENSE) for details.

## Contributing
Fork the repo, make changes, and submit a pull request. Report bugs or suggest features via [Issues](https://github.com/YourUsername/ChannelPackerTool/issues).

## Credits
Developed by Richard/NotMyFirstNull with significant contributions from Grok (xAI) and ChatGPT.

## Usage Notes
If you encounter errors (e.g., unassigned textures) - reopening the tool window resets preferences if needed.
