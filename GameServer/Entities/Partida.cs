using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public abstract class Partida : IPartidaFactory
    {
        public int Id { get; set; }
        public TipoPartida TipoPartida { get; set; }
        public DateTime DataCriado { get; set; }
        public DateTime DataFinalizado { get; set; }
        public int JogadorCriado { get; set; }
        public int Dificuldade { get; set; }
        public AreaConhecimento AreaConhecimento { get; set; }
        public int QuantidadeQuestao { get; set; }
        public bool Revisao { get; set; }
        public abstract void Hello();
    }
}
