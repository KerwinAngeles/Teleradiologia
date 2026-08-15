-- Esquema completo de Teleradiología. Fuente de verdad del modelo de datos.
-- Se trabaja database first: los cambios se escriben acá y se aplican con SQL,
-- no con migraciones code first de EF.
--
-- Aplicar sobre una base vacía:
--   psql -U teleradiologia -d teleradiologia -f db/schema.sql
--
-- Para una base ya existente, ver db/scripts/.

BEGIN;

CREATE TABLE IF NOT EXISTS "Usuarios" (
    "Id"              uuid                     NOT NULL,
    "NombreCompleto"  character varying(200)   NOT NULL,
    "Email"           character varying(256)   NOT NULL,
    "Rol"             character varying(20)    NOT NULL,
    "EstadoAcceso"    character varying(20)    NOT NULL,
    -- Proveedor de identidad externo. ProveedorUserId es el claim `sub` del token.
    "Proveedor"       character varying(50)    NOT NULL,
    "ProveedorUserId" character varying(128),
    "FechaDecision"   timestamp with time zone,
    "DecididoPorId"   uuid,
    "MotivoDecision"  character varying(500),
    "CreatedAt"       timestamp with time zone NOT NULL,
    "CreatedBy"       character varying(256),
    "LastModified"    timestamp with time zone,
    "LastModifiedBy"  character varying(256),
    CONSTRAINT "PK_Usuarios" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Usuarios_Usuarios_DecididoPorId" FOREIGN KEY ("DecididoPorId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_Email" ON "Usuarios" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_ProveedorUserId" ON "Usuarios" ("ProveedorUserId")
    WHERE "ProveedorUserId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_Usuarios_EstadoAcceso" ON "Usuarios" ("EstadoAcceso");

CREATE TABLE IF NOT EXISTS "Pacientes" (
    "Id"                 uuid                     NOT NULL,
    "NombreCompleto"     character varying(200)   NOT NULL,
    "DocumentoIdentidad" character varying(50)    NOT NULL,
    "FechaNacimiento"    date                     NOT NULL,
    "Sexo"               character varying(20)    NOT NULL,
    "CreatedAt"          timestamp with time zone NOT NULL,
    "CreatedBy"          character varying(256),
    "LastModified"       timestamp with time zone,
    "LastModifiedBy"     character varying(256),
    CONSTRAINT "PK_Pacientes" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Pacientes_DocumentoIdentidad" ON "Pacientes" ("DocumentoIdentidad");

CREATE TABLE IF NOT EXISTS "Estudios" (
    "Id"                  uuid                     NOT NULL,
    "PacienteId"          uuid                     NOT NULL,
    "OrthancStudyId"      character varying(64)    NOT NULL,
    "StudyInstanceUid"    character varying(128)   NOT NULL,
    "Modalidad"           character varying(16)    NOT NULL,
    "DescripcionEstudio"  character varying(500),
    "HospitalOrigen"      character varying(200)   NOT NULL,
    "FechaEstudio"        timestamp with time zone NOT NULL,
    "Estado"              character varying(20)    NOT NULL,
    "RadiologoAsignadoId" uuid,
    "SubidoPorId"         uuid                     NOT NULL,
    "CreatedAt"           timestamp with time zone NOT NULL,
    "CreatedBy"           character varying(256),
    "LastModified"        timestamp with time zone,
    "LastModifiedBy"      character varying(256),
    CONSTRAINT "PK_Estudios" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Estudios_Pacientes_PacienteId" FOREIGN KEY ("PacienteId")
        REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Estudios_Usuarios_RadiologoAsignadoId" FOREIGN KEY ("RadiologoAsignadoId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Estudios_Usuarios_SubidoPorId" FOREIGN KEY ("SubidoPorId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Estudios_OrthancStudyId" ON "Estudios" ("OrthancStudyId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Estudios_StudyInstanceUid" ON "Estudios" ("StudyInstanceUid");
CREATE INDEX IF NOT EXISTS "IX_Estudios_Estado" ON "Estudios" ("Estado");
CREATE INDEX IF NOT EXISTS "IX_Estudios_PacienteId" ON "Estudios" ("PacienteId");
CREATE INDEX IF NOT EXISTS "IX_Estudios_RadiologoAsignadoId" ON "Estudios" ("RadiologoAsignadoId");
CREATE INDEX IF NOT EXISTS "IX_Estudios_SubidoPorId" ON "Estudios" ("SubidoPorId");

CREATE TABLE IF NOT EXISTS "Informes" (
    "Id"                uuid                     NOT NULL,
    "EstudioId"         uuid                     NOT NULL,
    "RadiologoId"       uuid                     NOT NULL,
    "Contenido"         text                     NOT NULL,
    "Estado"            character varying(20)    NOT NULL,
    -- Una adenda apunta al informe firmado que corrige. NULL en el informe original.
    "InformeAnteriorId" uuid,
    "FirmadoAt"         timestamp with time zone,
    "CreatedAt"         timestamp with time zone NOT NULL,
    "CreatedBy"         character varying(256),
    "LastModified"      timestamp with time zone,
    "LastModifiedBy"    character varying(256),
    CONSTRAINT "PK_Informes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Informes_Estudios_EstudioId" FOREIGN KEY ("EstudioId")
        REFERENCES "Estudios" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Informes_Informes_InformeAnteriorId" FOREIGN KEY ("InformeAnteriorId")
        REFERENCES "Informes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Informes_Usuarios_RadiologoId" FOREIGN KEY ("RadiologoId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Informes_EstudioId" ON "Informes" ("EstudioId");
CREATE INDEX IF NOT EXISTS "IX_Informes_InformeAnteriorId" ON "Informes" ("InformeAnteriorId");
CREATE INDEX IF NOT EXISTS "IX_Informes_RadiologoId" ON "Informes" ("RadiologoId");

-- Bitácora inmutable de accesos a datos de salud. No lleva columnas de auditoría:
-- las filas nunca se modifican.
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id"          uuid                     NOT NULL,
    "UsuarioId"   uuid                     NOT NULL,
    "EstudioId"   uuid,
    "Accion"      character varying(30)    NOT NULL,
    "Timestamp"   timestamp with time zone NOT NULL,
    "DireccionIp" character varying(45),
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Estudios_EstudioId" FOREIGN KEY ("EstudioId")
        REFERENCES "Estudios" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AuditLogs_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EstudioId_Timestamp" ON "AuditLogs" ("EstudioId", "Timestamp");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UsuarioId" ON "AuditLogs" ("UsuarioId");

COMMIT;
