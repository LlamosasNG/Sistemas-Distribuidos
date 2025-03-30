
import java.net.ServerSocket;
import java.net.Socket;
import java.io.*;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

class ServidorHTTP {
    static class Worker extends Thread {
        Socket conexion;
        static final String LAST_MODIFIED_DATE = getServerTime(); // Simulamos la última modificación

        Worker(Socket conexion) {
            this.conexion = conexion;
        }

        int valor(String parametros, String variable) throws Exception {
            String[] p = parametros.split("&");
            for (String param : p) {
                String[] s = param.split("=");
                if (s[0].equals(variable))
                    return Integer.parseInt(s[1]);
            }
            throw new Exception("Se espera la variable: " + variable);
        }

        public void run() {
            try {
                BufferedReader entrada = new BufferedReader(new InputStreamReader(conexion.getInputStream()));
                PrintWriter salida = new PrintWriter(conexion.getOutputStream());

                String req = entrada.readLine();
                System.out.println("Petición: " + req);

                boolean isModified = true;
                String ifModifiedSince = null;

                for (;;) {
                    String encabezado = entrada.readLine();
                    System.out.println("Encabezado: " + encabezado);
                    if (encabezado.equals(""))
                        break;

                    if (encabezado.startsWith("If-Modified-Since: ")) {
                        ifModifiedSince = encabezado.substring(19);
                    }
                }

                if (ifModifiedSince != null && ifModifiedSince.equals(LAST_MODIFIED_DATE)) {
                    // Responder con 304 Not Modified si el recurso no ha cambiado
                    salida.println("HTTP/1.1 304 Not Modified");
                    salida.println("Connection: close");
                    salida.println();
                    salida.flush();
                    isModified = false;
                }

                if (isModified) {
                    if (req.startsWith("GET / ")) {
                        String respuesta = "<html>" +
                                "<script>" +
                                "function get(req,callback)" +
                                "{" +
                                "const xhr = new XMLHttpRequest();" +
                                "xhr.open('GET', req, true);" +
                                "xhr.onload=function()" +
                                "{" +
                                "if (callback != null) callback(xhr.status,xhr.response);" +
                                "};" +
                                "xhr.send();" +
                                "}" +
                                "</script>" +
                                "<body>" +
                                "<button onclick=\"get('/suma?a=1&b=6&c=3',function(status,response){alert(status + ' ' + response);})\">Aceptar</button>"
                                +
                                "</body>" +
                                "</html>";
                        salida.println("HTTP/1.1 200 OK");
                        salida.println("Content-type: text/html; charset=utf-8");
                        salida.println("Content-length: " + respuesta.length());
                        salida.println("Last-Modified: " + LAST_MODIFIED_DATE);
                        salida.println("Connection: close");
                        salida.println();
                        salida.println(respuesta);
                        salida.flush();
                    } else if (req.startsWith("GET /suma?")) {
                        String parametros = req.split(" ")[1].split("\\?")[1];
                        String respuesta = String
                                .valueOf(valor(parametros, "a") + valor(parametros, "b") + valor(parametros, "c"));
                        salida.println("HTTP/1.1 200 OK");
                        salida.println("Access-Control-Allow-Origin: *");
                        salida.println("Content-type: text/plain; charset=utf-8");
                        salida.println("Content-length: " + respuesta.length());
                        salida.println("Last-Modified: " + LAST_MODIFIED_DATE);
                        salida.println("Connection: close");
                        salida.println();
                        salida.println(respuesta);
                        salida.flush();
                    } else {
                        salida.println("HTTP/1.1 404 File Not Found");
                        salida.flush();
                    }
                }
            } catch (Exception e) {
                System.err.println("Error en la conexión: " + e.getMessage());
            } finally {
                try {
                    conexion.close();
                } catch (Exception e) {
                    System.err.println("Error en close: " + e.getMessage());
                }
            }
        }
    }

    public static void main(String[] args) throws Exception {
        int puerto = 8080;

        ServerSocket servidor = new ServerSocket(puerto);
        System.out.println("Servidor HTTP ejecutado en el puerto: " + puerto);

        for (;;) {
            Socket conexion = servidor.accept();
            new Worker(conexion).start();
        }
    }

    private static String getServerTime() {
        SimpleDateFormat formatter = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss z", Locale.US);
        formatter.setTimeZone(TimeZone.getTimeZone("GMT"));
        return formatter.format(new Date()); // Simula la última modificación como la hora actual del servidor
    }
}
