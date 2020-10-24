using GameServer.Entities;
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
            var treino = partidaFactory.GetPartida(TipoPartidaCode.Treino);
            treino.Hello();

            var ranqueada = partidaFactory.GetPartida(TipoPartidaCode.Ranqueada);
            ranqueada.Hello();

            var player1 = new Jogador();
            var player2 = new Jogador();

            treino.Attach(player1);
            treino.Attach(player2);

            var treinoSubject = (PartidaSubject)treino;
            treinoSubject.SomeBusinessLogic();

            //((PartidaSubject)treino).SomeBusinessLogic();
        }
    }
}
