using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaExceptionLog
{
    public int ExceptionId { get; set; }

    public int? Uid { get; set; }

    public string Username { get; set; }

    public string ExceptionMessage { get; set; }

    public string Server { get; set; }

    public DateTime? CreateDate { get; set; }
}
