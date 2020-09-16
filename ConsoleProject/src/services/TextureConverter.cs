using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ConsoleProject.DTO;
using ConsoleProject.Utils;

namespace ConsoleProject.Services {

    public class TextureConverter {

        public List<Texture> ParseFile(string filename) {
            using (BigEndianBinaryReader reader = new BigEndianBinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
                TexHeaderMap textureHeader = this.PopulateHeaderMap(reader);

                if (textureHeader.FileExtension != "XET.") {
                    throw new InvalidDataException("Invalid file extension!");
                }

                Console.WriteLine("Image count:" + textureHeader.FileCount.ToString());

                List<Texture> images = new List<Texture>();
                for (int i = 0; i < textureHeader.FileCount; i++) {
                    images.Add(this.PopulateTexture(i, reader, textureHeader));
                }

                return images;
            }
        }

        private TexHeaderMap PopulateHeaderMap(BigEndianBinaryReader f) {
            return new TexHeaderMap {
                FileExtension = System.Text.Encoding.UTF8.GetString(f.ReadBytes(4), 0, 4),
                FileVersion = f.ReadInt32(),
                FileCount = f.ReadInt16(),
                FileCountB = f.ReadInt16(),
                Unk0c = f.ReadInt32(),
                FileListOffset = f.ReadInt32(),
                FileInfoOffset = f.ReadInt32(),
                FileNamesOffset = f.ReadInt32()
            };
        }

        private Texture PopulateTexture(int i, BigEndianBinaryReader f, TexHeaderMap textureHeader) {
            Texture texture = new Texture();

            f.BaseStream.Seek(textureHeader.FileListOffset + (4 * i) + (12 * i), SeekOrigin.Begin);
            texture.BitsPerPixel = f.ReadInt32() == 5 ? 8 : 4; // TODO documentation
            texture.Number = f.ReadInt32();
            texture.Unk08 = f.ReadInt32();
            texture.InfoOffset = f.ReadInt32();

            f.BaseStream.Seek(texture.InfoOffset, SeekOrigin.Begin);
            texture.Width = f.ReadInt16();
            texture.Height = f.ReadInt16();
            texture.Largest = f.ReadInt16();
            texture.Unk06 = f.ReadInt16();
            texture.DataOffset = f.ReadInt32();

            f.BaseStream.Seek(textureHeader.FileInfoOffset + 4 + (8 * i), SeekOrigin.Begin);
            int paletteOffset = f.ReadInt32(); // TODO persist on DTO

            byte[] nb = f.ReadAt(textureHeader.FileNamesOffset, (int)f.BaseStream.Length - textureHeader.FileNamesOffset);
            string name = System.Text.Encoding.UTF8.GetString(nb, 0, nb.Length).Split('\x00')[i];
            texture.Name = name;

            texture.Binary = f.ReadAt(texture.DataOffset, paletteOffset - texture.DataOffset);
            texture.Palette = f.ReadAt(paletteOffset, texture.InfoOffset - paletteOffset);
            texture.Unswizzled = UnSwizzle(texture);
            texture.Bitmap = GetBitmapFromPalette(texture);

            // TODO
            Console.WriteLine(texture);

            return texture;
        }

        private Bitmap GetBitmapFromPalette(Texture texture) {
            Bitmap bmp = new Bitmap(texture.Width, texture.Height, PixelFormat.Format8bppIndexed);
            if (texture.BitsPerPixel == 4)
                bmp = new Bitmap(texture.Width, texture.Height, PixelFormat.Format4bppIndexed);
            int row = 0;
            int bitsPerPixelMultiplier = 8 / texture.BitsPerPixel;

            System.Drawing.Imaging.ColorPalette pal = bmp.Palette;
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            byte[] palette = texture.Palette;
            for (int c = 0; c < pal.Entries.Length; c++) {
                Color col = Color.FromArgb(palette[c * 4 + 3], palette[c * 4], palette[c * 4 + 1], palette[c * 4 + 2]);
                pal.Entries[c] = col;
            }

            bmp.Palette = pal;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

            byte[] bytes = new byte[data.Height * data.Stride];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (int x = 0; x < texture.Unswizzled.Length; x++) {
                if (x >= texture.Width + (texture.Width * row)) row++;

                int col = (x - (texture.Width * row));
                int dataIndex = x;
                int paletteIndex = 0;
                if (texture.BitsPerPixel == 4) {
                    paletteIndex = (texture.Unswizzled[dataIndex] & 0xF0) >> 4 | (texture.Unswizzled[dataIndex] & 0x0F) << 4;
                } else {
                    paletteIndex = texture.Unswizzled[dataIndex];
                }
                int p = row * (texture.Width) + col;
                bytes[p] = (byte)paletteIndex;
            }


            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        private byte[] UnSwizzle(Texture texture) {
            NewTexture swizzledTexture = new NewTexture {
                Width = texture.Width,
                Height = texture.Height,
                BitsPerPixel = texture.BitsPerPixel,
                Binary = texture.Binary
            };
            return SwizzleService.UnSwizzle(swizzledTexture);
        }

    }

}
