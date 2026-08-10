using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
namespace PangyaAPI.Network.Repository
{
    public class CmdRegisterServer : Pangya_DB
    {
        ServerInfoEx m_si;
        public CmdRegisterServer(ServerInfoEx _si)
        {
            m_si = _si;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

        }

        protected override Response prepareConsulta()
        {
            var r = procedure(
                "pangya.ProcRegServer_New",
                m_si.Id,
                m_si.Name,
                m_si.IpAddress,
                m_si.Port,
                (short)m_si.tipo,
                m_si.MaxUsers,
                m_si.ConnectedUsers,
                m_si.rate.pang,
                m_si.version,
                m_si.version_client,
                (uint)m_si.propriedade,
                m_si.Angelic_wings_num,
                (ushort)m_si.flagEvent,
                m_si.rate.exp,
                m_si.ImageNumber,
                m_si.rate.scratchy,
                m_si.rate.club_mastery,
                m_si.rate.treasure,
                m_si.rate.papel_shop_rare_item,
                m_si.rate.papel_shop_cookie_item,
                m_si.rate.chuva);

            checkResponse(r, "nao conseguiu registrar o server[GUID=" + (m_si.Id) + ", PORT=" + (m_si.Port) + ", NOME=" + (m_si.Name) + "] no banco de dados");
            return r;
        }

        public ServerInfoEx getServerList()
        {
            return this.m_si;
        }


        public void setInfo(ServerInfoEx _si)
        {
            m_si = _si;
        }
    }
}
