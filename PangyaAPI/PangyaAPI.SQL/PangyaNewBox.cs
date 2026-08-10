using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewBox
{
    public int Id { get; set; }

    public string Nome { get; set; }

    public int Typeid { get; set; }

    public int OpenedTypeid { get; set; }

    public int Numero { get; set; }

    /// <summary>
    /// 0 SEND ITEM TO MAIL, 1 SEND ITEM TO MY ROOM
    /// </summary>
    public byte TipoOpen { get; set; }

    /// <summary>
    /// 0 SEND ITEM TO MAIL, 1 SEND ITEM TO MY ROOM
    /// </summary>
    public byte Tipo { get; set; }

    public string Message { get; set; }

    public byte Active { get; set; }
}
