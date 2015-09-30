using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    internal sealed class ImcJumpBlock : ImcBlock
    {
        public override int Size
        {
            get
            {
                return 16;
            }
        }

        public int Destination { get; set; }

        public int HookId { get; set; }

        public int Delay { get; set; }

        public override void Read(BinaryReader file, int positionOffset)
        {
            int size = file.ReadBigEndianInt32();

            if (size != 16)
            {
                throw new InvalidDataException();
            }

            this.Position = file.ReadBigEndianInt32() - positionOffset;
            this.Destination = file.ReadBigEndianInt32() - positionOffset;
            this.HookId = file.ReadBigEndianInt32();
            this.Delay = file.ReadBigEndianInt32();
        }

        public override void Write(BinaryWriter file, int positionOffset)
        {
            file.Write(Encoding.ASCII.GetBytes("JUMP"));
            file.WriteBigEndian(this.Size);
            file.WriteBigEndian(this.Position + positionOffset);
            file.WriteBigEndian(this.Destination + positionOffset);
            file.WriteBigEndian(this.HookId);
            file.WriteBigEndian(this.Delay);
        }
    }
}
