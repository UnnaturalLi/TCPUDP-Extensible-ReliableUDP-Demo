using System.Net;
using System.Threading;
namespace NetworkBase
{
    public abstract class ClientBase
    {
        public NetworkSessionBase session;
        protected IPEndPoint m_Addr;
        public bool isStopped
        {
            get { return session.isClosed; }
            set { session.isClosed = value; }
        }

        public virtual bool Init(IPEndPoint ipEndPoint, params object[] args)
        {
            m_Addr = ipEndPoint;
            return OnInit(args);
        }
        public void Start()
        {
            OnStart();
        }
        public void Stop()
        {
            if (isStopped == true)
            {
                return;
            }
            OnStop();
            
            isStopped = true;
        }
        protected virtual bool OnInit(params object[] args)
        {
            
            return true;
        }
        protected virtual void OnStart()
        {
            session.Start();
        }
        protected virtual void OnStop()
        {
            session.Stop();
        }     
    }
}