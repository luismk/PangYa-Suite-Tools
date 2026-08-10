using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGrandZodiacTime
{
    public long Index { get; set; }

    public TimeOnly InicioTime { get; set; }

    public TimeOnly FimTime { get; set; }

    public byte Type { get; set; }

    public short Valid { get; set; }
}
