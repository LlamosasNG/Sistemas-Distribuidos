import java.io.*;
import java.net.*;
import javax.net.ssl.SSLServerSocketFactory;

public class ServidorSSL {
    static class Worker extends Thread {
        Socket conexion;

        Worker(Socket conexion) {
            this.conexion = conexion;
        }

        public void run() {
            try {

                DataInputStream entrada = new DataInputStream(conexion.getInputStream());
                // DataOutputStream salida = new DataOutputStream(conexion.getOutputStream());

                System.out.println("Cliente conectado desde: " + conexion.getInetAddress());

                double x = entrada.readDouble();
                System.out.println(x);

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

        System.setProperty("javax.net.ssl.keyStore", "keystore_servidor.jks");
        System.setProperty("javax.net.ssl.keyStorePassword", "1234567");
        SSLServerSocketFactory socket_factory = (SSLServerSocketFactory) SSLServerSocketFactory.getDefault();
        ServerSocket socket_servidor = socket_factory.createServerSocket(50000);

        for (;;) {
            Socket conexion = socket_servidor.accept();
            Worker w = new Worker(conexion);
            w.start();
        }
    }
}
