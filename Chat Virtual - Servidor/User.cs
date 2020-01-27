using System.IO;
using System.Net.Sockets;
using DataStructures;
using ShippingData;
using System.Threading;
using System;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Net;

namespace Chat_Virtual___Servidor {
    public class User {
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public NetworkStream Stream { get; set; }
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }

        private TcpClient Client;
        private readonly LinkedQueue<Data> WritingQueue;
        private readonly LinkedQueue<Data> ReadingQueue;
        private readonly Semaphore CanWrite;
        private readonly Semaphore CanRead;

        public static Semaphore Regulator;
        public static ServerConnection Server;

        public User() {
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
        }
        public User(string Name) {
            this.Name = Name;
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
        }

        public User(TcpClient Client) {
            this.UpdateConnection(Client);
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
        }

        public void UpdateConnection(TcpClient Client) {
            this.IsActive = false;
            this.Client = Client;
            this.Stream = Client.GetStream();
            this.Writer = new BinaryWriter(this.Stream);
            this.Reader = new BinaryReader(this.Stream);
            this.IsActive = true;
            Thread t = new Thread(this.ExecuteRequest) {
                IsBackground = true
            };
            t.Start();
        }

        public void ReadingEnqueue(Data data) {
            this.IsActive = false;
            this.CanRead.WaitOne();
            this.ReadingQueue.Enqueue(data);
            this.CanRead.Release();
            this.IsActive = true;
        }

        public Data ReadingDequeue() {
            this.CanRead.WaitOne();
            Data data = this.ReadingQueue.Dequeue();
            this.CanRead.Release();
            return data;
        }

        public void WritingEnqueue(Data data) {
            this.CanWrite.WaitOne();
            this.WritingQueue.Enqueue(data);
            this.CanWrite.Release();
        }

        public Data WritingDequeue() {
            this.CanWrite.WaitOne();
            Data data = this.WritingQueue.Dequeue();
            this.CanWrite.Release();
            return data;
        }

        public void Disconnect() {
            this.Client.Close();
            ServerConnection.Users.Remove(this.Name);
            this.IsActive = false;
        }

