using GameServer.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Interfaces
{
    public interface IPartidaMediator
    {
        void RegistrarJogador(Jogador jogador);

        void Notify();
    }
}
