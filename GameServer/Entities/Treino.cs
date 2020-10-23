using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Treino : Partida
    {
        public int Acerto { get; set; }
        public int Erro { get; set; }

        public void AtualizarAcerto(int acerto)
        {

        }

        public void AtualizarErro(int erro)
        {

        }

        public override void Hello()
        {
            Console.WriteLine("Partida treino");
        }
    }
}
