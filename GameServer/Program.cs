using GameServer.Entities;
using GameServer.Factory;
using GameServer.Interfaces;
using GameServer.Patterns;
using System;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var partidaFactory = new PartidaFactory();
            var treino = partidaFactory.GetPartida(TipoPartidaCode.Treino);
            //treino.Hello();

            var ranqueada = partidaFactory.GetPartida(TipoPartidaCode.Ranqueada);
            //ranqueada.Hello();

            var usuarioFactory = new UsuarioFactory();
            var monitor = usuarioFactory.GetUsuario(TipoUsuarioCode.Monitor);
            var administrador = usuarioFactory.GetUsuario(TipoUsuarioCode.Administrador);
            var jogador = usuarioFactory.GetUsuario(TipoUsuarioCode.Jogador);

            monitor.Hello();
            administrador.Hello();
            jogador.Hello();

            //var player1 = new Jogador();
            //var player2 = new Jogador();

            //treino.Attach((IJogadorObserver)jogador);
            //treino.Attach((IJogadorObserver)jogador);

            //var treinoSubject = (PartidaSubject)treino;
            //treinoSubject.SomeBusinessLogic();

            ((PartidaSubject)treino).SomeBusinessLogic();
        }
    }
}
