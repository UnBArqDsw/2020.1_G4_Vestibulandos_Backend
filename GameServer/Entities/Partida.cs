using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public abstract class Partida : IPartidaFactory
    {
        public abstract void Hello();
    }
}