        /// <summary>
        /// Método de Hilo. Redirige los mensaje y responde a las peticiones de los usuarios
        /// </summary>
        private void ExecuteRequest() {
            while (this.IsActive) {
                Regulator.WaitOne();
                this.Write();
                this.Read();
                Data Readed = this.ReadingDequeue();
                if (Readed == null) {
                    Regulator.Release();
                    Thread.Sleep(100);
                    continue;
                }
                DataBaseConnection Oracle = ServerConnection.Oracle;
                if (Readed is SignIn si) {                                             // Si el objeto recibido es un inicio de sesión.
                    bool exist = false;
                    ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT * FROM INFOMRACION_INICIO");
                    while (ServerConnection.Oracle.Oracle.DataReader.Read()) {
                        if (ServerConnection.Oracle.Oracle.DataReader["USUARIO"].Equals(si.user) && ServerConnection.Oracle.Oracle.DataReader["CONTRASEÑA"].Equals(si.password)) {
                            exist = true;
                            break;
                        }
                    }
                    if (exist) {                                                    // Si la información del cliente corresponde con la de la base de datos.
                        this.Name = si.user;
                        this.WritingEnqueue(new RequestAnswer(true));
                        ServerConnection.Users.AddElement(this.Name, this);
                        Server.ConsoleAppend("El usuario [" + this.Name + " | " + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "] se ha conectado satisfactoriamente.");
                        Server.InsertTable(this.Name, IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                        Initialize();
                    } else {                                                                                        // Si la infomación de inicio de sesión es incorrecta.
                        this.WritingEnqueue(new RequestAnswer(false));
                        this.WritingEnqueue(new RequestError(1));
                        Server.ConsoleAppend("Se ha intentado conectar el remoto [" + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "] con información de inicio de sesión incorrecta.");
                        this.Disconnect();
                    }
                } else if (Readed is SignUp su) {                                                                      // Si el objeto recibido es un nuevo registro
                    if (ServerConnection.Oracle.Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.userName + "', '" + su.name + "', '" + su.password + "', 'Hey there! I am using SADIRI.','F:\\SADIRI\\Usuarios\\default.png', null, default)")) {
                        this.Name = su.userName;
                        this.WritingEnqueue(new RequestAnswer(true));
                        Server.ConsoleAppend("Se ha registrado el usuario [" + this.Name + " | " + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "] correctamente.");
                        ServerConnection.Users.AddElement(this.Name, this);
                        Server.ConsoleAppend("El usuario [" + this.Name + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                        Server.InsertTable(this.Name, IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                        Profile profile = new Profile();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string path = ServerConnection.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string status = ServerConnection.Oracle.Oracle.DataReader["ESTADO"].ToString();
                        using (FileStream stream = File.Open(path, FileMode.Open)) {
                            profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                            stream.Close();
                        }
                        profile.Status = status;
                        profile.Name = this.Name;
                        this.WritingEnqueue(profile);

                    } else {
                        this.WritingEnqueue(new RequestAnswer(false));
                        this.WritingEnqueue(new RequestError(0));
                        this.Write();
                        Server.ConsoleAppend("Se ha intentado registrar el remoto [" + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "] con un nombre de usuario ya existente.");
                        this.Disconnect();
                    }
                } else if (Readed is Chat ch) {                                                             // Si se desea obtener la información de un Chat privado.
                    Oracle.Oracle.ExecuteSQL("SELECT USUARIO, ESTADO, RUTA_FOTO " +
                        "FROM USUARIOS " +
                        "WHERE USUARIO = '" + ch.memberTwo.Name + "'");
                    if (Oracle.Oracle.DataReader.Read()) {
                        Profile profile = new Profile {
                            Name = Oracle.Oracle.DataReader["USUARIO"].ToString(),
                            Status = Oracle.Oracle.DataReader["ESTADO"].ToString()
                        };
                        using (FileStream stream = File.Open(Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                            profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                            stream.Close();
                        }
                        this.WritingEnqueue(new Chat(ch.memberOne, profile));
                    }
                } else if (Readed is ChatMessage ms) {                                                      // Si se envía un mensaje en privado.
                    ms.date = new ShippingData.Message.Date(DateTime.Now);
                    if (ms.Image == null) {
                        Oracle.Oracle.ExecuteSQL("INSERT INTO MENSAJES_CHAT VALUES (DEFAULT, '" + ms.Sender + "', '" + ms.Receiver
                        + "', DEFAULT, '" + ms.Content + "', NULL)");
                    } else {
                        Oracle.Oracle.ExecuteSQL("INSERT INTO MENSAJES_CHAT VALUES (DEFAULT, '" + ms.Sender + "', '" + ms.Receiver
                        + "', DEFAULT, '" + ms.Content + "', 'F:\\SADIRI\\MensajesChat\\')");
                        Image picture = Serializer.DeserializeImage(ms.Image);

                        Oracle.Oracle.ExecuteSQL("SELECT MAX(ID_MENSAJE) AS MAX FROM MENSAJES_CHAT");
                        Oracle.Oracle.DataReader.Read();

                        string path = "F:\\SADIRI\\MensajesChat\\" + Oracle.Oracle.DataReader["MAX"].ToString() + ".png";
                        using (FileStream stream = File.Open(path, FileMode.Create)) {
                            picture.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            stream.Close();
                        }
                    }
                    User theOther;
                    if (this.Name.Equals(ms.Sender)) {
                        theOther = ServerConnection.Users.Search(ms.Receiver);
                    } else {
                        theOther = ServerConnection.Users.Search(ms.Sender);
                    }
                    this.WritingEnqueue(ms);
                    theOther?.WritingEnqueue(ms);
                    Server.ConsoleAppend("Mensaje  [" + ms.Sender + "] a [" + ms.Receiver + "]: " + ms.Content);
                } else if (Readed is GroupMessage groupMessage) {                                           // Si se envía un mensaje en un grupo.
                    Oracle.Oracle.ExecuteSQL("SELECT USUARIO FROM INTEGRANTES_GRUPO WHERE ID_GRUPO = '" + groupMessage.IdGroupReceiver + "'");
                    while (Oracle.Oracle.DataReader.Read()) {
                        ServerConnection.Users.Search(Oracle.Oracle.DataReader["USUARIO"].ToString()).WritingEnqueue(groupMessage);
                    }
                                                                                                            //Carpeta: F:\\SADIRI\\MensajesGrupos\\
                } else if (Readed is Profile profile) {                                                     // Si es un cambio de perfil.
                    if (profile.Status != null) {
                        Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET ESTADO = '" + profile.Status + "' WHERE USUARIO = '" + profile.Name + "'");
                        this.WritingEnqueue(new RequestAnswer(true));
                    }
                    if (profile.Image != null) {
                        Image foto = Serializer.DeserializeImage(profile.Image);
                        string path = "F:\\SADIRI\\Usuarios\\" + profile.Name + ".png";
                        using (FileStream stream = File.Open(path, FileMode.Create)) {
                            foto.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            stream.Close();
                        }
                        Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET RUTA_FOTO = 'F:\\SADIRI\\Usuarios\\" + profile.Name + ".png' WHERE USUARIO = '" + profile.Name + "'");
                        this.WritingEnqueue(new RequestAnswer(true)); //TODO: Código respuesta.
                    }
                    Server.ConsoleAppend("Se ha cambiado satisfactoriamente el perfil del usuario  [" + this.Name + " | " + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "]. ");
                } else if (Readed is ChangePassword changePassword) {                                       // Si es una solicitud de cambio de contraseña.
                    Oracle.Oracle.ExecuteSQL("SELECT CONTRASEÑA FROM USUARIOS WHERE USUARIO = '" + changePassword.UserName + "'");
                    Oracle.Oracle.DataReader.Read();
                    string currentPassword = Oracle.Oracle.DataReader["CONTRASEÑA"].ToString();
                    if (!changePassword.CurrentPassword.Equals(currentPassword)) {
                        this.WritingEnqueue(new RequestAnswer(false, 3));
                        this.WritingEnqueue(new RequestError(3));
                        Server.ConsoleAppend("El cambio contraseña del usuario  [" + this.Name + " | " + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "]. no pudo ser realizado.");
                    } else {
                        Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET CONTRASEÑA = '" + changePassword.NewPassword + "' WHERE USUARIO = '" + changePassword.UserName + "'");
                        Server.ConsoleAppend("Se ha cambiado satisfactoriamente la contraseña del usuario  [" + this.Name + " | " + this.Client.Client.RemoteEndPoint.ToString() + "]. ");
                        this.WritingEnqueue(new RequestAnswer(true, 3));
                    }
                } else if (Readed is TreeActivities tree) {                                                 // Si es una actualización del arbol de tareas.
                    string path = "F:\\SADIRI\\ArbolesTareas\\" + this.Name + ".dat";
                    IFormatter formatter = new BinaryFormatter();
                    using (FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
                        formatter.Serialize(stream, tree.Node);
                        stream.Close();
                    }
                    Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET RUTA_ARBOL = '" + path + "' WHERE USUARIO = '" + this.Name + "'");
                    Server.ConsoleAppend("Se ha guardado satisfactoriamente el árbol de tareas del usuario  [" + this.Name + " | " + IPAddress.Parse(((IPEndPoint)this.Client.Client.RemoteEndPoint).Address.ToString()) + "]. ");
                } else if (Readed is Search search) {
                    if (search.ToSearch == ToSearch.Chat) {
                        Oracle.Oracle.ExecuteSQL(
                            "SELECT USUARIO, ESTADO, RUTA_FOTO " +
                            "FROM USUARIOS " +
                            "WHERE LOWER(USUARIO) LIKE '%" + search.StringToSearch.ToLower() + "%'");
                        LinkedList<Profile> Results = new LinkedList<Profile>();
                        while (Oracle.Oracle.DataReader.Read()) {
                            Profile Profile = new Profile {
                                Name = Oracle.Oracle.DataReader["USUARIO"].ToString(),
                                Status = Oracle.Oracle.DataReader["ESTADO"].ToString()
                            };
                            if (Profile.Name.Equals(this.Name))
                                continue;
                            using (FileStream stream = File.Open(Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                                Profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                stream.Close();
                            }
                            Results.Add(Profile);
                        }
                        this.WritingEnqueue(new UserList(SearchRequest.SearchUsers, Results.ToArray()));
                    } else if (search.ToSearch == ToSearch.Group) {

                    } else if (search.ToSearch == ToSearch.NewGroup) {
                        Oracle.Oracle.ExecuteSQL(
                            "SELECT USUARIO, ESTADO, RUTA_FOTO " +
                            "FROM USUARIOS " +
                            "WHERE LOWER(USUARIO) != '" + search.StringToSearch.ToLower() + "'"); //Despues hay que cambiarlo a una busqueda entre amigos pero mientras no agreguemos amigos pues eso
                        LinkedList<Profile> Results = new LinkedList<Profile>();
                        while (Oracle.Oracle.DataReader.Read()) {
                            Profile Profile = new Profile {
                                Name = Oracle.Oracle.DataReader["USUARIO"].ToString(),
                                Status = Oracle.Oracle.DataReader["ESTADO"].ToString()
                            };
                            using (FileStream stream = File.Open(Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                                Profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                stream.Close();
                            }
                            Results.Add(Profile);
                        }
                        this.WritingEnqueue(new UserList(SearchRequest.CreateGroup, Results.ToArray()));
                    }
                } else if (Readed is CreateGroup createGroup) { 
                    ChatGroup chatGroup = new ChatGroup();
                    Oracle.Oracle.ExecuteSQL("SELECT * " +
                        "FROM GRUPOS " +
                        "WHERE NOMBRE = '" + createGroup.Name + "' AND PROPIETARIO = '" + this.Name + "'");
                    if (Oracle.Oracle.DataReader.Read()) {
                        this.WritingEnqueue(new RequestError(2, "El grupo ya existe"));
                        continue;
                    } else {
                        chatGroup.Name = createGroup.Name;
                        chatGroup.Description = createGroup.Description;
                        chatGroup.Photo = createGroup.Photo;
                    }
                    Oracle.Oracle.ExecuteSQL("INSERT INTO GRUPOS VALUES(DEFAULT, '" + createGroup.Name + "', '" + this.Name + "', '" + createGroup.Description + "')");
                    Oracle.Oracle.ExecuteSQL("SELECT ID_GRUPO " +
                        "FROM GRUPOS " +
                        "WHERE NOMBRE = '" + createGroup.Name + "' AND PROPIETARIO = '" + this.Name + "'");
                    if (Oracle.Oracle.DataReader.Read()) {
                        chatGroup.IdGroup = int.Parse(Oracle.Oracle.DataReader["ID_GRUPO"].ToString());
                    }
                    for (int i = 0; i<createGroup.Members.Length; i++) {
                        Oracle.Oracle.ExecuteSQL("INSERT INTO INTEGRANTES_GRUPO VALUES ('" + chatGroup.IdGroup + "', '" + createGroup.Members[i] + "')");
                        ServerConnection.Users.Search(createGroup.Members[i])?.WritingEnqueue(chatGroup);
                    }
                }
                Regulator.Release();
            }
        }



        /// <summary>
        /// Escribe los datos que esten pendientes en la cola WritingQueue de un ususario.
        /// </summary>
        /// <param name="user">El ususario del que se van a intentar escribir los datos.</param>
        /// <returns>Verdadero si los datos fueron enviados, falso si almenos uno falló.</returns>
        public bool Write() {
            Data data = this.WritingDequeue();
            if (data == default) {
                return false;
            } else {
                try {
                    byte[] toSend = Serializer.Serialize(data);                                      // Serializa el primer objeto de la cola.
                    this.Writer.Write(toSend.Length);                                                // Envía el tamaño del objeto.
                    this.Writer.Write(toSend);                                                       // Envía el objeto.     
                    return true;
                } catch (Exception) {
                    //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                    //this.ConsoleAppend(ex.Message);
                    this.WritingEnqueue(data);
                    return false;
                }
            }
        }

        /// <summary>
        /// Lee los datos que estén pendientes para un usuario y los guarda en la cola ReadingQueue del mismo.
        /// </summary>
        /// <param name="user">EL ususario del que se van a intentar leer datos.</param>
        /// <returns>Verdadero si le leyeron todos los datos, falso si uno de ellos no pudo ser leido</returns>
        public bool Read() {
            try {
                if (this.Stream.DataAvailable) {                                            // Verifica si hay datos por leer.
                    int size = this.Reader.ReadInt32();                                     // Lee el tamaño del objeto.
                    byte[] data = new byte[size];                                           // Crea el arreglo de bytes para el objeto.
                    data = this.Reader.ReadBytes(size);                                     // Lee el el objeto y lo guarda en el arreglo de bytes.
                    object a = Serializer.Deserialize(data);                                // Deserializa el objeto.
                    this.ReadingEnqueue((Data)a);                                           // Guarda el objeto en la cola de lectura.
                }
                return true;
            } catch (Exception) {
                //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                return false;
            }
        }

        private void Initialize() {
            //Enviar el perfil
            Profile profile = new Profile();
            ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
            ServerConnection.Oracle.Oracle.DataReader.Read();
            string path = ServerConnection.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
            ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
            ServerConnection.Oracle.Oracle.DataReader.Read();
            string status = ServerConnection.Oracle.Oracle.DataReader["ESTADO"].ToString();
            using (FileStream stream = File.Open(path, FileMode.Open)) {
                profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
            }
            profile.Status = status;
            profile.Name = this.Name;
            this.WritingEnqueue(profile);

            //Enviar el arbol
            TreeActivities tree = new TreeActivities();
            ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_ARBOL FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
            ServerConnection.Oracle.Oracle.DataReader.Read();
            string treePath = ServerConnection.Oracle.Oracle.DataReader["RUTA_ARBOL"].ToString();
            if (treePath != "") {
                IFormatter formatter = new BinaryFormatter();
                using (FileStream stream = File.Open(treePath, FileMode.Open, FileAccess.Read)) {
                    tree.Node = (TreeNode[])formatter.Deserialize(stream);
                    stream.Close();
                }
                this.WritingEnqueue(tree);
            }

            //Enviar los grupos
            //Enviar los 10 mensajes mas recientes que se vieron y todos los que no se han visto en el grupo

            //Enviar los chats
            //Enviar los 10 mensajes mas recientes que se vieron y todos los que no se han visto en un chat
        }
    }
}
