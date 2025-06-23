using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class compra_articulo
    {
        class ParamCompraArticulo
        {
            public int? id_articulo;
            public int? cantidad;
            public int? id_usuario;
            public string? token;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("compra_articulo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamCompraArticulo? data = JsonConvert.DeserializeObject<ParamCompraArticulo>(body);

                if (data == null)
                    throw new Exception("Se esperan los parámetros de compra");

                // Validación de parámetros
                if (data.id_articulo == null)
                    throw new Exception("Se debe proporcionar el ID del artículo");

                if (data.cantidad == null || data.cantidad <= 0)
                    throw new Exception("La cantidad debe ser mayor a cero");

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

                    // Iniciar transacción
                    MySqlTransaction transaccion = conexion.BeginTransaction();

                    try
                    {
                        // Verificar si hay suficiente stock (con FOR UPDATE para bloqueo)
                        MySqlCommand cmd_stock = new MySqlCommand();
                        cmd_stock.Connection = conexion;
                        cmd_stock.Transaction = transaccion;
                        cmd_stock.CommandText = "SELECT cantidad FROM stock WHERE id_articulo = @id_articulo FOR UPDATE";
                        cmd_stock.Parameters.AddWithValue("@id_articulo", data.id_articulo);

                        MySqlDataReader rs_stock = cmd_stock.ExecuteReader();

                        int cantidad_disponible;
                        try
                        {
                            if (!rs_stock.Read())
                            {
                                transaccion.Rollback();
                                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("El artículo no existe")));
                            }

                            cantidad_disponible = rs_stock.GetInt32(0);
                        }
                        finally
                        {
                            rs_stock.Close();
                        }

                        // Verificar si hay suficiente stock
                        if (data.cantidad > cantidad_disponible)
                        {
                            transaccion.Rollback();
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("No hay suficientes artículos")));
                        }

                        // Insertar en carrito_compra (o actualizar si ya existe)
                        MySqlCommand cmd_carrito = new MySqlCommand();
                        cmd_carrito.Connection = conexion;
                        cmd_carrito.Transaction = transaccion;
                        cmd_carrito.CommandText = @"INSERT INTO carrito_compra(id_usuario, id_articulo, cantidad) 
                                                   VALUES (@id_usuario, @id_articulo, @cantidad) 
                                                   ON DUPLICATE KEY UPDATE cantidad = cantidad + @cantidad_adicional";

                        cmd_carrito.Parameters.AddWithValue("@id_usuario", data.id_usuario);
                        cmd_carrito.Parameters.AddWithValue("@id_articulo", data.id_articulo);
                        cmd_carrito.Parameters.AddWithValue("@cantidad", data.cantidad);
                        cmd_carrito.Parameters.AddWithValue("@cantidad_adicional", data.cantidad);

                        cmd_carrito.ExecuteNonQuery();

                        // Actualizar el stock
                        MySqlCommand cmd_update = new MySqlCommand();
                        cmd_update.Connection = conexion;
                        cmd_update.Transaction = transaccion;
                        cmd_update.CommandText = "UPDATE stock SET cantidad = cantidad - @cantidad WHERE id_articulo = @id_articulo";

                        cmd_update.Parameters.AddWithValue("@cantidad", data.cantidad);
                        cmd_update.Parameters.AddWithValue("@id_articulo", data.id_articulo);

                        cmd_update.ExecuteNonQuery();

                        // Confirmar la transacción
                        transaccion.Commit();

                        return new OkResult();
                    }
                    catch (Exception e)
                    {
                        // Deshacer la transacción en caso de error
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
                    try
                    {
                        conexion.Close();
                    }
                    catch (Exception)
                    {
                        // Ignorar errores al cerrar la conexión
                    }
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}