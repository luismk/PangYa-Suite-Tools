
using System;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdVerifyNick : Pangya_DB
    {
        uint m_uid = 0;
        string m_nick = "";
        bool m_check = false;

        public CmdVerifyNick(string nick)
        {
            m_nick = nick;
            m_check = false;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {
                m_uid = uint.Parse(_result.data[0].ToString());
                m_check = m_uid > 0;

                if (!m_check)
                    throw new Exception("[CmdVerify::prepareConsulta][Error] Nick invalid");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            if (string.IsNullOrEmpty(m_nick))
                throw new Exception("[CmdVerify::prepareConsulta][Error] Nick invalid");

            m_check = false;
            m_uid = 0;

            var r = procedureWithParams("pangya.ProcVerifyNickname",
                new[] { "@NICKNAME" }, new[] { System.Data.SqlDbType.NVarChar }, new object[] { m_nick });

            checkResponse(r, "nao conseguiu verificar se existe o nick: " + m_nick);
            m_check = r.getResultSet().Count > 0;
            return r;
        }

        public bool getLastCheck() => m_check;

        public uint getUID() => m_uid;
    }
}
