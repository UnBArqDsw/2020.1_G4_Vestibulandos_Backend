using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Monitor : Usuario
    {
        public string Nome { get; set; }
        public override void Hello()
        {
            Console.WriteLine("Usuário do tipo Monitor");
        }
    }
}
