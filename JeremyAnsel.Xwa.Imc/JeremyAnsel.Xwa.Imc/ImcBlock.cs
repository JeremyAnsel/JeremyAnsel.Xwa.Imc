using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal abstract class ImcBlock
    {
        public abstract int Size { get; }

        public int Position { get; set; }

        public abstract void Read(BinaryReader file, int positionOffset);

        public abstract void Write(BinaryWriter file, int positionOffset);
    }
}
