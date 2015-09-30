using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteBigEndian(this BinaryWriter file, short value)
        {
            file.Write((byte)((ushort)value >> 8));
            file.Write((byte)((ushort)value));
        }

        public static void WriteBigEndian(this BinaryWriter file, int value)
        {
            file.Write((byte)((uint)value >> 24));
            file.Write((byte)((uint)value >> 16));
            file.Write((byte)((uint)value >> 8));
            file.Write((byte)((uint)value));
        }
    }
}
