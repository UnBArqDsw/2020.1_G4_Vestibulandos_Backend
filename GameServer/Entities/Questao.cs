using GameServer.Interfaces;
using GameServer.Patterns;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GameServer.Entities
{
    public class Questao : IQuestaoSubject
    {
        private List<IMonitorObserver> _monitorObservers = new List<IMonitorObserver>();

        public void Attach(IMonitorObserver observer)
        {
            Console.WriteLine("Subject: Attached an observer.");
            _monitorObservers.Add(observer);
        }

        public void Detach(IMonitorObserver observer)
        {
            _monitorObservers.Remove(observer);
            Console.WriteLine("Subject: Detached an observer.");
        }

        public void Notify()
        {
            Console.WriteLine("Subject: Notifying observers...");

            foreach (var observer in _monitorObservers)
            {
                observer.Update(this);
            }
        }
        public void SomeBusinessLogic()
        {
            Console.WriteLine("\nSubject: I'm doing something important.");

            Thread.Sleep(15);

            Console.WriteLine("Subject: My state has just changed to: " + new Random().Next(0, 10));
            Notify();
        }
    }
}
