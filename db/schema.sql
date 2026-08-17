-- Esquema completo de Teleradiología. Fuente de verdad del modelo de datos.
-- Se trabaja database first: los cambios se escriben acá y se aplican con SQL,
-- no con migraciones code first de EF.
--
-- Aplicar sobre una base vacía:
--   psql -U teleradiologia -d teleradiologia -f db/schema.sql
--
-- Para una base ya existente, ver db/scripts/.

BEGIN;

CREATE TABLE IF NOT EXISTS "CatalogoEstablecimientos" (
    "Codigo"        integer                NOT NULL,
    "Nombre"        character varying(250) NOT NULL,
    "NivelAtencion" character varying(50),
    "Tipo"          character varying(80),
    "RegionSalud"   character varying(50),
    "Provincia"     character varying(80),
    "Municipio"     character varying(120),
    "AnioApertura"  integer,
    CONSTRAINT "PK_CatalogoEstablecimientos" PRIMARY KEY ("Codigo")
);

CREATE INDEX IF NOT EXISTS "IX_CatalogoEstablecimientos_Provincia" ON "CatalogoEstablecimientos" ("Provincia");
CREATE INDEX IF NOT EXISTS "IX_CatalogoEstablecimientos_Tipo" ON "CatalogoEstablecimientos" ("Tipo");

