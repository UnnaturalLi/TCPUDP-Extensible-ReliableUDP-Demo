using System.Net;
using System.Threading;

namespace NetworkBase
{
    public abstract class NetworkSessionBase
    {
        private bool m_IsClosed;
        public bool isClosed { get { return m_IsClosed; }
            set { m_IsClosed = value; }
        }
        protected IPEndPoint m_Addr;

        public bool Init(IPEndPoint ip)
        {
            m_Addr = ip;
            return OnInit();
        }
        
        public void Start()
        {
            OnStart();
            Logger.LogToTerminal($"started:{m_Addr.Address}:{m_Addr.Port}");
        }
        
        public void Stop()
        {
            if (isClosed == true)
            {
                return;
            }
            OnClose();
            Logger.LogToTerminal($"stopped:{m_Addr.Address}:{m_Addr.Port}");
            isClosed = true;
        }
        protected Thread CreateThread(ThreadStart threadFunc)
        {
            var nt = new Thread(threadFunc)
            {
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
            nt.Start();
            return nt;
        }
        protected virtual bool OnInit()
        {
            return true;
        }
        protected virtual void OnStart()
        {
        }
        protected virtual void OnClose()
        {
        }       
    }
}