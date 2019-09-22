using System;
using System.Globalization;
using System.Windows.Forms;


namespace Chat_Virtual___Servidor {
    public partial class GraphicInterface : Form {

        public static bool Connected;
        private readonly DataBaseConnection Oracle;
        private readonly SocketConnection Socket;
        private delegate void LogConsoleAppend(string text);

        public GraphicInterface(){
            this.InitializeComponent();
            this.Socket = new SocketConnection(this.LogConsole);
            this.Oracle = new DataBaseConnection(this.LogConsole);
            Connected = false;
            this.ConsoleAppend("Hola, Bienvenido al servidor de UNtalk\n");
        }

        private void ExitEvent(object sender, EventArgs e){
            this.ShutDown();
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
            if (Connected){
                this.ShutDown();
            }else{
                this.ShutUp();
            }
        }

        private void ShutUp(){
            try {
                this.Socket.ConnectSockets();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex+"/n");
                this.ConsoleAppend("Servidor no inicializado.");
                return;
            }
            try {
                this.Oracle.ConnectDataBase();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex+"\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.Socket.DisconnectSockets();
                return;
            }
            Connected = true;
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.Button.Text = "Apagar Servidor.";
        }

        private void ShutDown(){
            try {
                this.Socket.DisconnectSockets();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la conexión de los sockets: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            Connected = false;
            this.Button.Text = "Encender servidor.";
        }


        private void ListenConnection() {

        }

        private void SocketsToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void OracleToolStripMenuItem_Click(object sender, EventArgs e) {

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
