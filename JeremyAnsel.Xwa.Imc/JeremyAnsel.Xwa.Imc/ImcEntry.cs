using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal sealed class ImcEntry
    {
        public byte Codec { get; set; }

        public int RawSize { get; set; }

        public int CompressedSize { get; set; }

        public int RawOffset { get; set; }

        //public int CompressedOffset { get; set; }

        public byte[]? Data { get; set; }
    }
}
