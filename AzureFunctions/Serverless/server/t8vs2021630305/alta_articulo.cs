using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class alta_articulo
    {
        class Articulo
        {
            public string? nombre;
            public string? descripcion;
            public double? precio;
            public int? cantidad;
            public int? id_usuario;
            public string? token;
            public string? foto;  // foto en base 64
        }

        class ParamAltaArticulo
        {
            public Articulo? articulo;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("alta_articulo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamAltaArticulo? data = JsonConvert.DeserializeObject<ParamAltaArticulo>(body);

                if (data == null || data.articulo == null)
                    throw new Exception("Se esperan los datos del artículo");

                Articulo articulo = data.articulo;

                // Validaciones de campos obligatorios
                if (articulo.nombre == null || articulo.nombre == "")
                    throw new Exception("Se debe ingresar el nombre del artículo");

                if (articulo.precio == null || articulo.precio <= 0)
                    throw new Exception("El precio debe ser mayor a cero");

                if (articulo.cantidad == null || articulo.cantidad < 0)
                    throw new Exception("La cantidad no puede ser negativa");

                if (articulo.id_usuario == null)
                    throw new Exception("Se debe proporcionar el ID de usuario");

                if (articulo.token == null || articulo.token == "")
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
                    cmd_token.Parameters.AddWithValue("@id_usuario", articulo.id_usuario);

                    MySqlDataReader reader = cmd_token.ExecuteReader();

                    try
                    {
                        if (!reader.Read())
                        {
                            return new UnauthorizedObjectResult(JsonConvert.SerializeObject(new Error("El usuario no existe")));
                        }

                        string? token_guardado = reader.IsDBNull(0) ? null : reader.GetString(0);

                        if (token_guardado == null || token_guardado != articulo.token)
                        {
                            return new UnauthorizedObjectResult(JsonConvert.SerializeObject(new Error("Token inválido")));
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }

                    // Iniciar transacción para insertar artículo
                    MySqlTransaction transaccion = conexion.BeginTransaction();

                    try
                    {
                        // Insertar el artículo en la tabla stock
                        MySqlCommand cmd_1 = new MySqlCommand();
                        cmd_1.Connection = conexion;
                        cmd_1.Transaction = transaccion;
                        cmd_1.CommandText = @"INSERT INTO stock(id_articulo, nombre, descripcion, precio, cantidad) 
                                             VALUES (0, @nombre, @descripcion, @precio, @cantidad)";

                        cmd_1.Parameters.AddWithValue("@nombre", articulo.nombre);
                        cmd_1.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                        cmd_1.Parameters.AddWithValue("@precio", articulo.precio);
                        cmd_1.Parameters.AddWithValue("@cantidad", articulo.cantidad);

                        cmd_1.ExecuteNonQuery();
                        long id_articulo = cmd_1.LastInsertedId;

                        // Si hay foto, insertarla en la tabla fotos_articulos
                        if (articulo.foto != null)
                        {
                            MySqlCommand cmd_2 = new MySqlCommand();
                            cmd_2.Connection = conexion;
                            cmd_2.Transaction = transaccion;
                            cmd_2.CommandText = @"INSERT INTO fotos_articulos(id_foto, foto, id_articulo) 
                                                 VALUES (0, @foto, @id_articulo)";

                            cmd_2.Parameters.AddWithValue("@foto", Convert.FromBase64String(articulo.foto));
                            cmd_2.Parameters.AddWithValue("@id_articulo", id_articulo);
                            cmd_2.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        return new OkResult();
                    }
                    catch (Exception e)
                    {
                        transaccion.Rollback();
                        throw new Exception(e.Message);
                    }
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