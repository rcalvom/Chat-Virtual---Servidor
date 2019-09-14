using System;
using System.Windows.Forms;


namespace Chat_Virtual___Servidor {
    public partial class GraphicInterface : Form {

        public static bool Connected;
        private readonly DataBaseConnection Oracle;
        private readonly SocketConnection Socket;

        public GraphicInterface(){
            this.InitializeComponent();
            this.Socket = new SocketConnection(this.LogConsole);
            this.Oracle = new DataBaseConnection(this.LogConsole);
            Connected = false;
            this.LogConsole.AppendText("Hola, Bienvenido al servidor de UNtalk\n");
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
            Connected = true;
            this.Socket.ConnectSockets();
            this.Oracle.ConnectDataBase();
            this.Button.Text = "Apagar Servidor.";
            
        }

        private void ShutDown(){
            Connected = false;
            this.Socket.DisconnectSockets();
            this.Button.Text = "Encender servidor.";
            // TODO: Implementar desconexion a la base de datos y desconectar sockets.
        }


        private void ListenConnection() {

        }


    }
}
