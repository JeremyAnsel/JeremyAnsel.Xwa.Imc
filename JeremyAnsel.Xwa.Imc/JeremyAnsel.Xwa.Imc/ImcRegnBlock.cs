using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal sealed class ImcRegnBlock : ImcBlock
    {
        public override int Size
        {
            get
            {
                return 8;
            }
        }

        public int Length { get; set; }

        public override void Read(BinaryReader file, int positionOffset)
        {
            int size = file.ReadBigEndianInt32();

            if (size != 8)
            {
                throw new InvalidDataException();
            }

            this.Position = file.ReadBigEndianInt32() - positionOffset;
            this.Length = file.ReadBigEndianInt32();
        }

        public override void Write(BinaryWriter file, int positionOffset)
        {
            file.Write(Encoding.ASCII.GetBytes("REGN"));
            file.WriteBigEndian(this.Size);
            file.WriteBigEndian(this.Position + positionOffset);
            file.WriteBigEndian(this.Length);
        }
    }
}
