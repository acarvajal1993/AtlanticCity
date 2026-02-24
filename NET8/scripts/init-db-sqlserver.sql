-- =============================================================
-- Script de inicialización de base de datos - Sistema de Carga Masiva
-- SQL Server
-- Base de datos: bulkupload
-- =============================================================

-- Crear la base de datos (ejecutar primero por separado si no existe)
-- CREATE DATABASE bulkupload;
-- GO
USE bulkupload;
-- GO

-- =============================================================
-- TABLA: Users (Autenticación)
-- =============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'User',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastLoginAt DATETIME2 NULL,
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );

    CREATE INDEX IX_Users_Username ON Users(Username);
    CREATE INDEX IX_Users_Email ON Users(Email);
END
GO

-- Usuario de prueba (password: Admin123!)
-- Hash: SHA256("Admin123!") = hs9lOEyYGmQ3xO9S0AxC3VTyRYIDAX6H5uXBr2Wf6FI=
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive)
    VALUES ('admin', 'admin@example.com', 'PrP+ZrMeO00Q+nC1ytSccRIpSvauTkdqHEBRVdRaoSE=', 'Admin', 1);
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'user1')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive)
    VALUES ('user1', 'user1@example.com', 'PrP+ZrMeO00Q+nC1ytSccRIpSvauTkdqHEBRVdRaoSE=', 'User', 1);
END
GO

-- =============================================================
-- TABLA: CargaArchivo (Trazabilidad de cargas)
-- =============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CargaArchivo')
BEGIN
    CREATE TABLE CargaArchivo (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NombreArchivo NVARCHAR(500) NOT NULL,
        Usuario NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Periodo NVARCHAR(50) NULL,
        FechaRegistro DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Estado NVARCHAR(50) NOT NULL DEFAULT 'Pendiente',
        FechaFin DATETIME2 NULL,
        RutaArchivo NVARCHAR(500) NULL,
        TamanoArchivo BIGINT NOT NULL DEFAULT 0,
        MensajeError NVARCHAR(MAX) NULL,
        TotalRegistros INT NOT NULL DEFAULT 0,
        RegistrosProcesados INT NOT NULL DEFAULT 0,
        RegistrosFallidos INT NOT NULL DEFAULT 0
    );

    CREATE INDEX IX_CargaArchivo_Usuario ON CargaArchivo(Usuario);
    CREATE INDEX IX_CargaArchivo_Estado ON CargaArchivo(Estado);
    CREATE INDEX IX_CargaArchivo_Periodo ON CargaArchivo(Periodo);
    CREATE INDEX IX_CargaArchivo_FechaRegistro ON CargaArchivo(FechaRegistro DESC);
END
GO

