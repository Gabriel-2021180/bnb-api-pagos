# API de Pagos de Servicios Básicos

API en .NET 8 para registrar y consultar pagos de servicios básicos (agua, electricidad, telecomunicaciones).

**Autor:** Gabriel Luis Zeballos Azurduy

- Arquitectura por capas (Clean Architecture): `Api` → `Application` → `Domain`, con `Infrastructure` como implementación de los puertos.
- Acceso a datos **solo por procedimientos almacenados** (Dapper). El usuario de base de datos de la aplicación únicamente tiene permiso `EXECUTE`; no puede leer ni escribir tablas directamente.
- Motor de base de datos elegible por configuración: **PostgreSQL** o **SQL Server**.
- Publicación opcional de eventos en **Kafka** al registrar un pago.
- Seguridad con **API Key** o **JWT Bearer**, rate limiting y cabeceras de seguridad.
- Swagger, health checks y tests unitarios (xUnit).

## Contenido

```
├─ src/
│  ├─ ApiPagos.Api/             Controllers, seguridad, Swagger, middleware, Program.cs
│  ├─ ApiPagos.Application/     Servicios, DTOs, validaciones (FluentValidation), contratos
│  ├─ ApiPagos.Domain/          Entidad Payment y reglas de negocio
│  └─ ApiPagos.Infrastructure/  Conexión dinámica, ejecutor de SPs, repositorio, Kafka, health checks
├─ tests/ApiPagos.UnitTests/
├─ database/
│  ├─ postgres/                 Script de inicio + tablas, funciones y permisos
│  └─ sqlserver/                Script de inicio + base, tablas, procedimientos y permisos
├─ scripts/e2e.sh              Pruebas end-to-end contra la API levantada
├─ docs/instrucciones-bnb.txt   Enunciado original
├─ Dockerfile
├─ docker-compose.yml
└─ .env.example
```

## Requisitos

- Docker Desktop (o Docker Engine) con Docker Compose v2.20 o superior.
- Opcional, para ejecutar sin Docker: .NET SDK 8 o superior.

> SQL Server solo publica imágenes `linux/amd64`. En equipos ARM (Apple Silicon) corre por emulación y tarda más en iniciar.

## Inicio rápido (Docker)

Desde la carpeta raíz del proyecto (donde está `docker-compose.yml`):

```bash
# 1. Crear el archivo de configuración (solo la primera vez)
cp .env.example .env            # PowerShell: Copy-Item .env.example .env

# 2. Construir y levantar todo
docker compose up -d --build

# 3. Verificar que esté listo (debe responder "Healthy")
curl http://localhost:8080/health/ready
```

La primera vez tarda unos minutos porque descarga las imágenes y compila la API. Se levantan tres contenedores:

| Contenedor            | Qué es                                        | Puerto en el host |
|-----------------------|-----------------------------------------------|-------------------|
| `apipagos-api-1`      | La API (.NET 8)                               | 8080              |
| `apipagos-postgres-1` | Base de datos (se crea sola la primera vez)   | 5432              |
| `apipagos-kafka-1`    | Broker de eventos                             | 9094              |

| Recurso      | URL                                      |
|--------------|------------------------------------------|
| Swagger      | http://localhost:8080/swagger            |
| Health live  | http://localhost:8080/health/live        |
| Health ready | http://localhost:8080/health/ready       |

Para detener todo: `docker compose down` (agregar `-v` para borrar también los datos).

> **Importante:** no ejecute la imagen `apipagos-api` sola (por ejemplo con el botón *Run* de Docker Desktop o con `docker run`). La API necesita la base de datos y las credenciales del `.env`; sin ellas se detiene al iniciar y muestra en el log qué configuración falta. Use siempre `docker compose`.

### Probar en Swagger

1. Abrir http://localhost:8080/swagger.
2. Clic en **Authorize** y, en **ApiKey**, pegar el valor de `API_KEY` del `.env`. Luego *Authorize* y *Close*.
   - Alternativa con JWT: en `POST /api/auth/token` usar *Try it out* con `JWT_CLIENT_ID` / `JWT_CLIENT_SECRET` del `.env`, copiar el `accessToken` y pegarlo en **Bearer** dentro de *Authorize*.