CREATE TABLE IF NOT EXISTS "Hospitales" (
    "Id"             uuid                     NOT NULL,
    "Nombre"         character varying(200)   NOT NULL,
    -- ID_CENTRO del catálogo del MISPAS. NULL en los privados.
    "CodigoExterno"  integer,
    "Provincia"      character varying(80),
    "Municipio"      character varying(120),
    "EmailContacto"  character varying(256),
    "Activo"         boolean                  NOT NULL DEFAULT true,
    "CreatedAt"      timestamp with time zone NOT NULL,
    "CreatedBy"      character varying(256),
    "LastModified"   timestamp with time zone,
    "LastModifiedBy" character varying(256),
    CONSTRAINT "PK_Hospitales" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Hospitales_CatalogoEstablecimientos_CodigoExterno" FOREIGN KEY ("CodigoExterno")
        REFERENCES "CatalogoEstablecimientos" ("Codigo") ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Hospitales_Nombre" ON "Hospitales" ("Nombre");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Hospitales_CodigoExterno" ON "Hospitales" ("CodigoExterno")
    WHERE "CodigoExterno" IS NOT NULL;

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
    "Matricula"       character varying(50),
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

-- Qué hospitales ve cada usuario. El Admin no lleva filas: ve todos.
CREATE TABLE IF NOT EXISTS "UsuarioHospitales" (
    "UsuarioId"  uuid                     NOT NULL,
    "HospitalId" uuid                     NOT NULL,
    "CreatedAt"  timestamp with time zone NOT NULL,
    "CreatedBy"  character varying(256),
    CONSTRAINT "PK_UsuarioHospitales" PRIMARY KEY ("UsuarioId", "HospitalId"),
    CONSTRAINT "FK_UsuarioHospitales_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId")
        REFERENCES "Usuarios" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UsuarioHospitales_Hospitales_HospitalId" FOREIGN KEY ("HospitalId")
        REFERENCES "Hospitales" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UsuarioHospitales_HospitalId" ON "UsuarioHospitales" ("HospitalId");

CREATE TABLE IF NOT EXISTS "Pacientes" (
    "Id"                 uuid                     NOT NULL,
    "HospitalId"         uuid                     NOT NULL,
    "NombreCompleto"     character varying(200)   NOT NULL,
    "DocumentoIdentidad" character varying(50)    NOT NULL,
    "FechaNacimiento"    date                     NOT NULL,
    "Sexo"               character varying(20)    NOT NULL,
    "CreatedAt"          timestamp with time zone NOT NULL,
    "CreatedBy"          character varying(256),
    "LastModified"       timestamp with time zone,
    "LastModifiedBy"     character varying(256),
    CONSTRAINT "PK_Pacientes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Pacientes_Hospitales_HospitalId" FOREIGN KEY ("HospitalId")
        REFERENCES "Hospitales" ("Id") ON DELETE RESTRICT
);

-- El documento es único dentro de cada hospital, no en toda la plataforma.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Pacientes_HospitalId_DocumentoIdentidad"
    ON "Pacientes" ("HospitalId", "DocumentoIdentidad");
CREATE INDEX IF NOT EXISTS "IX_Pacientes_HospitalId" ON "Pacientes" ("HospitalId");

CREATE TABLE IF NOT EXISTS "Estudios" (
    "Id"                  uuid                     NOT NULL,
    "PacienteId"          uuid                     NOT NULL,
    "OrthancStudyId"      character varying(64)    NOT NULL,
    "StudyInstanceUid"    character varying(128)   NOT NULL,
    "Modalidad"           character varying(16)    NOT NULL,
    "DescripcionEstudio"  character varying(500),
    "HospitalId"          uuid                     NOT NULL,
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
    CONSTRAINT "FK_Estudios_Hospitales_HospitalId" FOREIGN KEY ("HospitalId")
        REFERENCES "Hospitales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Estudios_Usuarios_RadiologoAsignadoId" FOREIGN KEY ("RadiologoAsignadoId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Estudios_Usuarios_SubidoPorId" FOREIGN KEY ("SubidoPorId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
);

-- Únicos por hospital: dos hospitales pueden recibir el mismo estudio y son registros distintos.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Estudios_HospitalId_OrthancStudyId" ON "Estudios" ("HospitalId", "OrthancStudyId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Estudios_HospitalId_StudyInstanceUid" ON "Estudios" ("HospitalId", "StudyInstanceUid");
CREATE INDEX IF NOT EXISTS "IX_Estudios_StudyInstanceUid" ON "Estudios" ("StudyInstanceUid");
CREATE INDEX IF NOT EXISTS "IX_Estudios_Estado" ON "Estudios" ("Estado");
CREATE INDEX IF NOT EXISTS "IX_Estudios_HospitalId" ON "Estudios" ("HospitalId");
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
    -- Firma digital. El firmante se copia acá: un documento firmado no cambia si la
    -- persona después corrige su nombre o matrícula.
    "HashContenido"     character varying(64),
    "Firma"             text,
    "AlgoritmoFirma"    character varying(30),
    "FirmanteNombre"    character varying(200),
    "FirmanteMatricula" character varying(50),
    "FirmaImagen"       text,
    "VersionFirma"      integer,
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
CREATE INDEX IF NOT EXISTS "IX_Informes_HashContenido" ON "Informes" ("HashContenido");

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

-- Bitácora de cambios sobre los datos. Distinta de AuditLogs, que registra accesos.
-- Sin FK a Usuarios: tiene que sobrevivir al borrado del usuario.
CREATE TABLE IF NOT EXISTS "Eventos" (
    "Id"           uuid                     NOT NULL,
    "Entidad"      character varying(80)    NOT NULL,
    "EntidadId"    character varying(64)    NOT NULL,
    "Operacion"    character varying(20)    NOT NULL,
    "UsuarioId"    uuid,
    "UsuarioEmail" character varying(256),
    "Cambios"      jsonb,
    "Timestamp"    timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Eventos" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Eventos_Timestamp" ON "Eventos" ("Timestamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Eventos_Entidad_Timestamp" ON "Eventos" ("Entidad", "Timestamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Eventos_UsuarioId" ON "Eventos" ("UsuarioId");
CREATE INDEX IF NOT EXISTS "IX_Eventos_Operacion" ON "Eventos" ("Operacion");

-- Notificaciones del radiólogo. Se persisten además de emitirse por SignalR: si no tenía la
-- pantalla abierta, el aviso lo tiene que encontrar al entrar.
CREATE TABLE IF NOT EXISTS "Notificaciones" (
    "Id"         uuid                     NOT NULL,
    "UsuarioId"  uuid                     NOT NULL,
    "Tipo"       character varying(40)    NOT NULL,
    "Titulo"     character varying(200)   NOT NULL,
    "Mensaje"    character varying(500)   NOT NULL,
    "EstudioId"  uuid,
    "HospitalId" uuid,
    "LeidaAt"    timestamp with time zone,
    "CreatedAt"  timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Notificaciones" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notificaciones_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId")
        REFERENCES "Usuarios" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notificaciones_Estudios_EstudioId" FOREIGN KEY ("EstudioId")
        REFERENCES "Estudios" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notificaciones_Hospitales_HospitalId" FOREIGN KEY ("HospitalId")
        REFERENCES "Hospitales" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Notificaciones_UsuarioId_CreatedAt" ON "Notificaciones" ("UsuarioId", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_Notificaciones_NoLeidas" ON "Notificaciones" ("UsuarioId") WHERE "LeidaAt" IS NULL;
CREATE INDEX IF NOT EXISTS "IX_Notificaciones_EstudioId" ON "Notificaciones" ("EstudioId");

-- Plantillas de informe por radiólogo. Las secciones van en jsonb porque siempre se leen
-- y escriben completas: no hay consulta que pida una sección suelta.
CREATE TABLE IF NOT EXISTS "PlantillasInforme" (
    "Id"              uuid                     NOT NULL,
    "RadiologoId"     uuid                     NOT NULL,
    "Nombre"          character varying(200)   NOT NULL,
    "Modalidad"       character varying(16),
    "RegionAnatomica" character varying(120),
    "Descripcion"     character varying(500),
    "Secciones"       jsonb                    NOT NULL DEFAULT '[]'::jsonb,
    "Favorita"        boolean                  NOT NULL DEFAULT false,
    "VecesUsada"      integer                  NOT NULL DEFAULT 0,
    "Activa"          boolean                  NOT NULL DEFAULT true,
    "CreatedAt"       timestamp with time zone NOT NULL,
    "CreatedBy"       character varying(256),
    "LastModified"    timestamp with time zone,
    "LastModifiedBy"  character varying(256),
    CONSTRAINT "PK_PlantillasInforme" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PlantillasInforme_Usuarios_RadiologoId" FOREIGN KEY ("RadiologoId")
        REFERENCES "Usuarios" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PlantillasInforme_RadiologoId" ON "PlantillasInforme" ("RadiologoId");
CREATE INDEX IF NOT EXISTS "IX_PlantillasInforme_Modalidad" ON "PlantillasInforme" ("Modalidad");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlantillasInforme_RadiologoId_Nombre"
    ON "PlantillasInforme" ("RadiologoId", lower("Nombre")) WHERE "Activa";

COMMIT;
