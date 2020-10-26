using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Administrador : Monitor
    {
        public Administrador(string login, string senha, string email, int tipoUsuario) : base(login, senha, email, tipoUsuario)
        {

        }

        public override void Hello()
        {
            //base.Hello();
            Console.WriteLine("Usuário do tipo Administrador");
        }

        public void TrocaTipoUsuario(Usuario usuario)
        {

        }

    }
}
