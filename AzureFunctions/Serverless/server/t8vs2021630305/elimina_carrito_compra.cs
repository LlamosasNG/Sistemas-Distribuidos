using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class elimina_carrito_compra
    {
        class ParamEliminaCarrito
        {
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

        class Respuesta
        {
            public string mensaje;
            public Respuesta(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("elimina_carrito_compra")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamEliminaCarrito? data = JsonConvert.DeserializeObject<ParamEliminaCarrito>(body);

                if (data == null)
                    throw new Exception("Se esperan los parámetros para eliminar el carrito");

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

                    // Iniciar transacción
                    MySqlTransaction transaccion = conexion.BeginTransaction();

                    try
                    {
                        // Obtener todos los artículos en el carrito del usuario
                        MySqlCommand cmd_carrito = new MySqlCommand();
                        cmd_carrito.Connection = conexion;
                        cmd_carrito.Transaction = transaccion;
                        cmd_carrito.CommandText = "SELECT id_articulo, cantidad FROM carrito_compra WHERE id_usuario = @id_usuario";
                        cmd_carrito.Parameters.AddWithValue("@id_usuario", data.id_usuario);

                        MySqlDataReader rs_carrito = cmd_carrito.ExecuteReader();

                        // Lista para almacenar los artículos que necesitan actualización de stock
                        var articulosParaActualizar = new List<(int id_articulo, int cantidad)>();

                        // Leer todos los artículos del carrito
                        while (rs_carrito.Read())
                        {
                            int id_articulo = rs_carrito.GetInt32("id_articulo");
                            int cantidad = rs_carrito.GetInt32("cantidad");
                            articulosParaActualizar.Add((id_articulo, cantidad));
                        }

                        rs_carrito.Close();

                        // Variable para verificar si había artículos en el carrito
                        bool hayArticulos = articulosParaActualizar.Count > 0;

                        // Devolver cada artículo al stock
                        foreach (var articulo in articulosParaActualizar)
                        {
                            MySqlCommand cmd_update = new MySqlCommand();
                            cmd_update.Connection = conexion;
                            cmd_update.Transaction = transaccion;
                            cmd_update.CommandText = "UPDATE stock SET cantidad = cantidad + @cantidad WHERE id_articulo = @id_articulo";

                            cmd_update.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                            cmd_update.Parameters.AddWithValue("@id_articulo", articulo.id_articulo);

                            cmd_update.ExecuteNonQuery();
                        }

                        // Eliminar todos los artículos del carrito del usuario
                        MySqlCommand cmd_delete = new MySqlCommand();
                        cmd_delete.Connection = conexion;
                        cmd_delete.Transaction = transaccion;
                        cmd_delete.CommandText = "DELETE FROM carrito_compra WHERE id_usuario = @id_usuario";
                        cmd_delete.Parameters.AddWithValue("@id_usuario", data.id_usuario);

                        cmd_delete.ExecuteNonQuery();

                        // Confirmar la transacción
                        transaccion.Commit();

                        // Si no había artículos, informar al cliente
                        if (!hayArticulos)
                        {
                            return new OkObjectResult(JsonConvert.SerializeObject(new Respuesta("El carrito ya estaba vacío")));
                        }

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