using System;
using System.Globalization;
using System.Windows.Forms;


namespace Chat_Virtual___Servidor {
    public partial class GraphicInterface : Form {

        private readonly DataBaseConnection Oracle;
        private ServerConnection Server;
        private readonly SocketConnection Socket;
        private delegate void LogConsoleAppend(string text);

        public GraphicInterface(){
            this.InitializeComponent();
            this.Server = new ServerConnection(this);
            this.ConsoleAppend("Hola, Bienvenido al servidor de SADIRI.\n");
        }

        private void ExitEvent(object sender, EventArgs e){
            Application.Exit();
        }

        private void InfoEvent(object sender, EventArgs e){
            MessageBox.Show("Aplicación creada por\n\n" +
                "Ricardo Andrés Calvo\n" +
                "Samuel González Nisperuza\n" +
                "Juan Diego Preciado\n");
            //TODO: Mejorar Cuadro Emergente
        }

        private void ButtonEvent(object sender, EventArgs e){
            /*if (this.Server.Connected) {
                this.Server.ShutDown();
            } else {
                this.Server.ShutUp();
            }*/
        }

        private void ConsoleAppend(string text) {
            if(this.LogConsole.InvokeRequired) {
                var d = new LogConsoleAppend(this.ConsoleAppend);
                this.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.LogConsole.AppendText("["+DateTime.Now.ToString(new CultureInfo("en-GB")) + "] "+ text + "\n");
            }
        }

    }
}
