using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Interfaces
{
    public interface IPartidaSubject
    {
        void Attach(IJogadorObserver observer);
        void Detach(IJogadorObserver observer);
        void Notify();
    }
}
