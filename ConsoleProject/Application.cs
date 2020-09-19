using System;
using ConsoleProject.View;

namespace ConsoleProject {

    class Application {

        [STAThread]
        public static void Main(string[] arguments) {
            new Frontend().ShowForm(arguments);
        }
    }
}
