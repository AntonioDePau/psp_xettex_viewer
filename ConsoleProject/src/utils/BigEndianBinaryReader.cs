using System;
using System.IO;
using System.Linq;

namespace ConsoleProject.Utils {

    public class BigEndianBinaryReader : BinaryReader {

        public BigEndianBinaryReader(Stream input) : base(input) {
            // empty
        }

        public override short ReadInt16() {
            byte[] b = ReadBytes(2);
            b.Reverse();
            return BitConverter.ToInt16(b, 0);
        }

        public override int ReadInt32() {
            byte[] b = ReadBytes(4);
            b.Reverse();
            return BitConverter.ToInt32(b, 0);
        }

        public override long ReadInt64() {
            byte[] b = ReadBytes(8);
            b.Reverse();
            return BitConverter.ToInt64(b, 0);
        }

        public byte[] ReadAt(int start, int length) {
            BaseStream.Seek(start, SeekOrigin.Begin);
            return ReadBytes(length);
        }
    }
}
