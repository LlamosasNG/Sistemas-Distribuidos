import java.io.*;
import java.net.*;
import javax.net.ssl.SSLSocketFactory;

public class ClienteSSL {
    public static void main(String[] args) throws InterruptedException {
        System.setProperty("javax.net.ssl.trustStore","keystore_cliente.jks");
        System.setProperty("javax.net.ssl.trustStorePassword","123456");

        try {
            SSLSocketFactory cliente = (SSLSocketFactory) SSLSocketFactory.getDefault();
            Socket conexion = cliente.createSocket("localhost", 50000);

            DataOutputStream salida = new DataOutputStream(conexion.getOutputStream());
            //DataInputStream entrada = new DataInputStream(conexion.getInputStream());

            salida.writeDouble(1234567890.1234567890);

            Thread.sleep(1000);
            conexion.close();
        } catch (IOException i) {
            System.out.println(i);
        }
    }

}

