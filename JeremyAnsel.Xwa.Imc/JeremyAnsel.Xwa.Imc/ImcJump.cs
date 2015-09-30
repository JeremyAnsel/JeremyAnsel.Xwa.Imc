using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    public sealed class ImcJump : ImcMapItem
    {
        public ImcJump()
        {
            this.Delay = 500;
        }

        public int Destination { get; set; }

        public int HookId { get; set; }

        public int Delay { get; set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} Jump {1} {2} {3}",
                this.Position,
                this.Destination,
                this.HookId,
                this.Delay);
        }
    }
}
