API DE PAGOS DE SERVICIOS BÁSICOS
Autor: Gabriel Luis Zeballos Azurduy

API en .NET 8 para registrar y consultar pagos de servicios básicos (agua,
luz, telecomunicaciones). Guarda los pagos en PostgreSQL o SQL Server (se elige
en la configuración) y, si está activado, publica un evento en Kafka cada vez
que se registra un pago.


1. QUÉ SE NECESITA
------------------

- Docker Desktop (o Docker Engine con Compose 2.20 o más nuevo).
- Nada más. Para correrlo sin Docker hace falta además el SDK de .NET 8.

Ojo: la imagen de SQL Server solo existe para amd64. En una Mac con chip M1/M2
funciona por emulación y tarda bastante más en arrancar.


2. CÓMO LEVANTARLO
------------------

Desde la carpeta del proyecto (la que tiene docker-compose.yml):

    cp .env.example .env
    docker compose up -d --build

En PowerShell el primer comando es: Copy-Item .env.example .env

La primera vez demora unos minutos porque baja las imágenes y compila la API.
Para saber si ya está lista:

    curl http://localhost:8080/health/ready

Tiene que responder "Healthy". Se levantan tres contenedores:

    apipagos-api-1        la API                        puerto 8080
    apipagos-postgres-1   la base (se crea sola)        puerto 5432
    apipagos-kafka-1      Kafka                         puerto 9094

Para bajar todo:                   docker compose down
Para bajar todo y borrar los datos: docker compose down -v

No hay que ejecutar la imagen apipagos-api sola (con el botón Run de Docker
Desktop o con docker run). La API necesita la base y las claves del .env; sin
eso no arranca y en el log indica qué es lo que falta. Siempre con docker
compose.


3. PROBARLO EN SWAGGER
----------------------

Swagger está en http://localhost:8080/swagger

1) Clic en "Authorize". En el campo ApiKey pegar el valor de API_KEY que está
   en el .env, después "Authorize" y "Close".
2) POST /api/payments -> "Try it out" -> "Execute". El ejemplo ya viene
   cargado y tiene que devolver 201.
3) GET /api/payments -> "Try it out" -> en customerId poner
   cfe8b150-2f84-4a1a-bdf4-923b20e34973 -> "Execute".

Para ver los rechazos basta con cambiar en el ejemplo la moneda a "USD" o el
monto a 1600.

Si se prefiere JWT en vez de API Key: en POST /api/auth/token mandar el
JWT_CLIENT_ID y el JWT_CLIENT_SECRET del .env, copiar el accessToken que
devuelve y pegarlo en "Bearer" dentro de "Authorize".

También dejé un script que prueba todo de una vez (seguridad, validaciones,
errores y cabeceras). Con la API levantada:

    bash scripts/e2e.sh

En Windows se corre desde Git Bash o WSL. Con SHOW=1 adelante muestra cada
respuesta.


4. ENDPOINTS
------------

POST /api/payments

    {
      "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
      "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
      "amount": 120.50,
      "currency": "BOB"
    }

Reglas:
- currency es obligatorio y solo se acepta BOB. Si llega USD (o cualquier
  otra moneda) se rechaza con 400.
- El monto tiene que ser mayor a 0, como máximo 1500 Bs y con dos decimales
  como mucho.
- customerId y serviceProvider son obligatorios (el proveedor, hasta 150
  caracteres).
- El pago se guarda con estado "pendiente".

Si todo está bien devuelve 201 con el pago:

    {
      "paymentId": "a248ad43-1f44-4b32-b0a0-e1c725b9bb7d",
      "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
      "amount": 120.50,
      "status": "pendiente",
      "createdAt": "2025-07-17T08:30:00Z"
    }

GET /api/payments?customerId=...

Devuelve los pagos de ese cliente, del más nuevo al más antiguo. Si no tiene
pagos devuelve una lista vacía.

Los dos endpoints piden autenticación: API Key en el header X-Api-Key, o un
token JWT (Authorization: Bearer ...) que se obtiene en POST /api/auth/token.


5. ERRORES
----------

Todos los errores vuelven en formato ProblemDetails, con el mensaje en
español y un traceId para buscarlo en los logs (docker compose logs api).
Nunca se muestra el stack trace. Por ejemplo, un pago en dólares:

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "La solicitud contiene datos inválidos.",
      "status": 400,
      "instance": "/api/payments",
      "errors": { "currency": ["No se aceptan pagos en dólares (USD)."] },
      "traceId": "0HNOS8P33TRC5:00000001"
    }

Los códigos que puede devolver:

400  no cumple una regla (USD, más de 1500, etc.), el JSON está mal armado o
     vacío, o un GUID o número tiene formato inválido.
401  falta la API Key o el token, o no son válidos. También cuando el
     clientId o el clientSecret de /api/auth/token están mal.
404  la ruta no existe.
405  método no permitido (por ejemplo DELETE).
413  el body pasa de 32 KB.
415  el Content-Type no es application/json.
429  demasiadas solicitudes: más de 100 por minuto por cliente, o más de 10
     por minuto en /api/auth/token.
503  la base de datos no responde. Cuando vuelve, la API sigue funcionando sola.
500  cualquier otro error inesperado.


6. BASE DE DATOS
----------------

El código nunca toca las tablas directamente: todo pasa por procedimientos
almacenados, y el usuario que usa la API solo tiene permiso de EXECUTE. Si
intenta hacer un SELECT a la tabla, la base se lo niega.

