﻿using Bottles.Deployment;

namespace Bottles.Deployers.Topshelf
{
    public class TopshelfService : IDirective
    {
        public string InstallLocation { get; set; }

        //optional
        public string Username { get; set; }
        public string Password { get; set; }

        //optional
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
}