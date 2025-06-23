# Usamos una imagen base de OpenJDK
FROM openjdk:11-jre-slim
# Establecemos /app como el directorio de trabajo dentro del contenedor
WORKDIR /app
# Copiamos el archivo JAR de la aplicación y el archivo keystore
COPY ServidorHTTPS.jar /app/ServidorHTTPS.jar
COPY keystore_servidor.jks /app/keystore_servidor.jks
# Exponemos el puerto 443 para que la aplicación pueda recibir tráfico HTTPS
EXPOSE 443
# Definimos las variables de entorno que accederá la aplicación en tiempo de ejecución
ENV keystore="keystore_servidor.jks"
ENV password="1234567"
ENV puerto="443"
# Especificamos el comando para ejecutar la aplicación Java cuando se inicie el contenedor
CMD ["java", "-jar", "/app/ServidorHTTPS.jar"]