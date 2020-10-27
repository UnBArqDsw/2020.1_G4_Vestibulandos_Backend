using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Interfaces
{
    public interface IQuestaoSubject
    {
        void Attach(IMonitorObserver observer);
        void Detach(IMonitorObserver observer);
        void Notify();

    }
}
