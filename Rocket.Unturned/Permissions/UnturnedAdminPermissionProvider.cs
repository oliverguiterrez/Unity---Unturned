﻿using Rocket.API.Permissions;
using Rocket.Core.Permissions;
using Rocket.Core.ServiceProxies;
using Rocket.Unturned.Player;

namespace Rocket.Unturned.Permissions
{
    [ServicePriority(Priority = ServicePriority.Lowest)]
    public class UnturnedAdminPermissionProvider : FullPermitPermissionProvider
    {
        public override bool SupportsTarget(IPermissionEntity target)
        {
            if (target is UnturnedUser user)
                return user.Player.IsAdmin;

            return false;
        }
    }
}