# 🚀 Sistema de Carga Masiva - Microservicios .NET 8

Sistema de carga masiva de archivos Excel implementado con arquitectura de microservicios en .NET 8.

## 📋 Arquitectura

```
┌─────────────────┐
│    GATEWAY      │ ← Rate Limiting + JWT Validation
│   (Port 5100)   │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌───────┐ ┌────────┐
│ AUTH  │ │CONTROL │
│ API   │ │  API   │
│(5101) │ │ (5102) │
└───┬───┘ └───┬────┘
    │         │
    ▼         ▼
┌─────────────────┐     ┌─────────────┐
│   SQL Server    │     │  RabbitMQ   │
│    (1433)       │     │(5672/15672) │
└─────────────────┘     └──────┬──────┘
                               │
                    ┌──────────┴──────────┐
                    │                     │
                    ▼                     ▼
            ┌─────────────┐       ┌─────────────┐
            │  BULKLOAD   │──────►│NOTIFICATIONS│
            │   WORKER    │       │   WORKER    │
            └─────────────┘       └─────────────┘
```

## 🛠️ Tecnologías

- **.NET 8** - Framework principal
- **SQL Server 2022** - Base de datos
- **RabbitMQ** - Cola de mensajes
- **SeaweedFS** - Almacenamiento de archivos
- **Docker** - Contenedores
- **Dapper** - Micro ORM
- **Polly** - Circuit Breaker y Retry Policies
- **JWT** - Autenticación
- **YARP** - Reverse Proxy (Gateway)

## ✅ Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) con WSL2
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (opcional, para ver la BD)

## 🚀 Pasos para levantar el proyecto

### Paso 1: Clonar el repositorio

```bash
git clone <URL_DEL_REPOSITORIO>
cd NET8
```

### Paso 2: Levantar los contenedores

```bash
docker-compose up -d
```

Espera aproximadamente 1-2 minutos para que todos los servicios inicien.

### Paso 3: Verificar que los contenedores estén corriendo

```bash
docker ps
```

Deberías ver estos contenedores:
- `bulkupload-sqlserver`
- `bulkupload-rabbitmq`
- `bulkupload-seaweedfs-master`
- `bulkupload-seaweedfs-volume`
- `bulkupload-auth-api`
- `bulkupload-control-api`
- `bulkupload-bulkload-worker`
- `bulkupload-notifications-worker`

### Paso 4: Crear la base de datos

```bash
docker exec -it bulkupload-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Passw0rd" -C -Q "CREATE DATABASE bulkupload"
```

### Paso 5: Ejecutar el script SQL

Conecta SSMS a:
- **Server:** `localhost,1433`
- **User:** `sa`
- **Password:** `Passw0rd`

Ejecuta el script: `scripts/init-db-sqlserver.sql`

### Paso 6: ¡Listo! Accede a las APIs

| Servicio | URL |
|----------|-----|
| Auth API (Swagger) | http://localhost:5101/swagger |
| Control API (Swagger) | http://localhost:5102/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |

## 🧪 Probar el sistema

### 1. Login

En Swagger Auth API (`http://localhost:5101/swagger`):

**POST** `/api/auth/login`
```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

Copia el token JWT de la respuesta.

### 2. Subir archivo Excel

En Swagger Control API (`http://localhost:5102/swagger`):

1. Click en **Authorize** (candado)
2. Pega el token JWT
3. **POST** `/api/carga/upload` → Sube un archivo Excel

### 3. Formato del Excel

El archivo Excel debe tener estas columnas:

| CodigoProducto | Descripcion | Cantidad | PrecioUnitario | Categoria | Periodo |
|----------------|-------------|----------|----------------|-----------|---------|
| PROD001 | Laptop HP | 10 | 899.99 | Tecnología | 2024-01 |
| PROD002 | Mouse | 50 | 29.99 | Accesorios | 2024-01 |

### 4. Consultar estado

**GET** `/api/carga/{id}`

## 👤 Usuarios de prueba

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin | Admin123! | Admin |
| user1 | Admin123! | User |

## 📦 Patrones implementados

- ✅ **Rate Limiting** - 100 requests/minuto por usuario
- ✅ **Circuit Breaker** - Polly (5 fallos → 30s abierto)
- ✅ **Retry Policy** - 3 reintentos con espera exponencial
- ✅ **Repository Pattern** - Dapper
- ✅ **CQRS** - Separación de comandos y consultas
- ✅ **Message Queue** - RabbitMQ

## 🛑 Detener el proyecto

```bash
docker-compose down
```

Para eliminar también los volúmenes (datos):
```bash
docker-compose down -v
```

## 📁 Estructura del proyecto

```
NET8/
├── src/
│   ├── Gateway/
│   │   └── Gateway.Api/          # API Gateway con YARP
│   ├── Services/
│   │   ├── Auth/                 # Microservicio de autenticación
│   │   ├── Control/              # Microservicio de control de cargas
│   │   ├── BulkLoad/             # Worker de procesamiento
│   │   └── Notifications/        # Worker de notificaciones
│   └── Shared/                   # Librerías compartidas
├── scripts/
│   └── init-db-sqlserver.sql     # Script de inicialización BD
└── docker-compose.yml            # Orquestación de contenedores
```

## 📝 Licencia

MIT
