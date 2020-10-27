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

            var usuarioFactory = new UsuarioFactory();
            var monitor = usuarioFactory.GetUsuario(TipoUsuarioCode.Monitor);
            var administrador = usuarioFactory.GetUsuario(TipoUsuarioCode.Administrador);
            var jogadorUsuario = usuarioFactory.GetUsuario(TipoUsuarioCode.Jogador);
            var jogadorUsuario2 = usuarioFactory.GetUsuario(TipoUsuarioCode.Jogador);

            monitor.Hello();
            administrador.Hello();
            jogadorUsuario.Hello();

            Console.WriteLine();

            var jogador = (Jogador)jogadorUsuario;
            jogador.Id = 2;

            var jogador2 = (Jogador)jogadorUsuario2;

            ((Partida)treino).RegistrarJogador(jogador);
            ((Partida)treino).RegistrarJogador(jogador2);

            _ = jogador.Ação(jogador.Id);

            Console.WriteLine("\n");

            var questao = new Questao();
            questao.Attach((Monitor)monitor);
            questao.SomeBusinessLogic();
        }
    }
}
