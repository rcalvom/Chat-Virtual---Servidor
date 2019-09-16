using System;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public class MainClass {

        [STAThread]
        public static void Main(){
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphicInterface());
        }
    }
}
