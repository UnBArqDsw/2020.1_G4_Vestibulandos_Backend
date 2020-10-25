using GameServer.Entities;
using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Patterns
{
    public class UsuarioFactory
    {
        public IUsuarioFactory GetUsuario(TipoUsuarioCode tipoUsuarioCode)
        {
            switch (tipoUsuarioCode)
            {
                case TipoUsuarioCode.Jogador: return new Jogador("login", "senha", "email", "apelido", 1);
                case TipoUsuarioCode.Monitor: return new Monitor("login", "senha", "email", 1);
                case TipoUsuarioCode.Administrador: return new Administrador("login", "senha", "email", 1);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
