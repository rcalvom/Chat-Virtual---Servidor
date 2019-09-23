using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public class SocketConnection {

        private static RichTextBox LogConsole;
        private OracleDataBase Oracle;
        private TcpListener Server;
        private readonly IPEndPoint IP;
        private TcpClient Client;
        private ConnectionSettings Settings;
        private readonly List<User> Users;
        private delegate void LogConsoleAppend(string text);

        [Serializable]
        private struct ConnectionSettings {
            public int Port;
            public int MaxUsers;
        }

        public SocketConnection(RichTextBox LogConsole, GraphicInterface GI) {
            SocketConnection.LogConsole = LogConsole;

            if(File.Exists("SocketSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("SocketSettings.config", FileMode.Open, FileAccess.Read);
                this.Settings = (ConnectionSettings)formatter.Deserialize(stream);
                stream.Close();
            } else {
                this.Settings.Port = 7777;
                this.Settings.MaxUsers = 30;
            }
            this.IP = new IPEndPoint(IPAddress.Any, this.Settings.Port);
            this.Users = new List<User>();
        }

        public void ConnectSockets() {
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.IP);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            Thread t = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t.Start();
        }

        private void ListenConnection() {
            do {
                try {
                    this.Client = this.Server.AcceptTcpClient();
                } catch(Exception) {}
                if(!(this.Client is null)) {
                    User user = new User();
                    user.SetStream(this.Client.GetStream());
                    user.SetWriter(new StreamWriter(this.Client.GetStream()));
                    user.SetReader(new StreamReader(this.Client.GetStream()));

                    string temp = user.GetReader().ReadLine();
                    if (temp.Equals("InicioSesion")){
                        user.SetName(user.GetReader().ReadLine());
                        this.Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + user.GetName() + "','123')");
                    }
                    else if (temp.Equals("Registro")){
                        Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + user.GetName() + "','123')");
                    }
                    //TODO: comprobar contraseña correcta o nuevo usuario.
                    
                    // La coleccion Users es la que se mostrará en la interfaz como usuarios activos.
                    this.Users.Add(user);
                    this.ConsoleAppend("El usuario [" + user.GetName() + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                }
            } while(this.Users.Count<=this.Settings.MaxUsers);
        }

        public void DisconnectSockets() {
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        public void SetLogConsole(RichTextBox LogConsole) {
            SocketConnection.LogConsole = LogConsole;
        }

        private void ConsoleAppend(string text) {
            if(LogConsole.InvokeRequired) {
                var d = new LogConsoleAppend(this.ConsoleAppend);
                LogConsole.Invoke(d, new object[] { text });
            } else {
                LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        //TODO: Revisar si la consola se puede trabajar desde un contexto estatico para ahorrar lineas de código.
        //TODO: Revisar como actualizar la lista de clientes de la tabla de la interfaz.
        //TODO: Agregar ventanas que guarden ajustes nuevos de sockets y BBDD.
        //TODO: Persistencia de los datos de configuración.


    }
}
