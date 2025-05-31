import java.io.*;
import java.net.*;
import java.nio.ByteBuffer;

public class ServidorMultihilos {
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

    static class Worker extends Thread {
        Socket conexion;

        Worker(Socket conexion) {
            this.conexion = conexion;
        }

        public void run() {
            try {
                DataInputStream entrada = new DataInputStream(conexion.getInputStream());
                DataOutputStream salida = new DataOutputStream(conexion.getOutputStream());

                System.out.println("Cliente conectado desde: " + conexion.getInetAddress());

                int n = entrada.readInt();
                System.out.println(n);

                double x = entrada.readDouble();
                System.out.println(x);

                byte[] buffer = new byte[4];
                read(entrada, buffer, 0, 4);
                System.out.println(new String(buffer, "UTF-8"));

                salida.write("HOLA".getBytes());

                byte[] a = new byte[5 * 8];
                read(entrada, a, 0, 5 * 8);
                ByteBuffer b = ByteBuffer.wrap(a);

                for (int i = 0; i < 5; i++) System.out.println(b.getDouble());
            } catch (IOException e) {
                System.err.println(e.getMessage());
            } finally {
                try {
                    conexion.close();
                } catch (IOException e2) {
                    System.err.println(e2.getMessage());
                }
            }
        }
    }

    public static void main(String[] args) throws Exception {
        System.out.println("Esperando cliente...");
        ServerSocket servidor = new ServerSocket(50000);
        for (; ; ) {
            Socket conexion = servidor.accept();
            Worker w = new Worker(conexion);
            w.start();
        }
    }
}
