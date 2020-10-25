using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Administrador : Monitor
    {
        public string Sobrenome { get; set; }
        public override void Hello()
        {
            //base.Hello();
            Console.WriteLine("Usuário do tipo Administrador");
        }
    }
}
