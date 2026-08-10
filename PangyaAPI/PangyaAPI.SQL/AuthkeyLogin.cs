using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class AuthkeyLogin
{
    public int Uid { get; set; }

    public string AuthKey { get; set; }

    public short Valid { get; set; }
}
