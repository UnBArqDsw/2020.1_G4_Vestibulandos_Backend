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
            var jogadorUsuario = usuarioFactory.GetUsuario(TipoUsuarioCode.Jogador);
            var jogadorUsuario2 = usuarioFactory.GetUsuario(TipoUsuarioCode.Jogador);

            monitor.Hello();
            administrador.Hello();
            jogadorUsuario.Hello();

            //var player1 = new Jogador();
            //var player2 = new Jogador();

            Console.WriteLine();

            var jogador = (Jogador)jogadorUsuario;
            jogador.Id = 2;

            var jogador2 = (Jogador)jogadorUsuario2;

            ((Partida)treino).RegistrarJogador(jogador);
            ((Partida)treino).RegistrarJogador(jogador2);

            _ = jogador.Ação(jogador.Id);

            //treino.Attach((IJogadorObserver)jogador);
            //treino.Attach((IJogadorObserver)jogador);

            //var treinoSubject = (PartidaSubject)treino;
            //treinoSubject.SomeBusinessLogic();

            //((PartidaSubject)treino).SomeBusinessLogic();
        }
    }
}
