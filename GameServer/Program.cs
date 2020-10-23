using GameServer.Factory;
using GameServer.Interfaces;
using System;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var partidaFactory = new PartidaFactory();
            IPartidaFactory treino = partidaFactory.GetPartida(TipoPartidaCode.Treino);
            treino.Hello();

            IPartidaFactory ranqueada = partidaFactory.GetPartida(TipoPartidaCode.Ranqueada);
            ranqueada.Hello();
        }
    }
}
