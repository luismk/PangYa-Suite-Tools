using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPointEventItem
{
    public string Name { get; set; }

    public int Typeid { get; set; }

    public string Icon { get; set; }

    public int? Price { get; set; }

    public short? IffType { get; set; }

    public int? CharType { get; set; }

    public int? Actived { get; set; }
}
