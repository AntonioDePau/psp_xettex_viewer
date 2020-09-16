using System;
using System.Collections.Generic;
using System.Drawing;

namespace ConsoleProject.DTO {

    public class Texture {

        public int Width { get; set; }
        public int Height { get; set; }
        public short Largest { get; set; }
        public short Unk06 { get; set; }
        public int DataOffset { get; set; }
        public int BitsPerPixel { get; set; }
        public int Number { get; set; }
        public int Unk08 { get; set; }
        public int InfoOffset { get; set; }
        public int PaletteOffset { get; set; }
        public byte[] Binary { get; set; }
        public byte[] Unswizzled { get; set; }
        public byte[] Palette { get; set; }
        public Bitmap Bitmap { get; set; }
        public string Name { get; set; }
        public List<Color> Colors { get; set; }

        public Texture() {
            Colors = new List<Color>();
            Unk06 = 0;
        }

        public Texture(int w, int h) {
            Colors = new List<Color>();
            Width = w;
            Height = h;
            Largest = (short)Math.Max(w, h);
        }

        public override string ToString() {
            return "Height: " + Height + " Width: " + Width +
                " Largest: " + Largest + " Unk06: " + Unk06 +
                " DataOffset: " + DataOffset + " BitsPerPixel: " + BitsPerPixel +
                " Number: " + Number + " Unk08: " + Unk08 +
                " InfoOffset: " + InfoOffset + " Name: " + Name;
        
        }

    }
}
