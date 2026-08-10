using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaMyroom
{
    public int Uid { get; set; }

    public string Senha { get; set; }

    public short PublicLock { get; set; }

    public short State { get; set; }
}
