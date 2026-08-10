using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGachaItem
{
    public int Index { get; set; }

    public int GachaNum { get; set; }

    public string Nome { get; set; }

    public int Typeid { get; set; }

    public int Qntd { get; set; }

    public int Probabilidade { get; set; }

    public short Tipo { get; set; }

    public short Premio { get; set; }

    public short Secret { get; set; }
}