Procedimientos (se llaman igual en los dos motores):

    payments.sp_payment_create             inserta el pago y lo devuelve
    payments.sp_payment_get_by_customer    pagos de un cliente
    payments.sp_health_check               para el health check

En PostgreSQL están hechos como funciones, porque es la forma de que
devuelvan filas. Los scripts están en database/postgres y database/sqlserver.

Para usar SQL Server en lugar de PostgreSQL hay que cambiar estas líneas del
.env y volver a levantar (docker compose down y docker compose up -d --build):

    COMPOSE_PROFILES=sqlserver,kafka
    DB_PROVIDER=SqlServer
    DB_HOST=sqlserver
    DB_PORT=1433

Para PostgreSQL los valores son: postgres,kafka / PostgreSql / postgres / 5432.

Cómo se crea cada base:
- PostgreSQL corre los scripts solo cuando el volumen está vacío. Si se
  cambian, hay que hacer docker compose down -v.
- En SQL Server un contenedor aparte (sqlserver-init) aplica los scripts cada
  vez que se levanta y después termina. Se pueden correr varias veces sin
  problema.


7. CONFIGURACIÓN
----------------

Todo está en src/ApiPagos.Api/appsettings.json y se puede sobrescribir con
variables de entorno usando doble guion bajo (Database__Host=otro-servidor).
Con Docker, los valores salen del .env.

La conexión a la base no es un connection string fijo: se arma con los datos
de la sección Database (Provider, que puede ser PostgreSql o SqlServer, Host,
Port, Name, Username, Password, UseSsl, etc.). Si Port queda vacío se usa el
puerto por defecto (5432 o 1433).

Otras secciones:
- Kafka: Enabled, BootstrapServers, Topic (y SASL/SSL si hiciera falta).
- Security: API Keys, datos del JWT y clientes que pueden pedir token.
- RateLimiting: cuántas solicitudes por minuto se permiten.
- Swagger: Enabled (queda activo también en producción).

Al arrancar se revisa la configuración. Si falta algo, la API no levanta y en
el log aparece la lista de lo que falta, por ejemplo:

    La API no puede iniciar porque la configuración está incompleta:
      - Security:Jwt:SigningKey debe tener al menos 32 caracteres.
      - Database:Password es obligatorio.


8. SEGURIDAD
------------

- API Key o JWT en todos los endpoints de pagos.
- Usuario de base de datos que solo puede ejecutar los procedimientos.
- Límite de solicitudes por cliente, y uno más estricto para pedir tokens.
- Cabeceras de seguridad (nosniff, X-Frame-Options, CSP, etc.) y sin la
  cabecera Server.
- Body limitado a 32 KB.
- El contenedor de la API corre sin root y con el sistema de archivos en
  solo lectura.
- Las claves van en el .env, que no se sube al repositorio. Los valores del
  .env.example son de prueba y hay que cambiarlos fuera de desarrollo.


9. KAFKA
--------

Con Kafka activado, cada pago registrado se publica en el tópico
payments.registered, usando el customerId como clave. Si Kafka está caído el
pago igual se guarda: el error queda en el log y /health/ready marca Degraded.

Para ver los eventos:

    docker compose --profile tools run --rm kcat


10. SIN DOCKER
--------------

1) Levantar solo la base: docker compose up -d postgres
2) Revisar los datos de conexión en src/ApiPagos.Api/appsettings.Development.json
3) dotnet run --project src/ApiPagos.Api

Swagger queda en http://localhost:5067/swagger. En
src/ApiPagos.Api/ApiPagos.Api.http hay requests de ejemplo para Visual Studio
o VS Code.


11. TESTS
---------

    dotnet test

O dentro de Docker, sin instalar nada: docker build --target test .


12. ESTRUCTURA
--------------

    src/ApiPagos.Api              controllers, seguridad, Swagger, manejo de errores
    src/ApiPagos.Application      servicio de pagos, DTOs y validaciones
    src/ApiPagos.Domain           entidad Payment y reglas de negocio
    src/ApiPagos.Infrastructure   conexión a la base, procedimientos, Kafka
    tests/ApiPagos.UnitTests      tests unitarios (xUnit)
    database/                     scripts de PostgreSQL y SQL Server
    scripts/e2e.sh                pruebas contra la API levantada
    docs/instrucciones-bnb.txt    el enunciado original

Imágenes que usa: la API sobre aspnet:8.0-jammy-chiseled-extra (Ubuntu
mínimo, sin shell), postgres:16-alpine, apache/kafka-native (bastante más
liviana que la normal) y, si se elige, mssql/server:2022, que pesa unos 2 GB
porque Microsoft no tiene una versión más chica. Como la imagen de la API no
tiene shell, para ver qué pasa se usa docker compose logs api.


13. SI ALGO FALLA
-----------------

- "Defina ... en .env": falta copiar el .env.example a .env.
- La API se cae al arrancar: mirar docker compose logs api, ahí dice qué
  falta. Si se corrió la imagen sola, usar docker compose.
- "port is already allocated": otro programa usa ese puerto. Cambiar
  API_PORT, POSTGRES_HOST_PORT, etc. en el .env.
- /health/ready dice Unhealthy: la base todavía está arrancando o se cayó.
  Revisar con docker compose ps y docker compose logs postgres.
- Swagger devuelve 401: faltó el paso de Authorize con la API Key.
- 429: se pasó el límite de solicitudes; hay que esperar un minuto.
