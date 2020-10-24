using GameServer.Entities;
using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Factory
{
    public class PartidaFactory
    {
        public IPartidaFactory GetPartida(TipoPartidaCode tipoPartidaCode)
        {
            switch (tipoPartidaCode)
            {
                case TipoPartidaCode.Treino: return new Treino();
                case TipoPartidaCode.Ranqueada: return new Ranqueada();
                default:throw new ArgumentOutOfRangeException();
            }
        }
            
    }
}
