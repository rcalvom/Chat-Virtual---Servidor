using Chat_Virtual___Servidor.Frontend;
using System;
using System.Globalization;
using System.Windows.Forms;


namespace Chat_Virtual___Servidor {
    public partial class GraphicInterface : Form {

        private readonly ServerConnection Server;
        private delegate void DLogConsoleAppend(string text);

        public GraphicInterface(){
            this.InitializeComponent();
            this.Server = new ServerConnection(this);
            this.ConsoleAppend("Hola, Bienvenido al servidor de SADIRI.\n");
        }

        private void ExitEvent(object sender, EventArgs e){
            this.Server.ShutDown();
            Application.Exit();
        }

        private void ButtonEvent(object sender, EventArgs e){
            if (this.Server.Connected) {
                this.Server.ShutDown();
            } else {
                this.Server.ShutUp();
            }
        }

        private void ConsoleAppend(string text) {
            if(this.LogConsole.InvokeRequired) {
                DLogConsoleAppend d = new DLogConsoleAppend(this.ConsoleAppend);
                this.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.LogConsole.AppendText("["+DateTime.Now.ToString(new CultureInfo("en-GB")) + "] "+ text + "\n");
            }
        }

        private void ServerConfig_Click(object sender, EventArgs e) {
            ServerConfigInterface sci = new ServerConfigInterface();
            sci.ShowDialog();
            sci.Dispose();
        }

        private void OracleConfig_Click(object sender, EventArgs e) {
            OracleConfigInterface oci = new OracleConfigInterface();
            oci.ShowDialog();
            oci.Dispose();
        }

        private void InfoEvent(object sender, EventArgs e) {
            InformationInterface ii = new InformationInterface();
            ii.ShowDialog();
            ii.Dispose();
        }
    }
}
