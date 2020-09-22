using System;
using ConsoleProject.View;
using log4net;

namespace ConsoleProject {

    class Application {

        private static readonly ILog LOG = LogManager.GetLogger(typeof(Application));

        [STAThread]
        public static void Main(string[] args) {
            LOG.Info("Starting Application...");

            //TODO: validate the input arguments
            new Frontend().ShowForm(args);
        }
    }
}
