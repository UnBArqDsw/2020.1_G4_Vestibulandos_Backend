using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Entities
{
    public class JogadorObserver : IJogadorObserver
    {
        public void Update(IPartidaSubject subject)
        {
            Console.WriteLine("Player: Reacted to the event.");
        }
    }
}
