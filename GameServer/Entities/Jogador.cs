﻿using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GameServer.Entities
{
    public class Jogador : Usuario, IJogadorObserver
    {
        public string Apelido { get; set; }
        public int Nivel { get; set; } = 0;
        public int Experiencia { get; set; } = 0;
        public int Vitoria { get; set; } = 0;
        public int Derrota { get; set; } = 0;
        public int AcertoQuestao { get; set; } = 0;
        public int ErroQuestao { get; set; } = 0;

        public Jogador(string login, string senha, string email, string apelido, int tipoUsuario) : base(login, senha, email, tipoUsuario)
        {
            Apelido = apelido;
        }

        public void AtualizarNivel()
        {
            Nivel++;
        }

        public void AtualizarExperiencia(int experiencia)
        {
            Experiencia += experiencia;
        }

        public void AtualizarVitoria()
        {
            Vitoria++;
        }

        public void AtualizarDerrota()
        {
            Derrota++;
        }

        public void AtualizarAcertoQuestao()
        {
            AcertoQuestao++;
        }
        public void AtualizarErroQuestao()
        {
            ErroQuestao++;
        }

        public int ObterTotalPartidas()
        {
            return AcertoQuestao + ErroQuestao;
        }

        public int ObterClassificacao()
        {
            return 1;
        }

        public override void Hello()
        {
            Console.WriteLine("Usuário do tipo Jogador");
        }

        public void Update(IPartidaSubject subject)
        {
            Console.WriteLine("Player: Reacted to the event.");
        }
    }
}
