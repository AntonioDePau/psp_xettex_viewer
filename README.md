# PSP XetTex Viewer
A tool to display unswizzled 4/8bpp TEX (or XET) psp texture files (for Lord of Apocalypse in particular, but may work for other Square Enix titles using XET texture files)

Drag a XET file over the exe and it will show you each texture it contains one by one (in a different window).
~~You need to close the first window for the next one to appear.~~ (Needs serious improvements and code cleaning)

TODO:
~~- Display the form even if no file has been dragged on the exe~~ Done
~~- Manage new files once the tool is open (drag-n-drop, browse file)~~ Done
- Extract textures to a folder (save as BMP probably)
- Repack extracted textures into a XET file

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



Huge thanks to https://github.com/nickworonekin whose https://github.com/nickworonekin/puyotools repo allowed me to unswizzle textures!