3. En `POST /api/payments` → *Try it out* → *Execute*. El ejemplo ya viene cargado y devuelve **201**.
4. En `GET /api/payments` → *Try it out* → `customerId` = `cfe8b150-2f84-4a1a-bdf4-923b20e34973` → *Execute*.

Para ver los rechazos, cambiar en el ejemplo `currency` a `"USD"` o `amount` a `1600`.

### Probar todo automáticamente

Con la API levantada, este script ejecuta 41 verificaciones (seguridad, reglas de negocio, errores y cabeceras) con las credenciales del `.env`:

```bash
bash scripts/e2e.sh           # SHOW=1 bash scripts/e2e.sh muestra cada respuesta
```

En Windows se ejecuta desde Git Bash o WSL.

### Elegir el motor de base de datos

Todo se controla desde `.env`:

| Motor      | `COMPOSE_PROFILES`   | `DB_PROVIDER` | `DB_HOST`   | `DB_PORT` |
|------------|----------------------|---------------|-------------|-----------|
| PostgreSQL | `postgres,kafka`     | `PostgreSql`  | `postgres`  | `5432`    |
| SQL Server | `sqlserver,kafka`    | `SqlServer`   | `sqlserver` | `1433`    |

Sin Kafka: quitar `kafka` de `COMPOSE_PROFILES` y poner `KAFKA_ENABLED=false`.

Al cambiar de motor:

```bash
docker compose down
docker compose up -d --build
```

Para borrar también los datos: `docker compose down -v`.

### Imágenes y tamaño

| Servicio   | Imagen                                        | Notas |
|------------|-----------------------------------------------|-------|
| API        | `aspnet:8.0-jammy-chiseled-extra` (propia)    | Ubuntu mínimo, sin shell ni gestor de paquetes, usuario no root. Se publica solo para la arquitectura de destino (amd64 o arm64). |
| PostgreSQL | `postgres:16-alpine`                          | Variante oficial más liviana. |
| Kafka      | `apache/kafka-native:3.8.0`                   | Versión compilada de forma nativa: ~185 MB frente a ~594 MB de la JVM, y arranca en segundos. Pensada para desarrollo y pruebas. |
| SQL Server | `mssql/server:2022-latest`                    | ~2.3 GB. Microsoft no ofrece una versión reducida; solo se descarga si se elige el perfil `sqlserver`. |

Como la imagen de la API no tiene shell, para depurarla se usa `docker compose logs api` o los endpoints `/health/*`.

### Cómo se inicializa cada base

- **PostgreSQL**: la imagen oficial ejecuta `database/postgres/00-init.sh` solo cuando el volumen está vacío. Si se cambian los scripts, hay que recrear el volumen (`docker compose down -v`).
- **SQL Server**: el servicio `sqlserver-init` corre `database/sqlserver/init.sh` en cada arranque y termina. Los scripts son idempotentes.

En ambos casos se crea el usuario `APP_DB_USER` con permiso solo de ejecución sobre el esquema `payments`.

## Ejecutar sin Docker

1. Tener PostgreSQL o SQL Server accesible y aplicar los scripts de `database/`, o levantar solo la base con Docker:
   ```bash
   docker compose up -d postgres
   ```
2. Revisar `src/ApiPagos.Api/appsettings.Development.json` (host, usuario y contraseña de la base).
3. Ejecutar:
   ```bash
   dotnet run --project src/ApiPagos.Api
   ```
   Swagger queda en http://localhost:5067/swagger.

## Configuración

La conexión se arma a partir de piezas, no de una cadena fija. Cualquier valor puede sobreescribirse por variable de entorno usando `__` (por ejemplo `Database__Host=mi-servidor`).

