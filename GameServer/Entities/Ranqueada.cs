using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Ranqueada : Partida
    {
        public int[] IdJogadores { get; set; }
        public int[] AcertoJogador { get; set; }
        public int[] ErroJogador { get; set; }

        public bool InserirJogadores(int[] idJogadores)
        {
            return true;
        }

        public bool AtualizarAcerto(int[] idJogadores)
        {
            return true;
        }

        public bool AtualizarErro(int[] idJogadores)
        {
            return true;
        }

        public override void Hello()
        {
            Console.WriteLine("Partida ranqueada");
        }
    }
}
