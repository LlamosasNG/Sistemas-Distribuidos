import java.io.*;
import java.net.*;
import java.nio.ByteBuffer;

public class Cliente {
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

    public static void main(String[] args) throws InterruptedException {
        Socket conexion = null;
        for (;;) {
            try {
                conexion = new Socket("localhost", 50000);
                break;
            } catch (Exception e) {
                System.err.println("Error al conectar con el servidor: " + e.getMessage());
                Thread.sleep(1000);
            }
        }
        try {
            DataOutputStream salida = new DataOutputStream(conexion.getOutputStream());
            DataInputStream entrada = new DataInputStream(conexion.getInputStream());

            salida.writeInt(123);
            salida.writeDouble(1234567890.1234567890);
            salida.write("hola".getBytes());

            byte[] buffer = new byte[4];
            read(entrada, buffer, 0, 4);
            System.out.println(new String(buffer, "UTF-8"));

            ByteBuffer b = ByteBuffer.allocate(5 * 8);

            b.putDouble(1.1);
            b.putDouble(1.2);
            b.putDouble(1.3);
            b.putDouble(1.4);
            b.putDouble(1.5);

            byte[] a = b.array();
            salida.write(a);

            Thread.sleep(1000);

            conexion.close();
        } catch (IOException i) {
            System.out.println(i);
        }
    }
}