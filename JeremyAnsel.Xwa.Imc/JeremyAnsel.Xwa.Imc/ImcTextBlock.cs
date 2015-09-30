using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal sealed class ImcTextBlock : ImcBlock
    {
        public override int Size
        {
            get
            {
                return 4 + Encoding.ASCII.GetByteCount(this.Text) + 1;
            }
        }

        public string Text { get; set; }

        public override void Read(BinaryReader file, int positionOffset)
        {
            int size = file.ReadBigEndianInt32();

            this.Position = file.ReadBigEndianInt32() - positionOffset;
            this.Text = Encoding.ASCII.GetString(file.ReadBytes(size - 5));
            file.ReadByte();
        }

        public override void Write(BinaryWriter file, int positionOffset)
        {
            file.Write(Encoding.ASCII.GetBytes("TEXT"));
            file.WriteBigEndian(this.Size);
            file.WriteBigEndian(this.Position + positionOffset);
            file.Write(Encoding.ASCII.GetBytes(this.Text));
            file.Write((byte)0);
        }
    }
}
