using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class elimina_articulo_carrito_compra
    {
        class ParamEliminaArticuloCarrito
        {
            public int? id_usuario;
            public int? id_articulo;
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

        [Function("elimina_articulo_carrito_compra")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamEliminaArticuloCarrito? data = JsonConvert.DeserializeObject<ParamEliminaArticuloCarrito>(body);

                if (data == null)
                    throw new Exception("Se esperan los parámetros para eliminar del carrito");

                // Validación de parámetros
                if (data.id_usuario == null)
                    throw new Exception("Se debe proporcionar el ID de usuario");

                if (data.id_articulo == null)
                    throw new Exception("Se debe proporcionar el ID del artículo");

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
                        // Primero, obtener la cantidad de artículos en el carrito
                        MySqlCommand cmd_cantidad = new MySqlCommand();
                        cmd_cantidad.Connection = conexion;
                        cmd_cantidad.Transaction = transaccion;
                        cmd_cantidad.CommandText = "SELECT cantidad FROM carrito_compra WHERE id_usuario = @id_usuario AND id_articulo = @id_articulo";

                        cmd_cantidad.Parameters.AddWithValue("@id_usuario", data.id_usuario);
                        cmd_cantidad.Parameters.AddWithValue("@id_articulo", data.id_articulo);

                        MySqlDataReader rs_cantidad = cmd_cantidad.ExecuteReader();

                        int cantidad_a_devolver;
                        try
                        {
                            // Si no existe el artículo en el carrito, devolver error
                            if (!rs_cantidad.Read())
                            {
                                transaccion.Rollback();
                                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("El artículo no existe en el carrito de compra")));
                            }

                            // Obtener la cantidad que se devolverá al stock
                            cantidad_a_devolver = rs_cantidad.GetInt32("cantidad");
                        }
                        finally
                        {
                            rs_cantidad.Close();
                        }

                        // Actualizar el stock (devolver los artículos)
                        MySqlCommand cmd_update = new MySqlCommand();
                        cmd_update.Connection = conexion;
                        cmd_update.Transaction = transaccion;
                        cmd_update.CommandText = "UPDATE stock SET cantidad = cantidad + @cantidad WHERE id_articulo = @id_articulo";

                        cmd_update.Parameters.AddWithValue("@cantidad", cantidad_a_devolver);
                        cmd_update.Parameters.AddWithValue("@id_articulo", data.id_articulo);

                        int filas_actualizadas = cmd_update.ExecuteNonQuery();

                        // Verificar que el artículo exista en stock
                        if (filas_actualizadas == 0)
                        {
                            transaccion.Rollback();
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("El artículo no existe en el inventario")));
                        }

                        // Eliminar el artículo del carrito
                        MySqlCommand cmd_delete = new MySqlCommand();
                        cmd_delete.Connection = conexion;
                        cmd_delete.Transaction = transaccion;
                        cmd_delete.CommandText = "DELETE FROM carrito_compra WHERE id_usuario = @id_usuario AND id_articulo = @id_articulo";

                        cmd_delete.Parameters.AddWithValue("@id_usuario", data.id_usuario);
                        cmd_delete.Parameters.AddWithValue("@id_articulo", data.id_articulo);

                        cmd_delete.ExecuteNonQuery();

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