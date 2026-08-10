using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewMemorialNormalItem
{
    public long Index { get; set; }

    public string Nome { get; set; }

    public int Typeid { get; set; }

    public int Qntd { get; set; }

    public byte Tipo { get; set; }

    public byte Active { get; set; }
}
