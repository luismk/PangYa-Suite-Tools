using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaItemTypelist
{
    public int Typeid { get; set; }

    public string Name { get; set; }

    public string Icon { get; set; }

    public int? Price { get; set; }

    public short Iscash { get; set; }

    public short? IffType { get; set; }

    public int? Type { get; set; }

    public int? Com0 { get; set; }

    public int? Com1 { get; set; }

    public int? Com2 { get; set; }

    public int? Com3 { get; set; }

    public int? Com4 { get; set; }

    public string CharSerialno { get; set; }

    public string Desc { get; set; }

    public string Tname { get; set; }

    public short? IsSalable { get; set; }

    public int? CharId { get; set; }

    public string NameItem { get; set; }
}
