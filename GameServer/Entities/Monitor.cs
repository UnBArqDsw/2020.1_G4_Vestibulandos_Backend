﻿using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class Monitor : Usuario, IMonitorObserver
    {
        public Monitor(string login, string senha, string email, int tipoUsuario) : base(login, senha, email, tipoUsuario)
        {

        }
        public override void Hello()
        {
            Console.WriteLine("Usuário do tipo Monitor");
        }

        public Questao CriarQuestao()
        {
            return new Questao();
        }

        public bool ValidarQuestao(Questao questao, bool aprovado)
        {
            return true;
        }

        public void Update(IQuestaoSubject subject)
        {
            Console.WriteLine("Monitor: Reacted to the event.");
        }
    }
}
