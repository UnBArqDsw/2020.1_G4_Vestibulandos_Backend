﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Interfaces
{
    public interface IMonitorObserver
    {
        void Update(IQuestaoSubject subject);
    }
}
