using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Interfaces
{
    public interface IJogador
    {
        bool Ação(int id);
        void ReactNotify();
    }
}
