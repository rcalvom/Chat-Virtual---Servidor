using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Chat_Virtual___Servidor{
    public class ServerConnection{


        private DataBaseConnection oracle;
        private ConnectionSettings settings;

        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        public bool Connected { get; set; }
        public TcpListener Server { get; set; }
        public GraphicInterface GraphicInterface { get; set; }
        public List<User> Users { get; set; }
        public TcpClient Client { get; set; }
        public IPEndPoint Ip { get; }

        [Serializable]
        private struct ConnectionSettings {
            public int port;
            public int maxUsers;
        }

        public ServerConnection(GraphicInterface GraphicInterface) {
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            this.oracle = new DataBaseConnection(this.GraphicInterface);
            if (File.Exists("SocketSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("SocketSettings.config", FileMode.Open, FileAccess.Read);
                this.settings = (ConnectionSettings)formatter.Deserialize(stream);
                stream.Close();
            } else {
                this.settings.port = 7777;
                this.settings.maxUsers = 30;
            }
            this.Ip = new IPEndPoint(IPAddress.Any, this.settings.port);
            this.Users = new List<User>();
        }

        public void ShutUp() {
            new Thread(this.Connect) {
                
            }.Start();
            //this.Connect();
        }

        public void Connect() {
            

            this.ButtonEnable(false);
            try {
                this.ConnectSockets();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex + "/n");
                this.ConsoleAppend("Servidor no inicializado.");
                this.ButtonEnable(true);
                return;
            }
            try {
                this.oracle.ConnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex + "\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.DisconnectSockets();
                this.ButtonEnable(true);
                return;
            }
            this.Connected = true;
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.ButtonText("Apagar Servidor");
            this.ButtonEnable(true);
        }

        private void DisconnectSockets() {
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        public void ConnectSockets() {
            
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.Ip);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            new Thread(this.ListenConnection) {
                IsBackground = true
            }.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            new Thread(this.ListenUsers) {
                IsBackground = true
            }.Start();
        }

        private void ListenUsers() {
            do {
                foreach(User u in this.Users) {
                    if(!(u.GetReader().ReadLine() is null)) {

                    }
                }
                //TODO: Queue. 
            } while(this.Connected);
        }

        private void ListenConnection() {
            do {
                if (this.Server.Pending()) {
                    this.Client = this.Server.AcceptTcpClient();
                    User user = new User();
                    user.SetStream(this.Client.GetStream());
                    user.SetWriter(new StreamWriter(this.Client.GetStream()));
                    user.SetReader(new StreamReader(this.Client.GetStream()));
                    string temp = user.GetReader().ReadLine();
                    user.SetName(user.GetReader().ReadLine());
                    string pass = user.GetReader().ReadLine();
                    switch (temp) {
                        case "InicioSesion":
                            bool exist = false;
                            this.oracle.GetOracleDataBase().ExecuteSQL("SELECT USUARIO,CONTRASENA FROM USUARIOS");
                            while (this.oracle.GetOracleDataBase().getDataReader().Read()) {
                                if (this.oracle.GetOracleDataBase().getDataReader()["USUARIO"].Equals(user.GetName()) && this.oracle.GetOracleDataBase().getDataReader()["CONTRASENA"].Equals(pass)) {                                    
                                    exist = true;
                                }
                            }
                            if (exist) {
                                user.GetWriter().WriteLine("SI");
                                user.GetWriter().Flush();
                                this.Users.Add(user);
                                this.ConsoleAppend("El usuario [" + user.GetName() + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                // TODO: Actualizar tabla.
                            } else {
                                user.GetWriter().WriteLine("NO");
                                user.GetWriter().Flush();
                                this.ConsoleAppend("Se ha intentado conectar el remoto ["+this.Client.Client.RemoteEndPoint.ToString() + "] con información de inicio de sesión incorrecta.");
                                this.Client.Close();
                            }
                            break;
                        case "Registro":
                            if(this.oracle.GetOracleDataBase().ExecuteSQL("INSERT INTO USUARIOS VALUES('" + user.GetName() + "','" + pass + "',DEFAULT)")) {
                                user.GetWriter().WriteLine("SI");
                                user.GetWriter().Flush();
                                this.ConsoleAppend("Se ha registrado el usuario [" +user.GetName()+" | "+ this.Client.Client.RemoteEndPoint.ToString() + "] correctamente.");
                                this.Users.Add(user);
                                this.ConsoleAppend("El usuario [" + user.GetName() + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                // TODO: Actualizar Tabla.
                            } else {
                                user.GetWriter().WriteLine("NO");
                                user.GetWriter().Flush();
                                this.ConsoleAppend("Se ha intentado registrar el remoto [" + this.Client.Client.RemoteEndPoint.ToString()+"] con un nombre de usuario ya existente.");                                
                                this.Client.Close();
                            }
                            break;
                        default:
                            break;
                    }                             
                }
                
            } while (this.Connected && this.Users.Count()<this.settings.maxUsers);
        }

        public void ShutDown() {
            try {
                this.DisconnectSockets();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la conexión de los sockets: \n " + ex);
            }
            try {
                this.oracle.DisconnectDataBase();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
        }

        private void ConsoleAppend(string text) {
            if (this.GraphicInterface.LogConsole.InvokeRequired) {
                var d = new DLogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        private void ButtonText(string text) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                var d = new DButtonText(this.ButtonText);
                this.GraphicInterface.Button.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.Button.Text = text;
            }
        }

        private void ButtonEnable(bool flag) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                var d = new DButtonEnable(this.ButtonEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.Button.Enabled = flag;
            }
        }

    }
}
