using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCometRefill
{
    public long Index { get; set; }

    public int Typeid { get; set; }

    public short Min { get; set; }

    public short Max { get; set; }
}
