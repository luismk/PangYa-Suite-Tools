using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPlayerBirthDayLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Login { get; set; }

    public DateOnly? SendDate { get; set; }
}