-- =============================================================
-- TABLA: DataProcesada (Datos extraídos del Excel)
-- =============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DataProcesada')
BEGIN
    CREATE TABLE DataProcesada (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IdCarga INT NOT NULL,
        CodigoProducto NVARCHAR(100) NOT NULL,
        Descripcion NVARCHAR(500) NOT NULL,
        Cantidad DECIMAL(18,4) NOT NULL DEFAULT 0,
        PrecioUnitario DECIMAL(18,4) NOT NULL DEFAULT 0,
        Categoria NVARCHAR(200) NULL,
        Periodo NVARCHAR(50) NOT NULL,
        FechaProcesamiento DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_DataProcesada_CargaArchivo FOREIGN KEY (IdCarga) 
            REFERENCES CargaArchivo(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_DataProcesada_IdCarga ON DataProcesada(IdCarga);
    CREATE INDEX IX_DataProcesada_CodigoProducto ON DataProcesada(CodigoProducto);
    CREATE INDEX IX_DataProcesada_Periodo ON DataProcesada(Periodo);
    CREATE UNIQUE INDEX IX_DataProcesada_CodigoProducto_Unique ON DataProcesada(CodigoProducto);
END
GO

-- =============================================================
-- TABLA: AuditoriaFallidos (Registro de filas fallidas)
-- =============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditoriaFallidos')
BEGIN
    CREATE TABLE AuditoriaFallidos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IdCarga INT NOT NULL,
        NumeroFila INT NOT NULL,
        CodigoProducto NVARCHAR(100) NULL,
        DatosOriginales NVARCHAR(MAX) NOT NULL,
        MotivoError NVARCHAR(500) NOT NULL,
        FechaRegistro DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_AuditoriaFallidos_CargaArchivo FOREIGN KEY (IdCarga) 
            REFERENCES CargaArchivo(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AuditoriaFallidos_IdCarga ON AuditoriaFallidos(IdCarga);
    CREATE INDEX IX_AuditoriaFallidos_FechaRegistro ON AuditoriaFallidos(FechaRegistro DESC);
END
GO

-- =============================================================
-- PROCEDIMIENTOS ALMACENADOS
-- =============================================================

-- Procedimiento: Actualizar estado de carga
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_ActualizarEstadoCarga')
    DROP PROCEDURE sp_ActualizarEstadoCarga;
GO

CREATE PROCEDURE sp_ActualizarEstadoCarga
    @IdCarga INT,
    @Estado NVARCHAR(50),
    @MensajeError NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE CargaArchivo 
    SET Estado = @Estado,
        MensajeError = ISNULL(@MensajeError, MensajeError),
        FechaFin = CASE 
            WHEN @Estado IN ('Finalizado', 'Notificado', 'Error', 'Rechazado') 
            THEN GETUTCDATE() 
            ELSE FechaFin 
        END
    WHERE Id = @IdCarga;
END
GO

-- Procedimiento: Obtener resumen de cargas por usuario
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_ResumenCargasUsuario')
    DROP PROCEDURE sp_ResumenCargasUsuario;
GO

CREATE PROCEDURE sp_ResumenCargasUsuario
    @Usuario NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) AS TotalCargas,
        SUM(CASE WHEN Estado IN ('Finalizado', 'Notificado') THEN 1 ELSE 0 END) AS CargasExitosas,
        SUM(CASE WHEN Estado IN ('Error', 'Rechazado') THEN 1 ELSE 0 END) AS CargasFallidas,
        ISNULL(SUM(RegistrosProcesados), 0) AS RegistrosProcesados,
        ISNULL(SUM(RegistrosFallidos), 0) AS RegistrosFallidos
    FROM CargaArchivo
    WHERE Usuario = @Usuario;
END
GO

-- Procedimiento: Verificar duplicidad de periodo
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_VerificarPeriodoExistente')
    DROP PROCEDURE sp_VerificarPeriodoExistente;
GO

CREATE PROCEDURE sp_VerificarPeriodoExistente
    @Periodo NVARCHAR(50),
    @IdCargaActual INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CAST(CASE WHEN EXISTS(
            SELECT 1 FROM CargaArchivo 
            WHERE Periodo = @Periodo 
            AND Estado IN ('Pendiente', 'EnProceso')
            AND (@IdCargaActual IS NULL OR Id != @IdCargaActual)
        ) THEN 1 ELSE 0 END AS BIT) AS ExisteActiva,
        CAST(CASE WHEN EXISTS(
            SELECT 1 FROM CargaArchivo 
            WHERE Periodo = @Periodo 
            AND Estado IN ('Cargado', 'Finalizado', 'Notificado')
        ) THEN 1 ELSE 0 END AS BIT) AS ExisteFinalizada;
END
GO

-- Procedimiento: Insertar carga de archivo
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertarCargaArchivo')
    DROP PROCEDURE sp_InsertarCargaArchivo;
GO

CREATE PROCEDURE sp_InsertarCargaArchivo
    @NombreArchivo NVARCHAR(500),
    @Usuario NVARCHAR(200),
    @Email NVARCHAR(200),
    @TamanoArchivo BIGINT,
    @IdCarga INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO CargaArchivo (NombreArchivo, Usuario, Email, TamanoArchivo, Estado, FechaRegistro)
    VALUES (@NombreArchivo, @Usuario, @Email, @TamanoArchivo, 'Pendiente', GETUTCDATE());
    
    SET @IdCarga = SCOPE_IDENTITY();
END
GO

-- =============================================================
-- VISTAS
-- =============================================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ResumenCargas')
    DROP VIEW vw_ResumenCargas;
GO

CREATE VIEW vw_ResumenCargas AS
SELECT 
    c.Id,
    c.NombreArchivo,
    c.Usuario,
    c.Email,
    c.Periodo,
    c.Estado,
    c.FechaRegistro,
    c.FechaFin,
    c.TotalRegistros,
    c.RegistrosProcesados,
    c.RegistrosFallidos,
    CASE 
        WHEN c.TotalRegistros > 0 
        THEN CAST(ROUND((CAST(c.RegistrosProcesados AS DECIMAL) / c.TotalRegistros) * 100, 2) AS DECIMAL(5,2))
        ELSE 0 
    END AS PorcentajeExito
FROM CargaArchivo c;
GO

-- =============================================================
-- MENSAJE DE CONFIRMACIÓN
-- =============================================================
PRINT 'Base de datos inicializada correctamente';
GO
