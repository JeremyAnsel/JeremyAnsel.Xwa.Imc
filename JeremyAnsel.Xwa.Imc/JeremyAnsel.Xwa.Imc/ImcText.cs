using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    public sealed class ImcText : ImcMapItem
    {
        public string Text { get; set; } = string.Empty;

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} Text {1}",
                this.Position,
                this.Text);
        }
    }
}
