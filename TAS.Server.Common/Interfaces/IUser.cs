﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Server.Common.Interfaces
{
    public interface IUser: ISecurityObject, IIdentity, IPersistent
    {
        IReadOnlyCollection<IGroup> Groups { get; }
        void GroupAdd(IGroup group);
        bool GroupRemove(IGroup group);
        bool IsAdmin { get; set; }
    }
}
