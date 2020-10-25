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
                case TipoUsuarioCode.Jogador: return new Jogador();
                case TipoUsuarioCode.Monitor: return new Monitor();
                case TipoUsuarioCode.Administrador: return new Administrador();
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
