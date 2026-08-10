using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewCourseDropItem
{
    public long Index { get; set; }

    public byte Course { get; set; }

    public byte Tipo { get; set; }

    public int Typeid { get; set; }

    public int Quantidade { get; set; }

    public int Probabilidade3h { get; set; }

    public int Probabilidade6h { get; set; }

    public int Probabilidade9h { get; set; }

    public int Probabilidade18h { get; set; }

    public byte Active { get; set; }
}
