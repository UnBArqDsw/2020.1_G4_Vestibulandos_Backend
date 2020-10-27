using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public abstract class Partida : IPartidaMediator, IPartidaFactory
    {
        public int Id { get; set; }
        public TipoPartida TipoPartida { get; set; }
        public DateTime DataCriado { get; set; }
        public DateTime DataFinalizado { get; set; }
        public int Dificuldade { get; set; }
        public AreaConhecimento AreaConhecimento { get; set; }
        public int QuantidadeQuestao { get; set; }
        public bool Revisao { get; set; }

        private Dictionary<int, Jogador> _jogadores = new Dictionary<int, Jogador>();

        public abstract void Hello();
        public void RegistrarJogador(Jogador jogador)
        {
            Console.WriteLine("Partida: player registered.");
            if (!_jogadores.ContainsValue(jogador))
            {
                _jogadores[jogador.Id] = jogador;
            }

            jogador.Partida = this;
        }

        public void Notify()
        {
            Console.WriteLine("Mediator: Notifying all players...");

            foreach (var id in _jogadores.Keys)
            {
                _jogadores[id].ReactNotify();
            }
        }

    }
}
