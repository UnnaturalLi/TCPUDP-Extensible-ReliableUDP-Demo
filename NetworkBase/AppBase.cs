using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NetworkBase;

namespace NetworkBase
{
    public abstract class AppBase
    {
        protected Thread m_RunThread;
        protected bool m_IsRunning;
        private int m_Interval;
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

        public void T_Run()
        {
            while (m_IsRunning)
            {
                OnRun();
                Thread.Sleep(m_Interval);
            }
            
        }
        public bool Init(int framRate, params object[] args)
        {
            m_Interval=(int)(1000f/framRate);
            return OnInit(args);
        }

        public  void Start()
        {
            OnStart();
            m_RunThread = CreateThread(T_Run);
            m_IsRunning = true;
        }

        public  void Stop()
        {
            m_IsRunning = false;
            OnStop();
        }

        public virtual void OnRun(){}
        public virtual bool OnInit(params object[] args)
        {
            return true;
        }

        public virtual void OnStart(){}
        public virtual void OnStop(){}
    }
}