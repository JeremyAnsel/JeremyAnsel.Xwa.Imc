using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal static class BinaryReaderExtensions
    {
        public static short ReadBigEndianInt16(this BinaryReader file)
        {
            return (short)((file.ReadByte() << 8) | file.ReadByte());
        }

        public static int ReadBigEndianInt32(this BinaryReader file)
        {
            return (int)((file.ReadByte() << 24) | (file.ReadByte() << 16) | (file.ReadByte() << 8) | file.ReadByte());
        }
    }
}