```json
"Database": {
  "Provider": "PostgreSql",          // PostgreSql | SqlServer
  "Host": "localhost",
  "Port": null,                      // null = 5432 o 1433 según el proveedor
  "Name": "apipagos",
  "Username": "apipagos_app",
  "Password": "",
  "UseSsl": false,
  "TrustServerCertificate": true,
  "Pooling": true,
  "MaxPoolSize": 100,
  "ConnectTimeoutSeconds": 15,
  "CommandTimeoutSeconds": 30
}
```

| Sección        | Uso                                                                  |
|----------------|----------------------------------------------------------------------|
| `Kafka`        | `Enabled`, `BootstrapServers`, `Topic`, SASL/SSL opcional            |
| `Security`     | API Keys, JWT (clave de firma, emisor, audiencia, clientes), CORS    |
| `RateLimiting` | Solicitudes por ventana (general y para `/api/auth/token`)           |
| `Swagger`      | `Enabled` (activo también en producción)                             |

La configuración se valida al iniciar. Si algo falta o es inválido, la API no arranca y el log lista todos los problemas juntos, por ejemplo:

```
crit: ApiPagos.Startup[0]
      La API no puede iniciar porque la configuración está incompleta:
        - Security:Jwt:SigningKey debe tener al menos 32 caracteres.
        - Database:Password es obligatorio.
```

## Seguridad

Todos los endpoints de `/api/payments` requieren **una** de estas dos credenciales:

- **API Key** en el header `X-Api-Key`.
- **JWT Bearer** obtenido en `POST /api/auth/token`:
  ```bash
  curl -X POST http://localhost:8080/api/auth/token \
    -H "Content-Type: application/json" \
    -d '{"clientId":"default-client","clientSecret":"<JWT_CLIENT_SECRET>"}'
  ```

En Swagger se usa el botón **Authorize**.

Otras medidas:

- Usuario de base de datos con permiso solo `EXECUTE`. En PostgreSQL las funciones son `SECURITY DEFINER` con `search_path` fijo; en SQL Server se niega `SELECT/INSERT/UPDATE/DELETE` sobre el esquema.
- Comparación de secretos en tiempo constante.
- Rate limiting por cliente (y más estricto en el endpoint de token).
- Cabeceras `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`; sin cabecera `Server`.
- Límite de tamaño de body (32 KB, configurable en `RequestLimits:MaxBodySizeBytes`).
- Errores en formato ProblemDetails sin exponer detalles internos (incluyen `traceId`).
- Contenedor con usuario no root, sistema de archivos de solo lectura y sin capabilities.
- Los secretos van en `.env` (no se versiona). **Cambiar todos los valores de `.env.example` fuera de desarrollo.**

## Endpoints

### `POST /api/payments`

```json
{
  "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
  "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
  "amount": 120.50,
  "currency": "BOB"
}
```

Reglas:

- `currency` es obligatorio y solo se acepta `BOB`. Montos en dólares (`USD`) u otra moneda → `400`.
- `amount` mayor a 0, como máximo **1500 Bs** y con hasta 2 decimales → si no, `400`.
- `customerId` y `serviceProvider` (máx. 150 caracteres) obligatorios.
- El pago se guarda con estado `pendiente`.

Respuesta `201 Created`:

```json
{
  "paymentId": "a248ad43-1f44-4b32-b0a0-e1c725b9bb7d",
  "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
  "amount": 120.50,
  "status": "pendiente",
  "createdAt": "2025-07-17T08:30:00Z"
}
```

