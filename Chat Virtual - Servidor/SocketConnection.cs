using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public class SocketConnection {

        private RichTextBox LogConsole;
        private TcpListener Server;
        private readonly IPEndPoint IP;
        private TcpClient Client;
        private readonly List<User> Users;

        public SocketConnection(RichTextBox LogConsole) {
            this.LogConsole = LogConsole;
            this.IP = new IPEndPoint(IPAddress.Any,7777);
            this.Users = new List<User>();
        }

        public void ConnectSockets() {
            this.LogConsole.AppendText("Iniciando Conexión de los sockets\n");
            this.Server = new TcpListener(this.IP);
            this.Server.Start();
            this.LogConsole.AppendText("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");

            Thread t = new Thread(this.ListenConnection);
            t.Start();
        }

        private void ListenConnection() {
            while(GraphicInterface.Connected) { //TODO: revisar finalizacion de hilo
                try {
                    this.Client = this.Server.AcceptTcpClient();
                } catch(Exception ex) {

                }
                if(!(this.Client is null)) {
                    
                    User user = new User();
                    user.SetStream(this.Client.GetStream());
                    user.SetWriter(new StreamWriter(this.Client.GetStream()));
                    user.SetReader(new StreamReader(this.Client.GetStream()));
                    user.SetName("Prueba");
                    //TODO: comprobar contraseña correcta o nuevo usuario.
                    // La coleccion Users es la que se mostrará en la interfaz como usuarios activos.
                    this.Users.Add(user);
                    this.LogConsole.AppendText("El usuario "+user.GetName()+" Se ha conectado satisfactoriamente.");
                }
            }
        }

        //TODO: Falta Sistema de desconexión de un usuario.

        public void DisconnectSockets() {
            this.Server.Stop();
        }

        public void SetLogConsole(RichTextBox LogConsole) {
            this.LogConsole = LogConsole;
        }

    }
}
