using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public abstract class Usuario : IUsuario
    {
        public int Id { get; set; }
        public int TipoUsuario { get; set; }
        public string Login { get; set; }
        public string Senha { get; set; }
        public string Email { get; set; }
        public abstract void Hello();

        public bool DeletarUsuario(int id)
        {
            return true;
        }
    }
}