Ejemplo de rechazo (`400`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "La solicitud contiene datos inválidos.",
  "status": 400,
  "instance": "/api/payments",
  "errors": { "currency": ["No se aceptan pagos en dólares (USD)."] },
  "traceId": "0HNOS8P33TRC5:00000001"
}
```

### `GET /api/payments?customerId=...`

Devuelve los pagos del cliente, del más reciente al más antiguo (lista vacía si no tiene).

### Errores

Todas las respuestas de error usan el formato estándar **ProblemDetails** (RFC 9457), con mensajes en español y un `traceId` para buscar el detalle en `docker compose logs api`. Nunca se exponen stack traces ni detalles internos.

| Código | Cuándo ocurre | Ejemplo de mensaje |
|--------|---------------|--------------------|
| `400` | Regla de negocio o validación | `No se aceptan pagos en dólares (USD).` / `El monto no puede superar 1500 Bs.` |
| `400` | JSON mal formado, GUID o número con formato inválido | `Valor o formato inválido.` |
| `400` | Body vacío | `El cuerpo de la solicitud es obligatorio y debe ser un JSON válido.` |
| `401` | Sin credenciales, API Key incorrecta, JWT inválido o vencido | `Envíe una API Key válida en el header X-Api-Key o un token Bearer válido.` |
| `401` | `clientId` / `clientSecret` incorrectos en `/api/auth/token` | `Credenciales de cliente inválidas.` |
| `404` | Ruta inexistente | `La ruta solicitada no existe.` |
| `405` | Método HTTP no soportado (ej. `DELETE`) | `El método HTTP no está permitido para esta ruta.` |
| `413` | Body mayor a 32 KB | `El cuerpo de la solicitud supera el tamaño máximo permitido.` |
| `415` | `Content-Type` distinto de `application/json` | `Envíe el cuerpo con Content-Type: application/json.` |
| `429` | Más de 100 solicitudes/min por cliente (10/min en `/api/auth/token`) | `Se superó el límite de solicitudes.` |
| `503` | Base de datos caída o inalcanzable | `No se pudo acceder a la base de datos.` |
| `500` | Error no previsto | `Intente nuevamente... contacte a soporte con el traceId.` |

### Evento Kafka

Con `Kafka:Enabled=true`, cada pago registrado publica en el tópico `payments.registered` (clave = `customerId`):

```json
{
  "eventId": "...",
  "eventType": "payment.registered",
  "occurredAt": "...",
  "paymentId": "...",
  "customerId": "...",
  "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
  "amount": 120.50,
  "currency": "BOB",
  "status": "pendiente"
}
```

Si Kafka no está disponible, el pago igual se registra, el error queda en el log y `/health/ready` informa `Degraded`.

Para ver los eventos publicados (usa [kcat](https://github.com/edenhill/kcat), que solo se descarga si se ejecuta este comando):

```bash
docker compose --profile tools run --rm kcat
```

## Procedimientos almacenados

| Procedimiento                        | Descripción                        |
|--------------------------------------|------------------------------------|
| `payments.sp_payment_create`         | Inserta el pago y lo devuelve      |
| `payments.sp_payment_get_by_customer`| Pagos de un cliente, por fecha     |
| `payments.sp_health_check`           | Verificación de conectividad       |

Los nombres y parámetros son iguales en ambos motores. En PostgreSQL se implementan como funciones, porque así se devuelven filas.

## Tests

```bash
dotnet test
```

O dentro de Docker, sin instalar el SDK:

```bash
docker build --target test .
```

El archivo `src/ApiPagos.Api/ApiPagos.Api.http` trae ejemplos de solicitudes para Visual Studio o VS Code (REST Client).

## Solución de problemas

| Síntoma | Causa y solución |
|---------|------------------|
| `docker compose` dice `Defina ... en .env` | Falta el archivo `.env`: `cp .env.example .env`. |
| El contenedor de la API se detiene al iniciar | Ver `docker compose logs api`: el mensaje lista la configuración que falta. Si se ejecutó la imagen sola, usar `docker compose up -d --build`. |
| `port is already allocated` | Otro programa usa el puerto (8080, 5432, 9094 o 1433). Cambiar `API_PORT`, `POSTGRES_HOST_PORT`, etc. en `.env`. |
| `/health/ready` responde `Unhealthy` | La base de datos todavía está iniciando o se detuvo: `docker compose ps` y `docker compose logs postgres`. |
| Los cambios en `database/postgres` no se aplican | Esos scripts solo corren con el volumen vacío: `docker compose down -v` y levantar de nuevo. |
| Swagger responde `401` | Falta hacer clic en **Authorize** con la API Key del `.env`. |
| Error `429` | Se alcanzó el límite de solicitudes; esperar un minuto. |
