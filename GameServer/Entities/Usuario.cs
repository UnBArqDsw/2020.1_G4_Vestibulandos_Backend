using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public abstract class Usuario : IUsuarioFactory
    {
        public int Id { get; set; }
        public int TipoUsuario { get; set; }
        public string Login { get; set; }
        public string Senha { get; set; }
        public string Email { get; set; }
        public abstract void Hello();

        public Usuario(string login, string senha, string email, int tipoUsuario)
        {
            Login = login;
            Senha = senha;
            Email = email;
            TipoUsuario = tipoUsuario;
        }

        public bool DeletarUsuario(int id)
        {
            return true;
        }
    }
}
