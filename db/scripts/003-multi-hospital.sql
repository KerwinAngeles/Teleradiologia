-- Multi-hospital: el hospital deja de ser un texto libre en Estudios y pasa a ser la unidad
-- de aislamiento. Cada estudio y cada paciente pertenecen a un hospital, y un usuario solo
-- ve los hospitales a los que está habilitado.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/003-multi-hospital.sql

BEGIN;

-- Catálogo de referencia del MISPAS/SNS (datos.gob.do, licencia ODbL). No son inquilinos:
-- es la lista de la que un Admin elige al dar de alta un hospital. Se puebla con el script 004.
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

-- Los hospitales que efectivamente usan la plataforma.
CREATE TABLE IF NOT EXISTS "Hospitales" (
    "Id"             uuid                     NOT NULL,
    "Nombre"         character varying(200)   NOT NULL,
    -- ID_CENTRO del catálogo. NULL en los privados, que no figuran en el listado público.
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

-- Qué hospitales puede ver cada usuario. Un técnico suele tener uno; un radiólogo lee para
-- varios, que es justamente el sentido de la plataforma. El Admin no lleva filas: ve todo.
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

-- Un hospital por cada valor distinto que había en el texto libre.
INSERT INTO "Hospitales" ("Id", "Nombre", "Activo", "CreatedAt")
SELECT gen_random_uuid(), e."HospitalOrigen", true, now()
FROM (SELECT DISTINCT "HospitalOrigen" FROM "Estudios") e
WHERE NOT EXISTS (SELECT 1 FROM "Hospitales" h WHERE h."Nombre" = e."HospitalOrigen");

ALTER TABLE "Estudios" ADD COLUMN IF NOT EXISTS "HospitalId" uuid;

UPDATE "Estudios" e
SET "HospitalId" = h."Id"
FROM "Hospitales" h
WHERE h."Nombre" = e."HospitalOrigen" AND e."HospitalId" IS NULL;

ALTER TABLE "Estudios" ALTER COLUMN "HospitalId" SET NOT NULL;
ALTER TABLE "Estudios" ADD CONSTRAINT "FK_Estudios_Hospitales_HospitalId"
    FOREIGN KEY ("HospitalId") REFERENCES "Hospitales" ("Id") ON DELETE RESTRICT;
CREATE INDEX IF NOT EXISTS "IX_Estudios_HospitalId" ON "Estudios" ("HospitalId");
ALTER TABLE "Estudios" DROP COLUMN "HospitalOrigen";

-- El paciente pasa a ser por hospital. Compartirlo entre inquilinos dejaría que un hospital
-- deduzca que su paciente se atendió en otro lado, que es exactamente lo que hay que evitar.
ALTER TABLE "Pacientes" ADD COLUMN IF NOT EXISTS "HospitalId" uuid;

UPDATE "Pacientes" p
SET "HospitalId" = primero."HospitalId"
FROM (
    SELECT DISTINCT ON (e."PacienteId") e."PacienteId", e."HospitalId"
    FROM "Estudios" e
    ORDER BY e."PacienteId", e."FechaEstudio", e."CreatedAt"
) primero
WHERE primero."PacienteId" = p."Id" AND p."HospitalId" IS NULL;

-- Se suelta antes del desdoblamiento: si no, la segunda ficha del mismo documento choca.
DROP INDEX IF EXISTS "IX_Pacientes_DocumentoIdentidad";

-- Un paciente que ya tenía estudios en más de un hospital se desdobla: una ficha por hospital.
CREATE TEMP TABLE pacientes_a_desdoblar ON COMMIT DROP AS
SELECT DISTINCT
    e."PacienteId"       AS "PacienteOriginalId",
    e."HospitalId"       AS "HospitalId",
    gen_random_uuid()    AS "PacienteNuevoId"
FROM "Estudios" e
JOIN "Pacientes" p ON p."Id" = e."PacienteId"
WHERE e."HospitalId" <> p."HospitalId";

INSERT INTO "Pacientes" ("Id", "NombreCompleto", "DocumentoIdentidad", "FechaNacimiento", "Sexo", "HospitalId", "CreatedAt")
SELECT d."PacienteNuevoId", p."NombreCompleto", p."DocumentoIdentidad", p."FechaNacimiento", p."Sexo", d."HospitalId", p."CreatedAt"
FROM pacientes_a_desdoblar d
JOIN "Pacientes" p ON p."Id" = d."PacienteOriginalId";

UPDATE "Estudios" e
SET "PacienteId" = d."PacienteNuevoId"
FROM pacientes_a_desdoblar d
WHERE e."PacienteId" = d."PacienteOriginalId" AND e."HospitalId" = d."HospitalId";

-- Pacientes sin ningún estudio (no debería haber): al primer hospital, para poder exigir NOT NULL.
UPDATE "Pacientes"
SET "HospitalId" = (SELECT "Id" FROM "Hospitales" ORDER BY "Nombre" LIMIT 1)
WHERE "HospitalId" IS NULL;

ALTER TABLE "Pacientes" ALTER COLUMN "HospitalId" SET NOT NULL;
ALTER TABLE "Pacientes" ADD CONSTRAINT "FK_Pacientes_Hospitales_HospitalId"
    FOREIGN KEY ("HospitalId") REFERENCES "Hospitales" ("Id") ON DELETE RESTRICT;

-- El documento es único dentro de cada hospital, no en toda la plataforma.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Pacientes_HospitalId_DocumentoIdentidad"
    ON "Pacientes" ("HospitalId", "DocumentoIdentidad");
CREATE INDEX IF NOT EXISTS "IX_Pacientes_HospitalId" ON "Pacientes" ("HospitalId");

-- Continuidad: los usuarios que ya existían quedan habilitados en todos los hospitales.
-- El Admin no lleva filas porque no filtra por hospital.
INSERT INTO "UsuarioHospitales" ("UsuarioId", "HospitalId", "CreatedAt")
SELECT u."Id", h."Id", now()
FROM "Usuarios" u
CROSS JOIN "Hospitales" h
WHERE u."Rol" <> 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM "UsuarioHospitales" uh
      WHERE uh."UsuarioId" = u."Id" AND uh."HospitalId" = h."Id");

COMMIT;
