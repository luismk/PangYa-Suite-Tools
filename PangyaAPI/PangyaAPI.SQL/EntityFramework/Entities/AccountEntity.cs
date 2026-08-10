namespace PangyaAPI.SQL.EntityFramework.Entities
{
    public sealed class AccountEntity
    {
        public int Uid { get; set; }
        public short Logon { get; set; }
        public string GameServerId { get; set; }
    }
}
