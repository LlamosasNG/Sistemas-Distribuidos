using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class consulta_articulos
    {
        class Articulo
        {
            public int? id_articulo;
            public string? nombre;
            public string? descripcion;
            public double? precio;
            public int? cantidad;
            public string? foto;  // foto en base 64
        }

        class ParamConsultaArticulos
        {
            public int? id_usuario;
            public string? token;
            public string? palabra_clave;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("consulta_articulos")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamConsultaArticulos? data = JsonConvert.DeserializeObject<ParamConsultaArticulos>(body);

                if (data == null)
                    throw new Exception("Se esperan los parámetros de consulta");

                // Validación de parámetros
                if (data.id_usuario == null)
                    throw new Exception("Se debe proporcionar el ID de usuario");

                if (data.token == null || data.token == "")
                    throw new Exception("Se debe proporcionar el token de autenticación");

                // Configuración de conexión a base de datos
                string? Server = Environment.GetEnvironmentVariable("Server");
                string? UserID = Environment.GetEnvironmentVariable("UserID");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");
                string cs = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";

                var conexion = new MySqlConnection(cs);
                conexion.Open();

                try
                {
                    // Verificar que el token corresponda al id_usuario
                    MySqlCommand cmd_token = new MySqlCommand();
                    cmd_token.Connection = conexion;
                    cmd_token.CommandText = "SELECT token FROM usuarios WHERE id_usuario = @id_usuario";
                    cmd_token.Parameters.AddWithValue("@id_usuario", data.id_usuario);

                    MySqlDataReader reader = cmd_token.ExecuteReader();

                    try
                    {
                        if (!reader.Read())
                        {
                            return new UnauthorizedObjectResult(JsonConvert.SerializeObject(new Error("El usuario no existe")));
                        }

                        string? token_guardado = reader.IsDBNull(0) ? null : reader.GetString(0);

                        if (token_guardado == null || token_guardado != data.token)
                        {
                            return new UnauthorizedObjectResult(JsonConvert.SerializeObject(new Error("Token inválido")));
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }

                    // Lista para almacenar los artículos encontrados
                    List<Articulo> lista = new List<Articulo>();

                    // Construir la consulta SQL dinámicamente
                    string sql = @"SELECT s.id_articulo, s.nombre, s.descripcion, s.precio, s.cantidad, f.foto 
                                  FROM stock s 
                                  LEFT JOIN fotos_articulos f ON s.id_articulo = f.id_articulo ";

                    // Si se proporcionó una palabra clave, agregar la condición LIKE
                    if (data.palabra_clave != null && data.palabra_clave != "")
                    {
                        sql += "WHERE s.nombre LIKE @patron OR s.descripcion LIKE @patron ";
                    }

                    // Ordenar por id_articulo
                    sql += "ORDER BY s.id_articulo";

                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.CommandText = sql;

                    // Si hay palabra clave, establecer los parámetros
                    if (data.palabra_clave != null && data.palabra_clave != "")
                    {
                        string patron = "%" + data.palabra_clave + "%";
                        cmd.Parameters.AddWithValue("@patron", patron);
                    }

                    MySqlDataReader rs = cmd.ExecuteReader();

                    try
                    {
                        while (rs.Read())
                        {
                            Articulo articulo = new Articulo();
                            articulo.id_articulo = rs.IsDBNull(0) ? null : rs.GetInt32(0);
                            articulo.nombre = rs.IsDBNull(1) ? null : rs.GetString(1);
                            articulo.descripcion = rs.IsDBNull(2) ? null : rs.GetString(2);
                            articulo.precio = rs.IsDBNull(3) ? null : rs.GetDouble(3);
                            articulo.cantidad = rs.IsDBNull(4) ? null : rs.GetInt32(4);

                            // Convertir foto de bytes a Base64 si existe
                            if (!rs.IsDBNull(5))
                            {
                                byte[] fotoBytes = (byte[])rs.GetValue(5);
                                articulo.foto = Convert.ToBase64String(fotoBytes);
                            }
                            else
                            {
                                articulo.foto = null;
                            }

                            lista.Add(articulo);
                        }
                    }
                    finally
                    {
                        rs.Close();
                    }

                    return new OkObjectResult(JsonConvert.SerializeObject(lista));
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}