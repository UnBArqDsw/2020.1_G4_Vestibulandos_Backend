﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer
{
    public enum TipoPartidaCode {
        Unknown = 0,
        Treino,
        Ranqueada
    }

    public enum TipoUsuarioCode
    {
        Unknown = 0,
        Jogador,
        Monitor,
        Administrador
    }
}
