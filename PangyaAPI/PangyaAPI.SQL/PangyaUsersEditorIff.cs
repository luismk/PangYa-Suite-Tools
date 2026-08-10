using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaUsersEditorIff
{
    public int Uid { get; set; }

    public string Username { get; set; }

    public string PasswordHash { get; set; }

    public int Capability { get; set; }

    public int Tipo { get; set; }

    public int Time { get; set; }

    public string MacAdress { get; set; }

    public bool IsBlocked { get; set; }

    public string Hwid { get; set; }

    public DateTime? LastAcess { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
