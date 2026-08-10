using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class TypeList
{
    public long TypeId { get; set; }

    public string Name { get; set; }

    public string Icon { get; set; }

    public long Price { get; set; }

    public short Type { get; set; }
}
