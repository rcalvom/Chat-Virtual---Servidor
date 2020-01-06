using DataStructures;
using ShippingData;
using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {

    /// <summary>
    /// Esta Clase se encarga de todas las conexiones del servidor.
    /// </summary>
    public class ServerConnection {
        public bool Connected { get; set; }                                         // Indica si el servidor esta encendido o no.
        private TcpListener Server;                                                 // Permite la escucha de conexiones entrantesde red TCP.
        private readonly GraphicInterface GraphicInterface;                         // Referencia al conexto gráfico.
        private readonly LinkedList<User> Users;                                    // Colección que contiene los usuarios conectados.
        private readonly IPEndPoint Ip;
        private readonly DataBaseConnection Oracle;

        //Métodos delegados, permiten que se pueda acceder a la interfaz desde cualquier hilo.
        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        private delegate void DDataGridViewPush(string name, string ip);
        private delegate void DDataGridViewPop(string name);
        private delegate void DMenuEnable(bool flag);
        private delegate void DAlterTable(string name, string ip);
        private delegate void DClearTable();

        /// <summary>
        /// Constructor. Inicializa la instancia para poder ser posteriormente utilizada como servidor.
        /// </summary>
        /// <param name="GraphicInterface">Referencia al a interfaz gráfica.</param>
        public ServerConnection(GraphicInterface GraphicInterface) {
            OracleConfigInterface.InitSettings();
            ServerConfigInterface.InitSettings();
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            this.Oracle = new DataBaseConnection(this.GraphicInterface);
            this.Ip = new IPEndPoint(IPAddress.Any, ServerConfigInterface.Settings.port);
            this.Users = new LinkedList<User>();
        }

        /// <summary>
        /// Enciende el servidor, permite que se escuchen solicitudes de conexión entrantes.
        /// </summary>
        public void ShutUp() {
            new Thread(this.Connect) {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Apaga el servidor, elimina la conexión de la base de datos y finaliza la escucha de nuevas solicitudes.
        /// </summary>
        public void ShutDown() {
            try {
                this.DisconnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido interrumpir escucha de nuevas conexiones: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
            this.MenuEnable(true);
            this.ClearTable();
            while (!this.Users.IsEmpty()) {
                this.Users.Remove(0);
            }
        }

        /// <summary>
        /// Método de un hilo. Inicializa la escucha de solicitudes y la base de datos.
        /// </summary>
        private void Connect() {
            this.MenuEnable(false);
            this.ButtonEnable(false);
            this.Connected = true;
            try {
                this.ConnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex + "/n");
                this.ConsoleAppend("Servidor no inicializado.");
                this.ButtonEnable(true);
                this.MenuEnable(true);
                this.Connected = false;
                return;
            }
            try {
                this.Oracle.ConnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex + "\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.DisconnectListener();
                this.ButtonEnable(true);
                this.MenuEnable(true);
                this.Connected = false;
                return;
            }
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.ButtonText("Apagar Servidor");
            this.ButtonEnable(true);

            // TODO: CARGAR GRUPOS ACTIVOS

        }

        /// <summary>
        /// Detiene la escucha de solicitudes de conexión.
        /// </summary>
        private void DisconnectListener() {
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        /// <summary>
        /// Permite que las solicitudes de conexión sean escuchadas.
        /// </summary>
        private void ConnectListener() {
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.Ip);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            Thread t1 = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t1.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            Thread t2 = new Thread(this.ExecuteRequest) {
                IsBackground = true
            };
            t2.Start();
            Thread t3 = new Thread(this.ReadUsers) {
                IsBackground = true
            };
            t3.Start();
            Thread t4 = new Thread(this.WriteUsers) {
                IsBackground = true
            };
            t4.Start();
        }

        /// <summary>
        /// Método de Hilo. Redirige los mensaje y responde a las peticiones de los usuarios
        /// </summary>
        private void ExecuteRequest() {
            do {
                Iterator<User> i = this.Users.Iterator();
                while (i.HasNext()) {
                    User user = i.Next();
                    object Readed = user.ReadingDequeue();
                    if (Readed == null) {
                        continue;
                    } else if (Readed is Chat ch) {                                                             // Si se desea obtener la información de un Chat privado.
                        if (ch.Searched) {
                            this.Oracle.Oracle.ExecuteSQL(
                                "SELECT USUARIO, ESTADO, RUTA_FOTO " +
                                "FROM USUARIOS " +
                                "WHERE LOWER(USUARIO) LIKE '%" + ch.memberTwo.Name.ToLower() + "%'");
                            while (this.Oracle.Oracle.DataReader.Read()) {
                                Profile profile = new Profile {
                                    Name = this.Oracle.Oracle.DataReader["USUARIO"].ToString(),
                                    Status = this.Oracle.Oracle.DataReader["ESTADO"].ToString()
                                };
                                using (FileStream stream = File.Open(this.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                                    profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                    stream.Close();
                                }
                                user.WritingEnqueue(new Chat(ch.memberOne, profile, true));
                            }
                        } else {
                            this.Oracle.Oracle.ExecuteSQL("SELECT USUARIO, ESTADO, RUTA_FOTO " +
                                "FROM USUARIOS " +
                                "WHERE USUARIO = '" + ch.memberTwo.Name + "'");
                            if (this.Oracle.Oracle.DataReader.Read()) {
                                Profile profile = new Profile {
                                    Name = this.Oracle.Oracle.DataReader["USUARIO"].ToString(),
                                    Status = this.Oracle.Oracle.DataReader["ESTADO"].ToString()
                                };
                                using (FileStream stream = File.Open(this.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                                    profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                    stream.Close();
                                }
                                user.WritingEnqueue(new Chat(ch.memberOne, profile, false));
                            }
                        }
                    } else if (Readed is ChatMessage ms) {                                                      // Si se envía un mensaje en privado.
                        ms.date = new ShippingData.Message.Date(DateTime.Now);
                        if (ms.Image == null) {
                            this.Oracle.Oracle.ExecuteSQL("INSERT INTO MENSAJES_CHAT VALUES (DEFAULT, '" + ms.Sender + "', '" + ms.Receiver
                            + "', DEFAULT, '" + ms.Content + "', NULL)");
                        } else {
                            this.Oracle.Oracle.ExecuteSQL("INSERT INTO MENSAJES_CHAT VALUES (DEFAULT, '" + ms.Sender + "', '" + ms.Receiver
                            + "', DEFAULT, '" + ms.Content + "', 'F:\\SADIRI\\MensajesChat\\')");
                            Image picture = Serializer.DeserializeImage(ms.Image);

                            this.Oracle.Oracle.ExecuteSQL("SELECT MAX(ID_MENSAJE) AS MAX FROM MENSAJES_CHAT");
                            this.Oracle.Oracle.DataReader.Read();

                            string path = "F:\\SADIRI\\MensajesChat\\"+this.Oracle.Oracle.DataReader["MAX"].ToString() +".png";
                            using (FileStream stream = File.Open(path, FileMode.Create)) {
                                picture.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                stream.Close();
                            }
                        }
                        User theOther;
                        if (user.Name.Equals(ms.Sender)) {
                            theOther = this.SearchUser(ms.Receiver);
                        } else {
                            theOther = this.SearchUser(ms.Sender);
                        }
                        user.WritingEnqueue(ms);
                        theOther.WritingEnqueue(ms);
                        this.ConsoleAppend("Mensaje  [" + ms.Sender + "] a [" + ms.Receiver + "]: " + ms.Content);
                    } else if (Readed is ChatGroup group) {                                                     // Si se desea obtener la información de un grupo.

                    } else if (Readed is GroupMessage groupMessage) {                                           // Si se envía un mensaje en un grupo.
                        //Carpeta: F:\\SADIRI\\MensajesGrupos\\
                    } else if (Readed is Profile profile) {                                                     // Si es un cambio de perfil.
                        if (profile.Status != null) {
                            this.Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET ESTADO = '" + profile.Status + "' WHERE USUARIO = '" + profile.Name + "'");
                            user.WritingEnqueue(new RequestAnswer(true));
                        }
                        if (profile.Image != null) {
                            Image foto = Serializer.DeserializeImage(profile.Image);
                            string path = "F:\\SADIRI\\Usuarios\\" + profile.Name + ".png";
                            using (FileStream stream = File.Open(path, FileMode.Create)) {
                                foto.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                stream.Close();
                            }
                            this.Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET RUTA_FOTO = 'F:\\SADIRI\\Usuarios\\" + profile.Name + ".png' WHERE USUARIO = '" + profile.Name + "'");
                            user.WritingEnqueue(new RequestAnswer(true)); //TODO: Código respuesta.
                        }
                        this.ConsoleAppend("Se ha cambiado satisfactoriamente el perfil del usuario  [" + user.Name + " | " + IPAddress.Parse(((IPEndPoint)user.Client.Client.RemoteEndPoint).Address.ToString()) + "]. ");
                    } else if (Readed is ChangePassword changePassword) {                                       // Si es una solicitud de cambio de contraseña.
                        this.Oracle.Oracle.ExecuteSQL("SELECT CONTRASEÑA FROM USUARIOS WHERE USUARIO = '" + changePassword.UserName + "'");
                        this.Oracle.Oracle.DataReader.Read();
                        string currentPassword = this.Oracle.Oracle.DataReader["CONTRASEÑA"].ToString();
                        if (!changePassword.CurrentPassword.Equals(currentPassword)) {
                            user.WritingEnqueue(new RequestAnswer(false, 3));
                            user.WritingEnqueue(new RequestError(3));
                            this.ConsoleAppend("El cambio contraseña del usuario  [" + user.Name + " | " + IPAddress.Parse(((IPEndPoint)user.Client.Client.RemoteEndPoint).Address.ToString()) + "]. no pudo ser realizado.");
                        } else {
                            this.Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET CONTRASEÑA = '" + changePassword.NewPassword + "' WHERE USUARIO = '" + changePassword.UserName + "'");
                            this.ConsoleAppend("Se ha cambiado satisfactoriamente la contraseña del usuario  [" + user.Name + " | " + user.Client.Client.RemoteEndPoint.ToString() + "]. ");
                            user.WritingEnqueue(new RequestAnswer(true, 3));
                        }
                    } else if (Readed is TreeActivities tree) {                                                 // Si es una actualización del arbol de tareas.
                        string path = "F:\\SADIRI\\ArbolesTareas\\" + user.Name + ".dat";
                        IFormatter formatter = new BinaryFormatter();
                        using (FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
                            formatter.Serialize(stream, tree.Node);
                            stream.Close();
                        }
                        this.Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET RUTA_ARBOL = '" + path + "' WHERE USUARIO = '" + user.Name + "'");
                        this.ConsoleAppend("Se ha guardado satisfactoriamente el árbol de tareas del usuario  [" + user.Name + " | " + IPAddress.Parse(((IPEndPoint)user.Client.Client.RemoteEndPoint).Address.ToString()) + "]. ");
                    }
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Devuelve el primer Usuario encontrado con el nombre correspondiente.
        /// </summary>
        private User SearchUser(string userName) {
            Iterator<User> i = this.Users.Iterator();
            while (i.HasNext()) {
                User u = i.Next();
                if (u.Name.Equals(userName)) {
                    return u;
                }
            }
            return null;
        }

        /// <summary>
        /// Método de hilo. Lee cada usuario y revisa si le ha enviado datos al servidor.
        /// </summary>
        private void ReadUsers() {
            do {
                Iterator<User> i = this.Users.Iterator();
                while (i.HasNext()) {
                    User user = i.Next();
                    this.Read(user);                        // Intenta leer los datos que estén pendientes para ese usuario .
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Método de hilo. Escribe los datos listos en los clientes.
        /// </summary>
        private void WriteUsers() {
            do {
                Iterator<User> i = this.Users.Iterator();   // Obtiene el iterador para la lista.
                while (i.HasNext()) {
                    User user = i.Next();                   // Obtiene cada elemento en la lista.
                    this.Write(user);                       // Intenta escribir los datos que estén pendientes para ese usuario.
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Esta a la escucha de nuevos clientes y verifica su información de inicio de sesión.
        /// </summary>
        private void ListenConnection() {
            do {
                try {
                    if (this.Server.Pending()) {                                            // Si hay solicitudes de conexión entrantes.
                        User U = new User(this.Server.AcceptTcpClient());
                        object obj = null;

                        for (int i = 0; i < 25; i++) {                                      // Intenta 25 veces recibir el objeto inicial.
                            try {
                                this.Read(U);
                                obj = U.ReadingDequeue();
                            } catch (Exception) { }
                            if (obj == null) {
                                Thread.Sleep(125);
                            } else {
                                break;
                            }
                        }

                        if (obj is SignIn si) {                                             // Si el objeto recibido es un inicio de sesión.
                            bool exist = false;
                            this.Oracle.Oracle.ExecuteSQL("SELECT * FROM INFOMRACION_INICIO");
                            while (this.Oracle.Oracle.DataReader.Read()) {
                                if (this.Oracle.Oracle.DataReader["USUARIO"].Equals(si.user) && this.Oracle.Oracle.DataReader["CONTRASEÑA"].Equals(si.password)) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {                                                    // Si la información del cliente corresponde con la de la base de datos.
                                U.Name = si.user;
                                U.WritingEnqueue(new RequestAnswer(true));
                                this.Write(U);
                                this.Users.Add(U);
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                                Profile profile = new Profile();
                                this.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                this.Oracle.Oracle.DataReader.Read();
                                string path = this.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                                this.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                this.Oracle.Oracle.DataReader.Read();
                                string status = this.Oracle.Oracle.DataReader["ESTADO"].ToString();
                                using (FileStream stream = File.Open(path, FileMode.Open)) {
                                    profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                }
                                profile.Status = status;
                                profile.Name = U.Name;
                                U.WritingEnqueue(profile);

                                TreeActivities tree = new TreeActivities();
                                this.Oracle.Oracle.ExecuteSQL("SELECT RUTA_ARBOL FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                this.Oracle.Oracle.DataReader.Read();
                                string treePath = this.Oracle.Oracle.DataReader["RUTA_ARBOL"].ToString();
                                if (treePath != "") {
                                    IFormatter formatter = new BinaryFormatter();
                                    using (FileStream stream = File.Open(treePath, FileMode.Open, FileAccess.Read)) {
                                        tree.Node = (TreeNode[])formatter.Deserialize(stream);
                                        stream.Close();
                                    }
                                    U.WritingEnqueue(tree);
                                }

                            } else {                                                                                        // Si la infomación de inicio de sesión es incorrecta.
                                U.WritingEnqueue(new RequestAnswer(false));
                                U.WritingEnqueue(new RequestError(1));
                                this.Write(U);
                                this.ConsoleAppend("Se ha intentado conectar el remoto [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] con información de inicio de sesión incorrecta.");
                                U.Client.Close();
                            }
                        } else if (obj is SignUp su) {                                                                      // Si el objeto recibido es un nuevo registro
                            if (this.Oracle.Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.userName + "', '" + su.name + "', '" + su.password + "', 'Hey there! I am using SADIRI.','F:\\SADIRI\\Usuarios\\default.png', null, default)")) {
                                U.Name = su.userName;
                                U.WritingEnqueue(new RequestAnswer(true));
                                this.Write(U);
                                this.ConsoleAppend("Se ha registrado el usuario [" + U.Name + " | " + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] correctamente.");
                                this.Users.Add(U);
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                                Profile profile = new Profile();
                                this.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                this.Oracle.Oracle.DataReader.Read();
                                string path = this.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                                this.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                this.Oracle.Oracle.DataReader.Read();
                                string status = this.Oracle.Oracle.DataReader["ESTADO"].ToString();
                                using (FileStream stream = File.Open(path, FileMode.Open)) {
                                    profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                    stream.Close();
                                }
                                profile.Status = status;
                                profile.Name = U.Name;
                                U.WritingEnqueue(profile);

                            } else {
                                U.WritingEnqueue(new RequestAnswer(false));
                                U.WritingEnqueue(new RequestError(0));
                                this.Write(U);
                                this.ConsoleAppend("Se ha intentado registrar el remoto [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] con un nombre de usuario ya existente.");
                                U.Client.Close();
                            }
                        } else if (obj is null) {                                                 // Si no llego objeto inicial.
                            this.ConsoleAppend("No se recibió informmación de ingreso por parte del remoto. [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] Se ha desconectado del servidor.");
                            U.Client.Close();
                        } else {                                                                  // Si el objeto inicial no es un tipo de dato reconocido.
                            this.ConsoleAppend("No se reconoce la información de ingreso por parte del remoto. [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] Se ha desconectado del servidor.");
                            U.Client.Close();
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Escribe los datos que esten pendientes en la cola WritingQueue de un ususario.
        /// </summary>
        /// <param name="user">El ususario del que se van a intentar escribir los datos.</param>
        /// <returns>Verdadero si los datos fueron enviados, falso si almenos uno falló.</returns>
        private bool Write(User user) {
            Data data = user.WritingDequeue();
            if (data == default) {
                return false;
            } else {
                try {
                    byte[] toSend = Serializer.Serialize(data);                                      // Serializa el primer objeto de la cola.
                    user.Writer.Write(toSend.Length);                                                // Envía el tamaño del objeto.
                    user.Writer.Write(toSend);                                                       // Envía el objeto.     
                    return true;
                } catch (Exception ex) {
                    //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                    //this.ConsoleAppend(ex.Message);
                    user.WritingEnqueue(data);
                    return false;
                }
            }
        }

        /// <summary>
        /// Lee los datos que estén pendientes para un usuario y los guarda en la cola ReadingQueue del mismo.
        /// </summary>
        /// <param name="user">EL ususario del que se van a intentar leer datos.</param>
        /// <returns>Verdadero si le leyeron todos los datos, falso si uno de ellos no pudo ser leido</returns>
        private bool Read(User user) {
            try {
                if (user.Stream.DataAvailable) {                                            // Verifica si hay datos por leer.
                    int size = user.Reader.ReadInt32();                                     // Lee el tamaño del objeto.
                    byte[] data = new byte[size];                                           // Crea el arreglo de bytes para el objeto.
                    data = user.Reader.ReadBytes(size);                                     // Lee el el objeto y lo guarda en el arreglo de bytes.
                    object a = Serializer.Deserialize(data);                                // Deserializa el objeto.
                    user.ReadingEnqueue((Data)a);                                           // Guarda el objeto en la cola de lectura.
                }
                return true;
            } catch (Exception) {
                //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                return false;
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el texto a la interfaz gráfica.
        /// </summary>
        /// <param name="text">Texto a agregar a la consola.</param>
        private void ConsoleAppend(string text) {
            if (this.GraphicInterface.LogConsole.InvokeRequired) {
                DLogConsoleAppend d = new DLogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        /// <summary>
        /// Metodo para hilo. Cambia el texto del botón de la interfaz.
        /// </summary>
        /// <param name="text">Texto por el cual se va a cambiar el texto.</param>
        private void ButtonText(string text) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                DButtonText d = new DButtonText(this.ButtonText);
                this.GraphicInterface.Button.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.Button.Text = text;
            }
        }

        /// <summary>
        /// Metodo para hilo. definie si el botón esta habilitado.
        /// </summary>
        /// <param name="flag">Bandera con la cual se habilitará o deshabilitará el botón.</param>
        private void ButtonEnable(bool flag) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                DButtonEnable d = new DButtonEnable(this.ButtonEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.Button.Enabled = flag;
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        /// <param name="ip">Ip del usuario que se va a conectar.</param>
        private void InsertTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DDataGridViewPush d = new DDataGridViewPush(this.InsertTable);
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name, ip });
            } else {
                this.GraphicInterface.UsersTable.Rows.Add(name, ip);
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        private void DeleteTable(string name) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DDataGridViewPop d = new DDataGridViewPop(this.DeleteTable);
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name });
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i < size; i++) {
                    if (this.GraphicInterface.UsersTable.Rows[i].Cells[0].Value.Equals(name)) {
                        this.GraphicInterface.UsersTable.Rows.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        private void AlterIpTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DAlterTable d = new DAlterTable(this.AlterIpTable);
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name, ip });
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i < size; i++) {
                    if (this.GraphicInterface.UsersTable.Rows[i].Cells[0].Value.Equals(name)) {
                        this.GraphicInterface.UsersTable.Rows[i].Cells[1].Value = ip;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo para hilo. Habilita o deshabilita el menú.
        /// </summary>
        /// <param name="flag">Verdadero si se quiere habilitar.</param>
        private void MenuEnable(bool flag) {
            if (this.GraphicInterface.MenuBar.InvokeRequired) {
                DMenuEnable d = new DMenuEnable(this.MenuEnable);
                this.GraphicInterface.MenuBar.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.configuraciónToolStripMenuItem.Enabled = flag;

            }
        }

        /// <summary>
        /// Metodo para hilo. Elimina todos los elementos de la tabla.
        /// </summary>
        private void ClearTable() {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DClearTable d = new DClearTable(this.ClearTable);
                this.GraphicInterface.UsersTable.Invoke(d, null);
            } else {
                this.GraphicInterface.UsersTable.Rows.Clear();
            }
        }
    }
}
