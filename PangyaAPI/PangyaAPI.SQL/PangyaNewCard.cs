using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewCard
{
    public int Index { get; set; }

    public string Name { get; set; }

    public int Typeid { get; set; }

    public int Probabilidade { get; set; }

    public byte Tipo { get; set; }

    public int Pack { get; set; }
}
