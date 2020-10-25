using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Jogador : JogadorObserver, IUsuario
    {
         public void Hello()
        {
            Console.WriteLine("Usuário do tipo Jogador");
        }
    }
}
