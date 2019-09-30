using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using DataStructures;

namespace Chat_Virtual___Servidor{
    public class ServerConnection{


        private readonly DataBaseConnection Oracle;
        private ConnectionSettings settings;

        public bool Connected { get; set; }
        public TcpListener Server { get; set; }
        public GraphicInterface GraphicInterface { get; set; }
        public LinkedList<User> Users { get; set; }
        public Chain<Group> Groups { get; set; }
        public LinkedQueue<string> Messages { get; set; }
        public TcpClient Client { get; set; }
        public IPEndPoint Ip { get; }

        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        private delegate void DDataGridViewRow(string name, string ip);

        [Serializable]
        private struct ConnectionSettings {
            public int port;
            public int maxUsers;
        }

        public ServerConnection(GraphicInterface GraphicInterface) {
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            this.Oracle = new DataBaseConnection(this.GraphicInterface);
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
            this.Users = new LinkedList<User>();
            this.Groups = new Chain<Group>();
            this.Messages = new LinkedQueue<string>();
        }

        public void ShutUp() {
            new Thread(this.Connect) {
                IsBackground = true
            }.Start();
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
                this.Oracle.ConnectDataBase();
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
            Thread t1 = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t1.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            Thread t2 = new Thread(this.ListenUsers) {
                IsBackground = true
            };
            t2.Start();
            Thread t3 = new Thread(this.WriteUsers) {

            };
            t3.Start();
        }

        private void ListenUsers() {
            LinkedListNode<User> node = null;
            do {
                if (this.Users.Count == 0) {
                    continue;
                }else if (node == null) {
                    node = this.Users.First;
                }

                if (node.Value.GetStream().DataAvailable) {
                    this.Messages.Put(node.Value.GetName()+": "+node.Value.GetReader().ReadLine());
                    this.ConsoleAppend(this.Messages.GetFrontElement());
                    //TODO: POSIBLES SOLUCITUDES DIFERENTES A MENSAJES.
                }/*
                if (node.next == null) {
                    node = this.Users.GetNode(0);
                } else {
                    node = node.ext;
                }*/
                node = node.Next;
            } while (true);
        }

        private void WriteUsers() {
            LinkedListNode<User> node;
            string s;
            do {
                if (this.Messages.IsEmpty()) {
                    continue;
                } else {
                    node = this.Users.First;
                    s = this.Messages.GetFrontElement();
                    //remover el primero
                }
                do {
                    try {
                        node.Value.GetWriter().WriteLine(s);
                        node.Value.GetWriter().Flush();
                    } catch {
                        //TODO: Se ha desconectado.
                    }
                    node = node.Next;
                } while (node!=null);

            } while (true);
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
                            this.Oracle.GetOracleDataBase().ExecuteSQL("SELECT USUARIO,CONTRASENA FROM USUARIOS");
                            while (this.Oracle.GetOracleDataBase().getDataReader().Read()) {
                                if (this.Oracle.GetOracleDataBase().getDataReader()["USUARIO"].Equals(user.GetName()) && this.Oracle.GetOracleDataBase().getDataReader()["CONTRASENA"].Equals(pass)) {                                    
                                    exist = true;
                                    break;
                                    // TODO: Emplear Colas o alguna estructura de datos.
                                }
                            }
                            if (exist) {
                                user.GetWriter().WriteLine("SI");
                                user.GetWriter().Flush();
                                this.Users.AddLast(user);
                                this.ConsoleAppend("El usuario [" + user.GetName() + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(user.GetName(), this.Client.Client.RemoteEndPoint.ToString());
                            } else {
                                user.GetWriter().WriteLine("NO");
                                user.GetWriter().Flush();
                                this.ConsoleAppend("Se ha intentado conectar el remoto ["+this.Client.Client.RemoteEndPoint.ToString() + "] con información de inicio de sesión incorrecta.");
                                this.Client.Client.Close();
                                this.Client.Close();
                            }
                            break;
                        case "Registro":
                            if(this.Oracle.GetOracleDataBase().ExecuteSQL("INSERT INTO USUARIOS VALUES('" + user.GetName() + "','" + pass + "',DEFAULT)")) {
                                user.GetWriter().WriteLine("SI");
                                user.GetWriter().Flush();
                                this.ConsoleAppend("Se ha registrado el usuario [" +user.GetName()+" | "+ this.Client.Client.RemoteEndPoint.ToString() + "] correctamente.");
                                this.Users.AddLast(user);
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
                
            } while (/*this.Connected && this.Users.Count()<this.settings.maxUsers*/true);
        }

        public void ShutDown() {
            try {
                this.DisconnectSockets();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la conexión de los sockets: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
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

        private void InsertTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                var d = new DDataGridViewRow(this.InsertTable);
                this.GraphicInterface.Button.Invoke(d, new object[] { name, ip });
            } else {
                this.GraphicInterface.UsersTable.Rows.Add(name, ip);
            }
        }

    }
}
