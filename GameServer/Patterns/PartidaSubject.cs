using GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GameServer.Entities
{
    public class PartidaSubject : IPartidaSubject
    {
        public int State { get; set; }
        private List<IJogadorObserver> _jogadoresObservers = new List<IJogadorObserver>();

        public void Attach(IJogadorObserver observer)
        {
            Console.WriteLine("Subject: Attached an observer.");
            this._jogadoresObservers.Add(observer);
        }

        public void Detach(IJogadorObserver observer)
        {
            this._jogadoresObservers.Remove(observer);
            Console.WriteLine("Subject: Detached an observer.");
        }

        public void Notify()
        {
            Console.WriteLine("Subject: Notifying observers...");

            foreach (var observer in _jogadoresObservers)
            {
                observer.Update(this);
            }
        }

        public void SomeBusinessLogic()
        {
            Console.WriteLine("\nSubject: I'm doing something important.");
            this.State = new Random().Next(0, 10);

            Thread.Sleep(15);

            Console.WriteLine("Subject: My state has just changed to: " + this.State);
            this.Notify();
        }
    }
}
