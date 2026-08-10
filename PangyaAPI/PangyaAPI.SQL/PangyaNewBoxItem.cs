using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewBoxItem
{
    public long Index { get; set; }

    public int BoxId { get; set; }

    public string Nome { get; set; }

    public int Typeid { get; set; }

    public int Numero { get; set; }

    public int? Probabilidade { get; set; }

    public int Qntd { get; set; }

    /// <summary>
    /// 0 NORMAL, 1 RARE, 2 SUPER RARE
    /// </summary>
    public byte Raridade { get; set; }

    public byte Duplicar { get; set; }

    public byte Active { get; set; }
}
