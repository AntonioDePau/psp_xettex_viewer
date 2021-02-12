# PSP XetTex Viewer
A tool to display unswizzled 4/8bpp TEX (or XET) psp texture files (for Lord of Apocalypse in particular, but may work for other Square Enix titles using XET texture files)

Drag a XET file over the exe or the form and it will show you the first texture, as well as the list of textures that you can select in the right-side list.
The "Extract" button saves the textures to the Xet2Png directory as PNG files.

XET file format:
- 4 first bytes: file format --> XET. (LE, would be .TEX in BE)
- 1st next byte: file version (?) --> 1
- 2 next bytes : file count [LE]
- 2 next bytes : (same as above?)
- 4 next bytes : padding?
- 4 next bytes : Offset to list of files [LE]
- 4 next bytes : Offset to files' info (bpp and palettes' offsets) [LE]
- 4 next bytes : Offset to list of files' names (separated by a null byte, in BE) [LE]

List of files:
- 4 first bytes: Bytes per pixel format (4 --> 4bpp, 5 --> 8bpp) [LE]
- 4 next bytes : file number  [LE]
- 4 next bytes : Unknown
- 4 next bytes : image info [LE]

Image info:
- 4 first bytes: Image width in pixels [LE]
- 4 next bytes : Image height in pixels [LE]
- 4 next bytes : The largest of the two values (width vs height)? [LE]
- 4 next bytes : offset of the beginning of the actual image's data


# Credits
Huge thanks to https://github.com/nickworonekin whose https://github.com/nickworonekin/puyotools
repo allowed to unswizzle textures!
