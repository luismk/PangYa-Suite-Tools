using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class AuthkeyGame
{
    public int Uid { get; set; }

    public string AuthKey { get; set; }

    public int ServerId { get; set; }

    public short Valid { get; set; }
}
