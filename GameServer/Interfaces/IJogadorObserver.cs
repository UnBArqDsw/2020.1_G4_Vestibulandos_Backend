using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Interfaces
{
    public interface IJogadorObserver
    {
        void Update(IPartidaSubject subject);
    }
}
