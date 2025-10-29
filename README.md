# MesaListo - Sistema de Reservas de Restaurantes

Sistema web para gestión de reservas en restaurantes con roles de Admin, Restaurante y Cliente.

##  Características

- Autenticación con ASP.NET Core Identity
- Tres roles: Admin, Restaurante, Cliente  
- CRUD completo de Restaurantes y Mesas
- Sistema de reservas con validación de disponibilidad
- PostgreSQL con Neon
- Docker configurado

## 🛠️ Tecnologías

- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- Bootstrap 5
- Docker

## Instalación

1. Clonar repositorio
2. Configurar connection string en `appsettings.json`
3. Ejecutar migraciones: `dotnet ef database update`
4. Ejecutar: `docker-compose up`

## Usuarios de Prueba

- **Admin**: `admin@mesalisto.com` / `Admin123!`
- **Restaurante**: `restaurante@ejemplo.com` / `Rest123!`
- **Cliente**: `cliente1@gmail.com` / `123456`

## Docker

```bash
docker-compose build
docker-compose up