using System;
using ConsoleProject.View;

namespace ConsoleProject {

    class Application {

        [STAThread]
        public static void Main(string[] fileList) {
            //TODO: validate the input arguments
            new Frontend().ShowForm(fileList);
        }
    }
}
