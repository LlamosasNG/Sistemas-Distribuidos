import java.io.*;
import java.net.*;
import java.nio.ByteBuffer;

public class Servidor {
    public static void read(DataInputStream f, byte[] b, int posicion, int longitud) throws IOException {
        while (longitud > 0) {
            int n = f.read(b, posicion, longitud);
            if (n == -1) {
                throw new EOFException("Se cerró la conexión antes de recibir todos los datos esperados.");
            }
            posicion += n;
            longitud -= n;
        }
    }

    public static void main(String[] args) {
        final int PUERTO = 50000;

        try (ServerSocket servidor = new ServerSocket(PUERTO)) {
            for (;;) {
                System.out.println("Esperando cliente...");
                try (
                        Socket conexion = servidor.accept();
                        DataInputStream entrada = new DataInputStream(conexion.getInputStream());
                        DataOutputStream salida = new DataOutputStream(conexion.getOutputStream());
                ) {
                    System.out.println("Cliente conectado desde: " + conexion.getInetAddress());

                    int n = entrada.readInt();
                    System.out.println(n);

                    double x = entrada.readDouble();
                    System.out.println(x);

                    byte[] buffer = new byte[4];
                    read(entrada, buffer, 0, 4);
                    System.out.println(new String(buffer, "UTF-8"));

                    salida.write("HOLA".getBytes());;

                    byte[] a = new byte[5 * 8];
                    read(entrada, a, 0, 5 * 8);
                    ByteBuffer b = ByteBuffer.wrap(a);

                    for (int i = 0; i < 5; i++) System.out.println(b.getDouble());

                    conexion.close();
                    System.out.println("Cliente desconectado.\n");
                } catch (IOException e) {
                    System.err.println("Error con el cliente: " + e.getMessage());
                }
            }
        } catch (IOException e) {
            System.err.println("Error al iniciar el servidor: " + e.getMessage());
        }
    }
}