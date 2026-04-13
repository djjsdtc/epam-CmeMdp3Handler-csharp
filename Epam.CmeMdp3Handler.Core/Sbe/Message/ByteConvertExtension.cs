using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epam.CmeMdp3Handler.Sbe.Message;

public static class ByteConvertExtension
{
    public static sbyte ToSignedByte(this byte value) => value > 127 ? (sbyte)(value - 256) : (sbyte)value;
}
